using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyLib.Test
{
	[TestClass]
	public class ExtensionMethodsTests
	{
		[TestMethod]
		public void TestToCommaDelimitedString()
		{
			string[] myArray = new string[] { "this", "that", "something" };
			string retval = myArray.ToCommaDelimitedString();
			Assert.IsTrue(retval == "this, that, and something");
		}
	}
}
