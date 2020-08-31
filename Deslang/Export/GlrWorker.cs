using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glory
{
	class GlrWorker
	{
		int[][][][] _parseTable;
		// used like a stack because Stack<T> doesn't expose everything we need
		List<int> _stack;
		LookAheadEnumerator _tokenEnum;
		public Token CurrentToken;
		public LRNodeType NodeType;
		public int[] RuleDefinition;
		public int ErrorCount;
		public int Id;
		public int Index;
		int _eosId;
		int _errorId;
		int[] _errorSentinels;
		int _tupleIndex;
		bool _continuation;
		public Queue<Token> ErrorTokens;
		IList<GlrWorker> _workers;
		WeakReference<GlrTableParser> _outer;
		GlrTableParser _Outer {
			get {
				if (null == _outer)
					return null;
				GlrTableParser result;
				if (_outer.TryGetTarget(out result))
					return result;
				return null;
			}
			set {
				_outer = new WeakReference<GlrTableParser>(value);
			}
		}
		public GlrWorker(GlrTableParser outer, int id,int[][][][] parseTable,int errorId,int eosId,int[] errorSentinels, IList<GlrWorker> workers, LookAheadEnumerator tokenEnum)
		{
			_Outer = outer;
			Id = id;
			_parseTable = parseTable;
			_errorId = errorId;
			_eosId = eosId;
			_tokenEnum = tokenEnum;
			_stack = new List<int>();
			Index = 0;
			_tupleIndex = 0;
			_workers = workers;
			_errorSentinels = errorSentinels;
			ErrorTokens = new Queue<Token>();
			_continuation = false;
			NodeType = LRNodeType.Initial;
		}
		public GlrWorker(GlrTableParser outer,GlrWorker worker,int tupleIndex)
		{
			_Outer = outer;
			_parseTable = worker._parseTable;
			_errorId = worker._errorId;
			_eosId = worker._eosId;
			_errorSentinels = worker._errorSentinels;
			ErrorTokens = new Queue<Token>(worker.ErrorTokens);
			_tokenEnum = worker._tokenEnum;
			_stack = new List<int>(worker._stack.Count);
			_stack.AddRange(worker._stack);
			Index = worker.Index;
			_tupleIndex = tupleIndex;
			NodeType = worker.NodeType;
			Id = outer.NextWorkerId;
			CurrentToken = worker.CurrentToken;
			++outer.NextWorkerId;
			_continuation = true;
			_workers = worker._workers;
		}
			
		public bool Read()
		{
			if(0!=ErrorTokens.Count)
			{
				Token tok = ErrorTokens.Dequeue();
				tok.SymbolId = _errorId;
				CurrentToken = tok;
				return true;
			}
			if (_continuation)
				_continuation = false;
			else
			{
				LRNodeType n = NodeType;
				if (LRNodeType.Shift == n)
				{
					_ReadNextToken();
				}
				else if (LRNodeType.Initial == n) {
					_stack.Add(0);
					_ReadNextToken();
					NodeType = LRNodeType.Error;
				} else if (LRNodeType.EndDocument == n) {
					return false;
				} else if(LRNodeType.Accept==n)
				{
					NodeType = LRNodeType.EndDocument;
					_stack.Clear();
					return true;
				}
			}
			if (0 < _stack.Count)
			{
				var entry = _parseTable[_stack[_stack.Count-1]];
				if (_errorId == CurrentToken.SymbolId)
				{
					_tupleIndex = 0;
					_Panic();
					return true;
				}
				var tbl = entry[CurrentToken.SymbolId];
				if(null==tbl)
				{
					_tupleIndex = 0;
					_Panic();
					return true;
				}
				int[] trns = tbl[_tupleIndex];
				// only create more if we're on the first index
				// that way we won't create spurious workers
				if (0 == _tupleIndex)
				{
					for (var i = 1; i < tbl.Length; ++i)
					{
						_workers.Add(new GlrWorker(_Outer, this, i));
					}
				}
				if (null == trns)
				{
					_Panic();
					_tupleIndex = 0;
					return true;
				}
				if (1 == trns.Length)
				{
					if (-1 != trns[0]) // shift
					{
						NodeType = LRNodeType.Shift;
						_stack.Add(trns[0]);
						_tupleIndex = 0;
						return true;
					}
					else
					{ // accept 
					  //throw if _tok is not $ (end)
						if (_eosId != CurrentToken.SymbolId)
						{
							_Panic();
							_tupleIndex = 0;

							return true;
						}

						NodeType = LRNodeType.Accept;
						_stack.Clear();
						_tupleIndex = 0;

						return true;
					}
				}
				else // reduce
				{
					RuleDefinition = new int[trns.Length - 1];
					for (var i = 1; i < trns.Length; ++i)
						RuleDefinition[i - 1] = trns[i];
					for (var i = 2; i < trns.Length; ++i)
						_stack.RemoveAt(_stack.Count-1);

					// There is a new number at the top of the stack. 
					// This number is our temporary state. Get the symbol 
					// from the left-hand side of the rule #. Treat it as 
					// the next input token in the GOTO table (and place 
					// the matching state at the top of the set stack).
					// - Stephen Jackson, https://web.cs.dal.ca/~sjackson/lalr1.html
					var state = _stack[_stack.Count-1];
					var e = _parseTable[state];
					if (null == e)
					{
						_Panic();
						_tupleIndex = 0;

						return true;
					}
					_stack.Add(_parseTable[state][trns[1]][0][0]);
					NodeType = LRNodeType.Reduce;
					_tupleIndex = 0;

					return true;
				}
				
			}
			else
			{
				// if we already encountered an error
				// return EndDocument in this case, since the
				// stack is empty there's nothing to do
				NodeType = LRNodeType.EndDocument;
				_tupleIndex = 0;
				return true;
			}
		}
		public int SymbolId {
			get {
				if (0 < ErrorTokens.Count)
					return _errorId;
				var n = NodeType;
				if(LRNodeType.Reduce==n)
					return RuleDefinition[0];
				if(LRNodeType.Error==n)
					return _errorId;
				if(LRNodeType.Shift==n)
					return CurrentToken.SymbolId;
				
				return -1;
			}
		}

		public string Value {
			get {
				if (0 < ErrorTokens.Count)
					return CurrentToken.Value;
				var n = NodeType;
				if (LRNodeType.Shift == n || LRNodeType.Error == n)
					return CurrentToken.Value;
				return null;
			}
		}

		public bool HasErrors { get { return 0<ErrorTokens.Count; } }

		void _UpdatePositionFinal()
		{
			for (var i = 0; i < CurrentToken.Value.Length; ++i)
			{
				var ch = CurrentToken.Value[i];

				if ('\n' == ch) {
					++CurrentToken.Line;
					CurrentToken.Column = 1;
				} else if('\r'==ch)
					CurrentToken.Column = 1;
				else if('\t'==ch)
					CurrentToken.Column += 4;
				else 
					++CurrentToken.Column;
				
				++CurrentToken.Position;
			}
		}
		void _ReadNextToken()
		{
			Token tok;
			if(_tokenEnum.TryPeek(Index,out tok))
			{
				CurrentToken = tok;
				if (-1 == CurrentToken.SymbolId)
					CurrentToken.SymbolId = _errorId;
				++Index;
			}
			else
			{
				CurrentToken.SymbolId = _eosId;
				if (null != CurrentToken.Value)
					_UpdatePositionFinal();
			}
		}
		void _Panic()
		{
			var sa = _errorSentinels;
			if(null==sa )
			{
				sa = new int[] {};
			}
			int idx= Array.IndexOf(sa, CurrentToken.SymbolId);
			if(-1<idx)
			{
				++ErrorCount; 
				ErrorTokens.Enqueue(CurrentToken);
				_ReadNextToken();
			}
			while (_eosId != CurrentToken.SymbolId && 0 > (idx=Array.IndexOf(sa, CurrentToken.SymbolId)))
			{
				++ErrorCount; 
				ErrorTokens.Enqueue(CurrentToken);
				_ReadNextToken();
			}
			if(-1<idx) // found a sentinel, adjust the stack now
			{
				while(0<_stack.Count)
				{
					var entry = _parseTable[_stack[_stack.Count-1]];
					var t= entry[CurrentToken.SymbolId];
					if (null != t)
					{
						_continuation = true;
						return;
					}
					else
						_stack.RemoveAt(_stack.Count - 1);
				}
			}	
		}
	}
}
