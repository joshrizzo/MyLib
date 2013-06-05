using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLib.Services;

namespace MyLib.Test
{
	[TestClass]
	public class WebContextServiceTest
	{
		[TestMethod]
		public void CheckADTest_BadUser()
		{
			Assert.IsFalse(new FrameworkService().CheckAD("FakeUsername", "FakePassword"));
		}

		///// <summary>
		///// NOTE: This username/password will expire on August 1, 2011.
		///// </summary>
		//[TestMethod]
		//public void CheckADTest_GoodUser()
		//{
		//    Assert.IsTrue(new FrameworkService().CheckAD("wos", "powerful,9"));
		//}
	}
}
