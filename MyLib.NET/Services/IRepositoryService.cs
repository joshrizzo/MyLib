using System;
using System.Linq;

namespace MyLib.Services
{
	public interface IRepositoryOperations
	{
		/// <summary>
		/// Use this to query the database with LINQ.
		/// NOTE: Some operations may not be supported directly on the database. If you continue to
		/// get errors on a query, try doing .ToList() first and performing the query in memory.
		/// </summary>
		/// <typeparam name="T">The class of the object that you want to query.</typeparam>
		/// <returns>A LINQ query compatible object.</returns>
		IQueryable<T> Search<T>() where T : class, new();

		/// <summary>
		/// Saves this document to the database and sets the timestamp.
		/// </summary>
		/// <returns>
		/// Returns null on success.  If the database's version of the document's timestamp 
		/// is newer than the document passed to this function, then the return value will 
		/// be the conflicting document from the database and the save will fail.
		/// </returns>
		T Save<T>(ref T item) where T : class, IRepoData, new();

		/// <summary>
		/// Deletes the given document from the database.
		/// </summary>
		void Delete<T>(T item) where T : class, IRepoData, new();

		/// <summary>
		/// Deletes all documents in the specified collection from the database.
		/// </summary>
		/// <typeparam name="T">The class of the objects that you want to delete.</typeparam>
		void DeleteAll<T>() where T : class, new();

		/// <summary>
		/// Deletes the the specified collection from the database entirely.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to drop.</typeparam>
		void Drop<T>() where T : class, new();

		/// <summary>
		/// Creates a collection for the given type.
		/// </summary>
		/// <typeparam name="T">The class of the collection that you want to create.</typeparam>
		void Create<T>() where T : class, new();
	}

	public interface IRepositoryService : IRepositoryOperations, IService
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
		IRepositoryConnectionService PersistentConnection();
	}

	public interface IRepositoryConnectionService : IDisposable, IRepositoryOperations
	{
	}

	/// <summary>
	/// Implement to make an object savable to the database.
	/// </summary>
	public interface IRepoData
	{
		Guid Id { get; set; }
		DateTime Timestamp { get; set; }
	}
}
