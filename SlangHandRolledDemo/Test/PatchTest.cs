using System;

namespace ParsleyAdvancedDemo
{
	class PatchTest
	{
		public event EventHandler Test;

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
