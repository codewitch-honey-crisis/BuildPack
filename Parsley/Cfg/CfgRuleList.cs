using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsley
{
	// beginnings of CFG optimization. Still not implemented
	public class CfgRuleList : IList<CfgRule>
	{
		WeakReference<CfgDocument> _document;
		internal CfgRuleList(CfgDocument cfg)
		{
			_document = new WeakReference<CfgDocument>(cfg);
			_inner = new List<CfgRule>();
		}
		List<CfgRule> _inner;
		internal CfgDocument Document { 
			get {
				CfgDocument result;
				if (_document.TryGetTarget(out result))
					return result;
				return null;
			} 
		}

		public CfgRule this[int index] { get => _inner[index]; set => _inner[index] = value; }

		public int Count => _inner.Count;

		public bool IsReadOnly => ((ICollection<CfgRule>)_inner).IsReadOnly;

		public void Add(CfgRule item)
		{
			var d = Document;
			if(null!=d)
			{
				if (d.StartSymbol == "TypeDecl" && item.Left == "Statement") System.Diagnostics.Debugger.Break();
			}
			_inner.Add(item);
		}

		public void Clear()
		{
			_inner.Clear();
		}

		public bool Contains(CfgRule item)
		{
			return _inner.Contains(item);
		}

		public void CopyTo(CfgRule[] array, int arrayIndex)
		{
			_inner.CopyTo(array, arrayIndex);
		}

		public IEnumerator<CfgRule> GetEnumerator()
		{
			return _inner.GetEnumerator();
		}

		public int IndexOf(CfgRule item)
		{
			return _inner.IndexOf(item);
		}

		public void Insert(int index, CfgRule item)
		{
			_inner.Insert(index, item);
		}

		public bool Remove(CfgRule item)
		{
			return _inner.Remove(item);
		}

		public void RemoveAt(int index)
		{
			_inner.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _inner.GetEnumerator();
		}
	}
}
