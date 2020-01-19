using System;
namespace CorporateHellscape
{
	/// <summary>
	/// Base widget implementation
	/// </summary>
	partial class Widget 
	{
		// our payload - to be filled
		byte[] _payload;
	
		public override string ToString()
		{
			return string.Concat("[Widget - " , Convert.ToBase64String(_payload) , "]");
		}
	}
}
