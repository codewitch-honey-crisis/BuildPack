using System;
using System.Collections.Generic;

namespace Parsley
{
	/// <summary>
	/// An enumerator that provides lookahead without advancing the cursor
	/// </summary>
	/// <typeparam name="T">The type to enumerate</typeparam>
	class LookAheadEnumerator<T> : object, IEnumerator<T>
	{
		private const int _Enumerating = 0;
		private const int _NotStarted = -2;
		private const int _Ended = -1;
		private const int _Disposed = -3;
		private IEnumerator<T> _inner;
		private int _state;

		// for the lookahead queue
		private const int _DefaultCapacity = 16;
		private const float _GrowthFactor = .9f;
		private T[] _queue;
		private int _queueHead;
		private int _queueCount;
		/// <summary>
		/// Creates a new instance. Once this is created, the inner/wrapped enumerator must not be touched.
		/// </summary>
		/// <param name="inner"></param>
		public LookAheadEnumerator(IEnumerator<T> inner)
		{
			_inner = inner;
			_state = _NotStarted;
			_queue = new T[_DefaultCapacity];
			_queueHead = 0;
			_queueCount = 0;
		}
		/// <summary>
		/// Discards the lookahead and advances the cursor to the physical position.
		/// </summary>
		public void DiscardLookAhead()
		{

			while (1 < _queueCount)
				_Dequeue();
		}
		/// <summary>
		/// Retrieves the value under the cursor
		/// </summary>
		public T Current {
			get {
				if (0 > _state)
				{
					if (_NotStarted == _state)
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					if (_Ended == _state)
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
				}
				return _queue[_queueHead];
			}

		}
		// legacy enum support (required)
		object System.Collections.IEnumerator.Current { get { return Current; } }
		internal int QueueCount { get { return _queueCount; } }
		/// <summary>
		/// Attempts to peek the specified number of positions from the current position without advancing
		/// </summary>
		/// <param name="lookahead">The offset from the current position to peek at</param>
		/// <param name="value">The value returned</param>
		/// <returns>True if the peek could be satisfied, otherwise false</returns>
		public bool TryPeek(int lookahead, out T value)
		{
			if (_Disposed == _state)
				throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
			if (0 > lookahead)
				throw new ArgumentOutOfRangeException("lookahead");
			if (_Ended == _state)
			{
				value = default(T);
				return false;
			}
			if (_NotStarted == _state)
			{
				if (0 == lookahead)
				{
					value = default(T);
					return false;
				}
			}
			if (lookahead < _queueCount)
			{
				value = _queue[(lookahead + _queueHead) % _queue.Length];
				return true;
			}
			lookahead -= _queueCount;
			value = default(T);
			while (0 <= lookahead && _inner.MoveNext())
			{
				value = _inner.Current;
				_Enqueue(value);
				--lookahead;
			}
			return -1 == lookahead;
		}

		/// <summary>
		/// Peek the specified number of positions from the current position without advancing
		/// </summary>
		/// <param name="lookahead">The offset from the current position to peek at</param>
		/// <returns>The value at the specified position</returns>
		public T Peek(int lookahead)
		{
			T value;
			if (!TryPeek(lookahead, out value))
				throw new InvalidOperationException("There were not enough values in the enumeration to satisfy the request");
			return value;
		}
		internal bool IsEnumerating {
			get {
				return -1 < _state;
			}
		}
		internal bool IsEnded {
			get {
				return _Ended == _state;
			}
		}
		/// <summary>
		/// Retrieves a lookahead cursor from the current cursor that can be navigated without moving the main cursor
		/// </summary>
		public IEnumerable<T> LookAhead {
			get {
				if (0 > _state)
				{
					if (_state == _NotStarted)
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					if (_state == _Ended)
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);

				}
				return new LookAheadEnumeratorEnumerable<T>(this);
			}
		}
		/// <summary>
		/// Advances the cursor
		/// </summary>
		/// <returns>True if more input was read, otherwise false</returns>
		bool System.Collections.IEnumerator.MoveNext()
		{
			if (0 > _state)
			{
				if (_Disposed == _state)
					throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
				if (_Ended == _state)
					return false;
				if (_NotStarted == _state)
				{
					if (0 < _queueCount)
					{
						_state = _Enumerating;
						return true;
					}
					if (!_inner.MoveNext())
					{
						_state = _Ended;
						return false;
					}
					// prime it
					_Enqueue(_inner.Current);
					_state = _Enumerating;
					return true;

				}
			}
			_Dequeue();
			if (0 == _queueCount)
			{
				if (!_inner.MoveNext())
				{
					_state = _Ended;
					return false;
				}
				_Enqueue(_inner.Current);
			}
			return true;
		}
		/// <summary>
		/// Resets the cursor, and clears the queue.
		/// </summary>
		void System.Collections.IEnumerator.Reset()
		{
			_inner.Reset();
			if (0 < _queueCount && null == default(T))
			{
				Array.Clear(_queue, _queueHead, _queue.Length - _queueHead);
				if (_queueHead + _queueCount >= _queue.Length)
					Array.Clear(_queue, 0, _queueHead + _queueCount % _queue.Length);
			}
			_queueHead = 0;
			_queueCount = 0;
			_state = _NotStarted;
		}

