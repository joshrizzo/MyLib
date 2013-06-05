using System;
using System.Linq;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLib.Services;

namespace MyLib.Test
{
	[TestClass]
	public class EFRepositoryServiceTest
	{
		private IRepositoryService repo;
		private EFTestData testData;

		[TestInitialize]
		public void TestInit()
		{
			repo = new EFRepositoryService<Entities>();
			repo.DeleteAll<EFTestData>();
			testData = Builder<EFTestData>.CreateNew().With(a => a.Id = Guid.Empty).Build();
			repo.Save(ref testData);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			repo.DeleteAll<EFTestData>();
		}

		[TestMethod]
		public void SaveTest()
		{
			Assert.IsTrue(Guid.Empty == testData.Id);
		}

		[TestMethod]
		public void SaveVerify()
		{
			var data = repo.Search<EFTestData>().Single();
			Assert.IsTrue(data.TestString == testData.TestString);
			var timeDiff = data.Timestamp.ToUniversalTime().Ticks - testData.Timestamp.ToUniversalTime().Ticks;
			Assert.IsTrue(Math.Abs(timeDiff) <= 10000);
		}

		[TestMethod]
		public void SearchTest()
		{
			Assert.IsNotNull(repo.Search<EFTestData>().SingleOrDefault());
		}

		[TestMethod]
		public void DeleteTest()
		{
			repo.Delete(testData);
			Assert.AreEqual(0, repo.Search<EFTestData>().Count());
		}

		[TestMethod]
		public void DeleteAllTest()
		{
			repo.DeleteAll<EFTestData>();
			Assert.AreEqual(0, repo.Search<EFTestData>().Count());
		}
	}
}
