using System;
using System.Linq;
using System.Web.Configuration;
using FluentMongo.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MyLib.Services
{
	/// <summary>
	/// Provides basic CRUD operation helper methods for this website's MongoDB data.
	/// </summary>
	public class MongoRepositoryService : IRepositoryService
	{
		private MongoDatabase db;
		private SafeMode safeMode = SafeMode.False;
		public bool Safe
		{
			get
			{
				if (safeMode == SafeMode.True) return true;
				else return false;
			}
		}

		public MongoRepositoryService(bool safe = false)
		{
			db = MongoDatabase.Create(WebConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
			if (safe) safeMode = SafeMode.True;
		}

		/// <summary>
		/// Use this to write to a database collection using a single connection.
		/// NOTE: Use this inside a "using" statement or manually call .Dispose() to close the connection.
		/// </summary>
		/// <typeparam name="T">The class of the object that you want to write.</typeparam>
		/// <returns>Returns an IDisposable object with database I/O methods.</returns>
		/// <example>
		/// using (var db = new RepositoryService().Write&lt;MY_CLASS&gt;())
		/// {
		///		db.Delete(MY_OLD_OBJECT);
		///		db.Save(ref MY_NEW_OBJECT);
		/// }
		/// </example>
		public IRepositoryConnectionService PersistentConnection()
		{
			return new RepositoryConnectionService(db, safeMode);
		}

		/// <summary>
		/// Use this to query the database with LINQ.
		/// NOTE: Some operations may not be supported directly on the database. If you continue to
		/// get errors on a query, try doing .ToList() first and performing the query in memory.
		/// </summary>
		/// <typeparam name="T">The class of the object that you want to query.</typeparam>
		/// <returns>A LINQ query compatible object.</returns>
		public IQueryable<T> Search<T>() where T : class, new()
		{
			using (var db = this.PersistentConnection()) return db.Search<T>();
		}

		/// <summary>
		/// Saves this document to the database and sets the timestamp.
		/// </summary>
		/// <returns>
		/// Returns null on success.  If the database's version of the document's timestamp 
		/// is newer than the document passed to this function, then the return value will 
		/// be the conflicting document from the database and the save will fail.
		/// </returns>
		public T Save<T>(ref T item) where T : class, IRepoData, new()
		{
			using (var db = this.PersistentConnection()) return db.Save(ref item);
		}

		/// <summary>
		/// Deletes the given document from the database.
		/// </summary>
		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			using (var db = this.PersistentConnection()) db.Delete(item);
		}

		/// <summary>
		/// Deletes all documents in the specified collection from the database.
		/// </summary>
		/// <typeparam name="T">The class of the objects that you want to delete.</typeparam>
		public void DeleteAll<T>() where T : class, new()
		{
			using (var db = this.PersistentConnection()) db.DeleteAll<T>();
		}

		/// <summary>
		/// Deletes the the specified collection from the database entirely.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to drop.</typeparam>
		public void Drop<T>() where T : class, new()
		{
			using (var db = this.PersistentConnection()) db.Drop<T>();
		}

		/// <summary>
		/// Creates a collection for the given type.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to create.</typeparam>
		public void Create<T>() where T : class, new()
		{
			using (var db = this.PersistentConnection()) db.Create<T>();
		}
	}

	public class RepositoryConnectionService : IRepositoryConnectionService
	{
		private MongoDatabase db;
		private SafeMode safeMode;

		public RepositoryConnectionService(MongoDatabase database, SafeMode safe)
		{
			db = database;
			safeMode = safe;
			db.RequestStart();
		}

		private MongoCollection<T> GetCollection<T>() where T : class, new()
		{
			string typeName = typeof(T).Name;
			var collection = db.GetCollection<T>(typeName, safeMode);
			return collection;
		}

		/// <summary>
		/// Use this to query the database with LINQ.
		/// NOTE: Some operations may not be supported directly on the database. If you continue to
		/// get errors on a query, try doing .ToList() first and performing the query in memory.
		/// </summary>
		/// <typeparam name="T">The class of the object that you want to query.</typeparam>
		/// <returns>A LINQ query compatible object.</returns>
		public T Save<T>(ref T item) where T : class, IRepoData, new()
		{
			T retval = null;
			T oldItem = null;
			var itemID = item.Id;
			var collection = GetCollection<T>();
			oldItem = collection.FindOneAs<T>(Query.EQ("Id", itemID.ToString()));

			if (oldItem != null && oldItem.Timestamp.DropMillisecods() > item.Timestamp.DropMillisecods())
			{
				retval = oldItem;
			}
			else
			{
				item.Timestamp = DateTime.Now;
				collection.Save(item, SafeMode.True);
			}
			return retval;
		}

		/// <summary>
		/// Deletes this document from the database.
		/// </summary>
		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			GetCollection<T>().Remove(Query.EQ("_id", item.Id), SafeMode.True);
		}

		/// <summary>
		/// Deletes all documents from this collection.
		/// </summary>
		public void DeleteAll<T>() where T : class, new()
		{
			GetCollection<T>().RemoveAll();
		}

		/// <summary>
		/// Deletes this collection entirely.
		/// </summary>
		public void Drop<T>() where T : class, new()
		{
			GetCollection<T>().Drop();
		}

		public void Dispose()
		{
			db.RequestDone();
		}

		/// <summary>
		/// Use this to query the database with LINQ.
		/// NOTE: Some operations may not be supported directly on the database. If you continue to
		/// get errors on a query, try doing .ToList() first and performing the query in memory.
		/// </summary>
		/// <typeparam name="T">The class of the object that you want to query.</typeparam>
		/// <returns>A LINQ query compatible object.</returns>
		public IQueryable<T> Search<T>() where T : class, new()
		{
			return GetCollection<T>().AsQueryable();
		}

		/// <summary>
		/// Creates a collection for the given type.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to create.</typeparam>
		public void Create<T>() where T : class, new()
		{
			string typeName = typeof(T).Name;
			db.CreateCollection(typeName, null);
		}
	}
}