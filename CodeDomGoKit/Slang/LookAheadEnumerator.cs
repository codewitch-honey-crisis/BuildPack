// an enumerator that keeps a lookahead queue used by slang's backtracking parser
using System;
using System.Collections;
using System.Collections.Generic;

namespace CD
{
	/// <summary>
	/// An enumerator that provides lookahead without advancing the cursor
	/// </summary>
	/// <typeparam name="T">The type to enumerate</typeparam>
	class LookAheadEnumerator<T> : IEnumerator<T>
	{
		const int _Enumerating = 0;
		const int _NotStarted = -2;
		const int _Ended = -1;
		const int _Disposed = -3;
		IEnumerator<T> _inner;
		int _state;
		
		// for the lookahead queue
		const int _DefaultCapacity = 16;
		const float _GrowthFactor = .9f;
		T[] _queue;
		int _queueHead;
		int _queueCount;
		public LookAheadEnumerator(IEnumerator<T> inner)
		{
			_inner = inner;
			_state = _NotStarted;
			_queue = new T[_DefaultCapacity];
			_queueHead = 0;
			_queueCount = 0;
		}
		public void DiscardLookAhead()
		{
		
			while (1 < _queueCount)
				_Dequeue();
		}
		public T Current {
			get {
				switch (_state)
				{
					case _NotStarted:
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					case _Ended:
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					case _Disposed:
						throw new ObjectDisposedException(GetType().Name);
				}
				return _queue[_queueHead];
			}

		}
		// legacy enum support (required)
		object IEnumerator.Current { get { return Current; } }
		internal int QueueCount { get { return _queueCount; } }
		public bool TryPeek(int lookahead, out T value)
		{
			if (_Disposed == _state)
				throw new ObjectDisposedException(GetType().Name);
			if (0 > lookahead)
				throw new ArgumentOutOfRangeException(nameof(lookahead));
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
			while (0 < lookahead && _inner.MoveNext())
			{
				value = _inner.Current;
				_Enqueue(value);
				--lookahead;
			}
			return 0 == lookahead;
		}
		public T Peek(int lookahead)
		{
			T value;
			if (!TryPeek(lookahead, out value))
				throw new InvalidOperationException("There were not enough values in the enumeration to satisfy the request");
			return value;
		}
		public IEnumerable<T> LookAhead {
			get {
				return new LookAheadEnumeratorEnumerable<T>(this);
			}
		}
		public bool MoveNext()
		{
			switch (_state)
			{
				case _Disposed:
					throw new ObjectDisposedException(GetType().Name);
				case _Ended:
					return false;
				case _NotStarted:
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
					else
					{
						// prime it
						_Enqueue(_inner.Current);
						_state = _Enumerating;
						return true;
					}
				default: // enumerating
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
		}
		
		public void Reset()
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


		public void Dispose()
		{
			if (_Disposed != _state)
			{
				_inner.Dispose();
				_state = _Disposed;
				GC.SuppressFinalize(this);
			}
		}
		void _Enqueue(T item)
		{
			if (_queueCount == _queue.Length)
			{
				var arr = new T[(int)(_queue.Length * _GrowthFactor)];
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
		~LookAheadEnumerator()
		{
			Dispose();
		}

		#endregion
	}
	class LookAheadEnumeratorEnumerable<T> : IEnumerable<T>
	{
		LookAheadEnumerator<T> _outer;
		public LookAheadEnumeratorEnumerable(LookAheadEnumerator<T> outer)
		{
			_outer = outer;
		}
		public IEnumerator<T> GetEnumerator()
		{
			return new LookAheadEnumeratorEnumerator<T>(_outer);
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
	class LookAheadEnumeratorEnumerator<T> : IEnumerator<T>
	{
		const int _NotStarted = -2;
		const int _Ended = -1;
		const int _Disposed = -3;
		LookAheadEnumerator<T> _outer;
		int _index;
		T _current;
		public LookAheadEnumeratorEnumerator(LookAheadEnumerator<T> outer)
		{
			_outer = outer;
			_index = _NotStarted;
		}
		public T Current {
			get {
				if (0 > _index)
				{
					if (_index == _NotStarted)
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					if(_index == _Ended)
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					throw new ObjectDisposedException(GetType().Name);
				}
				return _current;
			}
		}
				
		object IEnumerator.Current { get { return Current; } }

		public void Dispose()
		{
			_index = _Disposed;
		}

		public bool MoveNext()
		{
			if (0 > _index)
			{
				if (_index == _Disposed)
					throw new ObjectDisposedException(GetType().Name);
				if (_index == _Ended)
					return false;
				_index = 0;
			}
			T value;
			++_index;
			if (!_outer.TryPeek(_index,out value))
			{
				_index = _Ended;
				return false;
			}
			
			_current = value;
			return true;
		}

		public void Reset()
		{
			_index = _NotStarted;
		}
		
	}
}
