using System;
using System.Linq;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyLib.Services;

namespace MyLib.Test
{
	[Serializable]
	public class TestData : IRepoData
	{
		public Guid Id { get; set; }
		public DateTime Timestamp { get; set; }
		public string TestString { get; set; }
	}

	[TestClass]
	public class MongoRepositoryServiceTest
	{
		private static IRepositoryService repo;
		private TestData testData;

		[TestInitialize]
		public void TestInit()
		{
			repo = new MongoRepositoryService(true);
			testData = Builder<TestData>.CreateNew().With(a => a.Id = Guid.Empty).Build();
			repo.Save(ref testData);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			repo.DeleteAll<TestData>();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			try
			{
				repo.Drop<TestData>();
			}
			catch (Exception ex)
			{
				Console.WriteLine("The MongoDB Drop command failed on the MongoRepositoryService test class.  The collection was probably dropped already, so no biggie.");
				Console.WriteLine(ex.Message);
			}
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
		public void WriteDeleteAllTest()
		{
			using (var db = repo.PersistentConnection()) db.DeleteAll<TestData>();
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}

		[TestMethod]
		public void DropTest()
		{
			repo.Drop<TestData>();
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}

		[TestMethod]
		public void WriteDropTest()
		{
			using (var db = repo.PersistentConnection()) db.Drop<TestData>();
			Assert.AreEqual(0, repo.Search<TestData>().Count());
		}
	}
}