		#region IDisposable Support

		/// <summary>
		/// Disposes of this instance
		/// </summary>
		void System.IDisposable.Dispose()
		{
			if (_Disposed != _state)
			{
				_inner.Dispose();
				_state = _Disposed;
			}
		}
		void _Enqueue(T item)
		{
			if (_queueCount == _queue.Length)
			{
				var arr = new T[(int)(_queue.Length * (1+_GrowthFactor))];
				if (_queueHead + _queueCount <= _queue.Length)
				{
					Array.Copy(_queue, arr, _queueCount);
					_queueHead = 0;
					arr[_queueCount] = item;
					++_queueCount;
					_queue = arr;
				}
				else // if(_head+_count<=arr.Length)
				{
					Array.Copy(_queue, _queueHead, arr, 0, _queue.Length - _queueHead);
					Array.Copy(_queue, 0, arr, _queue.Length - _queueHead, _queueHead);
					_queueHead = 0;
					arr[_queueCount] = item;
					++_queueCount;
					_queue = arr;
				}
			}
			else
			{
				_queue[(_queueHead + _queueCount) % _queue.Length] = item;
				++_queueCount;
			}
		}
		T _Dequeue()
		{
			if (0 == _queueCount)
				throw new InvalidOperationException("The queue is empty");
			var result = _queue[_queueHead];
			// if the type is a ref type we have to clear it so it gets garbage collected
			_queue[_queueHead] = default(T);
			++_queueHead;
			_queueHead = _queueHead % _queue.Length;
			--_queueCount;
			return result;
		}
		
		#endregion
	}
	class LookAheadEnumeratorEnumerable<T> : object, IEnumerable<T>
	{
		private LookAheadEnumerator<T> _outer;
		public LookAheadEnumeratorEnumerable(LookAheadEnumerator<T> outer)
		{
			_outer = outer;
		}
		public IEnumerator<T> GetEnumerator()
		{
			// for some reason VB was resolving new as AddressOf, so use this.
			LookAheadEnumeratorEnumerator<T> result = (LookAheadEnumeratorEnumerator<T>)
				Activator.CreateInstance(typeof(LookAheadEnumeratorEnumerator<T>),_outer);
			return result;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
	class LookAheadEnumeratorEnumerator<T> : object, IEnumerator<T>
	{
		private const int _NotStarted = -2;
		private const int _Ended = -1;
		private const int _Disposed = -3;
		private LookAheadEnumerator<T> _outer;
		private int _index;
		private T _current;
		public LookAheadEnumeratorEnumerator(LookAheadEnumerator<T> outer)
		{
			_outer = outer;
			if (_outer.IsEnumerating)
				_current = _outer.Current;
			_index = _NotStarted;

		}
		public T Current {
			get {
				if (0 > _index)
				{
					if (_index == _NotStarted)
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					if (_index == _Ended)
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					throw new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator<T>).Name);
				}
				return _current;
			}
		}

		object System.Collections.IEnumerator.Current { get { return Current; } }

		void System.IDisposable.Dispose()
		{
			_index = _Disposed;
		}

		bool System.Collections.IEnumerator.MoveNext()
		{
			T value;
			if (0 > _index)
			{
				if (_index == _Disposed)
					throw new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator<T>).Name);
				if (_index == _Ended)
					return false;
				_index = -1;

			}

			++_index;
			if (!_outer.TryPeek(_index, out value))
			{
				_index = _Ended;
				return false;
			}
			_current = value;
			return true;
		}

		void System.Collections.IEnumerator.Reset()
		{
			_index = _NotStarted;
		}

	}
}
