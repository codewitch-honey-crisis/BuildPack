using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CD
{
	
	class ConcatEnumerator<T> : IEnumerator<T> 
	{
		const int _Enumerating = 0;
		const int _NotStarted = -2;
		const int _Ended = -1;
		const int _Disposed = -3;
		int _state;
		IEnumerator<IEnumerable<T>> _collections;
		IEnumerator<T> _e;
		public ConcatEnumerator(IEnumerable<IEnumerable<T>> collections)
		{
			_collections = collections.GetEnumerator();
			_state = _NotStarted;
			_e = null;
		}
		public ConcatEnumerator(params IEnumerable<T>[] collections) : this((IEnumerable<IEnumerable<T>>)collections)
		{
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
				return _e.Current;
			}
		}
		// legacy enumerator support (required)
		object System.Collections.IEnumerator.Current => Current;
		public bool MoveNext()
		{
			switch (_state)
			{
				case _Disposed:
					throw new ObjectDisposedException(GetType().Name);
				case _Ended:
					return false;
				case _NotStarted:
					return _MoveToNextEnum();	
			}
			// enumerating
			if (!_e.MoveNext())
				return _MoveToNextEnum();
			return true;
		}

		bool _MoveToNextEnum()
		{
			var found = false;
			while (_collections.MoveNext())
			{
				var e = _collections.Current.GetEnumerator();
				if (e.MoveNext())
				{
					found = true;
					if (null != _e)
						_e.Dispose();
					_e = e;
					break;
				}
			}
			if (!found)
			{
				_state = _Ended;
				return false;
			}
			_state = _Enumerating;
			return true;
		}
		public void Reset()
		{
			switch(_state)
			{
				case _Disposed:
					throw new ObjectDisposedException(GetType().Name);
				case _NotStarted:
					return;
			}
			_collections.Reset();
			if (null != _e)
				_e.Dispose();
		}
		public void Dispose()
		{
			_state = _Disposed;
			if (null != _e)
				_e.Dispose();
			_collections.Dispose();
		}
	}
}
