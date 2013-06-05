using System;
using System.Collections.Generic;
using System.Linq;
using MyLib.Services;
using Omu.ValueInjecter;

namespace MyLib.Test
{
	public class MockRepositoryService : IRepositoryService
	{
		private Dictionary<Type, List<object>> db;

		public MockRepositoryService()
		{
			db = new Dictionary<Type, List<object>>();
		}

		public IQueryable<T> Search<T>() where T : class, new()
		{
			using (var conn = new MockRepositoryConnection(ref db)) return conn.Search<T>();
		}

		public T Save<T>(ref T item) where T : class, IRepoData, new()
		{
			using (var con = new MockRepositoryConnection(ref db)) return con.Save(ref item);
		}

		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			using (var con = new MockRepositoryConnection(ref db)) con.Delete(item);
		}

		public void DeleteAll<T>() where T : class, new()
		{
			using (var con = new MockRepositoryConnection(ref db)) con.DeleteAll<T>();
		}

		public void Drop<T>() where T : class, new()
		{
			using (var con = new MockRepositoryConnection(ref db)) con.Drop<T>();
		}

		public void Create<T>() where T : class, new()
		{
			using (var con = new MockRepositoryConnection(ref db)) con.Create<T>();
		}

		public IRepositoryConnectionService PersistentConnection()
		{
			return new MockRepositoryConnection(ref db);
		}
	}

	public class MockRepositoryConnection : IRepositoryConnectionService
	{
		public Dictionary<Type, List<object>> db;

		public MockRepositoryConnection(ref Dictionary<Type, List<object>> thisdb)
		{
			db = thisdb;
		}

		public void Dispose()
		{
		}

		public IQueryable<T> Search<T>() where T : class, new()
		{
			return GetCollection<T>().AsQueryable();
		}

		public T Save<T>(ref T item) where T : class, IRepoData, new()
		{
			T retval = null;
			T oldItem = null;
			var itemID = item.Id;
			oldItem = Search<T>().SingleOrDefault(a => a.Id == itemID);

			if (oldItem != null && oldItem.Timestamp.DropMillisecods() > item.Timestamp.DropMillisecods())
			{
				retval = oldItem;
			}
			else
			{
				item.Timestamp = DateTime.Now;
				if (oldItem.IsNotNull())
				{
					oldItem.InjectFrom(item);
				}
				else
				{
					item.Id = Guid.NewGuid();
					db[typeof(T)].Add(item);
				}
			}
			return retval;
		}

		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			db[typeof(T)].Remove(item);
		}

		public void DeleteAll<T>() where T : class, new()
		{
			GetCollection<T>();
			db[typeof(T)].Clear();
		}

		public void Drop<T>() where T : class, new()
		{
			GetCollection<T>();
			db.Remove(typeof(T));
		}

		public List<T> GetCollection<T>() where T : class, new()
		{
			if (!db.ContainsKey(typeof(T))) db.Add(typeof(T), new List<object>());
			else if (db[typeof(T)].IsNull()) db[typeof(T)] = new List<object>();
			return db[typeof(T)].Select(a => a.CastAs<T>()).ToList();
		}

		public void Create<T>() where T : class, new()
		{
			db.Add(typeof(T), new List<object>());
		}
	}

}
