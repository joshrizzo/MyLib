﻿using System;
using System.Data;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;

namespace MyLib.Services
{
	public class EFRepositoryService<EFContext> : IRepositoryService where EFContext : ObjectContext, new()
	{
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
			return new EFConectionService<EFContext>();
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
			var quickSearch = new EFContext();
			var set = quickSearch.CreateObjectSet<T>();
			set.MergeOption = MergeOption.NoTracking;
			set.EnablePlanCaching = false;
			return set;
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
			T retval = null;
			using (var db = PersistentConnection())
				retval = db.Save(ref item);
			return retval;
		}

		/// <summary>
		/// Deletes the given document from the database.
		/// </summary>
		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			using (var db = PersistentConnection())
				db.Delete(item);
		}

		/// <summary>
		/// Deletes all documents in the specified collection from the database.
		/// </summary>
		/// <typeparam name="T">The class of the objects that you want to delete.</typeparam>
		public void DeleteAll<T>() where T : class, new()
		{
			using (var db = PersistentConnection())
				db.DeleteAll<T>();
		}

		/// <summary>
		/// Deletes the the specified collection from the database entirely.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to drop.</typeparam>
		public void Drop<T>() where T : class, new()
		{
			using (var db = PersistentConnection())
				db.Drop<T>();
		}

		/// <summary>
		/// Creates a collection for the given type.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to create.</typeparam>
		public void Create<T>() where T : class, new()
		{
			using (var db = PersistentConnection())
				db.Create<T>();
		}
	}

	public class EFConectionService<EFContext> : IRepositoryConnectionService where EFContext : ObjectContext, new()
	{
		private ObjectContext db;

		public EFConectionService()
		{
			db = new EFContext();
		}

		public void Dispose()
		{
			db.SaveChanges();
			db.Dispose();
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
			var set = db.CreateObjectSet<T>();
			set.MergeOption = MergeOption.NoTracking;
			return set;
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
			object oldItem = null;
			var setName = db.CreateObjectSet<T>().EntitySet.Name;
			EntityKey itemID = item.CastTo<EntityObject>().EntityKey ?? db.CreateEntityKey(setName, item);

			if (db.TryGetObjectByKey(itemID, out oldItem))
			{
				var existingItem = oldItem.CastTo<T>();
				if (oldItem.IsNotNull() && existingItem.Timestamp.DropMillisecods() > item.Timestamp.DropMillisecods())
				{
					retval = existingItem;
				}
				else
				{
					item.Timestamp = DateTime.Now;
					db.ApplyCurrentValues(setName, item);
				}
			}
			else
			{
				item.Timestamp = DateTime.Now;
				db.AddObject(setName, item);
			}

			return retval;
		}

		/// <summary>
		/// Deletes this document from the database.
		/// </summary>
		public void Delete<T>(T item) where T : class, IRepoData, new()
		{
			db.Attach(item.CastTo<IEntityWithKey>());
			db.DeleteObject(item);
		}

		/// <summary>
		/// Deletes all documents from this collection.
		/// </summary>
		public void DeleteAll<T>() where T : class, new()
		{
			Search<T>().ForEach(a =>
			{
				db.Attach(a.CastTo<IEntityWithKey>());
				db.DeleteObject(a);
			});
		}

		/// <summary>
		/// Deletes this collection entirely.
		/// </summary>
		public void Drop<T>() where T : class, new()
		{
			throw new Exception("You cannot delete a table with this Entity Framework implementation.");
		}

		/// <summary>
		/// Creates a collection for the given type.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to create.</typeparam>
		public void Create<T>() where T : class, new()
		{
			throw new Exception("You cannot create a table with this Entity Framework implementation.");
		}
	}
}