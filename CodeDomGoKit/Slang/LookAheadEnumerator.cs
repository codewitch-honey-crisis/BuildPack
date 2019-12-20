using System;
using System.Collections;
using System.Collections.Generic;

namespace CD
{
	class LookAheadEnumerator<T> : IEnumerator<T>
	{
		const int _Enumerating = 0;
		const int _FirstRead = 1;
		const int _NotStarted = -2;
		const int _Ended = -1;
		const int _Disposed = -3;
		IEnumerator<T> _inner;
		IndexedQueue<T> _queue;
		int _state;
		public LookAheadEnumerator(IEnumerator<T> inner)
		{
			_inner = inner;
			_state = _NotStarted;
			_queue = new IndexedQueue<T>();
		}
		public T Current {
			get {
				switch(_state)
				{
					case _NotStarted:
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
					case _Ended:
						throw new InvalidOperationException("The cursor is after the end of the enumeration.");
					case _Disposed:
						throw new ObjectDisposedException(GetType().Name);
				}
				return _queue.Peek();
			}

		}
		// legacy enum support (required)
		object IEnumerator.Current => Current;

		public bool TryPeek(int lookahead,out T value)
		{
			if (_Disposed == _state)
				throw new ObjectDisposedException(GetType().Name);
			if (0 > lookahead)
				throw new ArgumentOutOfRangeException(nameof(lookahead));
			if(_Ended==_state)
			{
				value = default(T);
				return false;
			}

			if (0==lookahead)
			{
				if(_NotStarted==_state)
				{
					if(!MoveNext())
					{
						value = default(T);
						return false;
					}
					_state = _FirstRead;
				}
				value = _queue.Peek();
				return true;
			}
			bool read = false;
			value = default(T);
			while (_queue.Count<=lookahead)
			{
				if(!_inner.MoveNext())
				{
					if(0==_queue.Count)
						_state = _Ended;
					value = default(T);
					return false;
				}
				value = _inner.Current;
				_queue.Enqueue(value);
				read = true;
			}
			// if this is our first peek out this far we shortcutted because we just read it so we return it
			if (read)
				return true;
			value=_queue[lookahead];
			return true;
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
				T value;
				var i = 1;
				while (TryPeek(i, out value))
				{
					yield return value;
					++i;
				}
			}
		}
		public bool MoveNext()
		{
			switch(_state)
			{
				case _Disposed:
					throw new ObjectDisposedException(GetType().Name);
				case _Ended:
					return false;
				case _NotStarted:
					if(!_inner.MoveNext())
					{
						_state = _Ended;
						return false;
					} else
					{
						// prime it
						_queue.Enqueue(_inner.Current);
						_state = _Enumerating;
						return true;
					}
				default: // enumerating
					_queue.Dequeue();
					if(0==_queue.Count)
					{
						if(!_inner.MoveNext())
						{
							_state = _Ended;
							return false;
						}
						_queue.Enqueue(_inner.Current);
					}
					return true;
			}
		}

		public void Reset()
		{
			_inner.Reset();
			_queue.Clear();
			_state = _NotStarted;
		}

		#region IDisposable Support


		public void Dispose()
		{
			if (_Disposed!=_state)
			{
				_inner.Dispose();
				_state = _Disposed;
				GC.SuppressFinalize(this);
			}
		}

		~LookAheadEnumerator() {
			Dispose();
		}

		#endregion

	}
}
