using System;

namespace ParsleyAdvancedDemo
{
	class PatchTest
	{
		public event EventHandler Test;

		// this won't come out right unless it's patched
		public PatchTest()
		{
			Test += PatchTest_Test;
		}

		private void PatchTest_Test(object sender, EventArgs e)
		{
			Console.WriteLine("Test");
		}

		
	}
}
