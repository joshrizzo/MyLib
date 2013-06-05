using System;
using System.Linq;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLib.Services;

namespace MyLib.Test
{
	[TestClass]
	public class MockRepositoryServiceTest
	{
		private IRepositoryService repo;
		private TestData testData;

		[TestInitialize]
		public void TestInit()
		{
			repo = new MockRepositoryService();
			testData = Builder<TestData>.CreateNew().With(a => a.Id = Guid.Empty).Build();
			repo.Save(ref testData);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			repo.DeleteAll<TestData>();
		}

		[TestMethod]
		public void SaveTest()
		{
			Assert.AreNotEqual(Guid.Empty, testData.Id);
		}

		[TestMethod]
		public void SaveVerify()
		{
			var data = repo.Search<TestData>().Single();
			Assert.IsTrue(data.TestString == testData.TestString);
			var timeDiff = data.Timestamp.ToUniversalTime().Ticks - testData.Timestamp.ToUniversalTime().Ticks;
			Assert.IsTrue(Math.Abs(timeDiff) <= 10000);
		}

		[TestMethod]
		public void SearchTest()
		{
			Assert.IsNotNull(repo.Search<TestData>().SingleOrDefault());
		}

		[TestMethod]
		public void DeleteTest()
		{
			repo.Delete(testData);
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}

		[TestMethod]
		public void DeleteAllTest()
		{
			repo.DeleteAll<TestData>();
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}

		[TestMethod]
		public void TestDrop()
		{
			repo.Drop<TestData>();
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}
	}
}
