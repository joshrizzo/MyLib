using System.Collections.Generic;
using System.Configuration;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;

public static class MemoryCache
{
	/// <summary>
	/// Either executes an EF LINQ query and and caches the results to memory,
	/// or retrieves the the results from memory if the cache exists.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="q">The EF LINQ query to be executed.</param>
	/// <returns>The results of the query.</returns>
	public static List<T> FromCache<T>(this IQueryable<T> q)
	{
		string connStrKayVal = ((ObjectQuery)q).Context.Connection.ConnectionString;

		var regex = new Regex(@"(name=)(?<val>[\w]+)");
		string connStrKey = regex.Match(connStrKayVal).Groups["val"].Value;

		string connStr = ConfigurationManager.ConnectionStrings[connStrKey].ConnectionString;

		regex = new Regex(@"provider connection string='(?<val>.+)'");
		string sqlConnStr = regex.Match(connStr).Groups["val"].Value;

		return q.FromCache(new HttpCacheWrapper(), sqlConnStr);
	}

	public static List<T> FromCache<T>(this IQueryable<T> q, ICache currentCache, string sqlConnStr)
	{
		var queryObject = (ObjectQuery)(q);
		string sqlCmd = queryObject.ToTraceString();
		var cacheKey = string.Format("{0}__{1}", sqlCmd.GetMd5Sum(), string.Join(",", queryObject.Parameters.OrderBy(a => a.Name).Select(a => a.Value).ToArray()));
		List<T> objCache = (List<T>)(currentCache.Get(cacheKey));

		if (objCache == null)
		{
			using (SqlConnection conn = new SqlConnection(sqlConnStr))
			{
				conn.Open();
				SqlCommand cmd = new SqlCommand(sqlCmd, conn);
				foreach (var param in queryObject.Parameters)
					cmd.Parameters.AddWithValue(param.Name, param.Value);

				foreach (SqlParameter param in cmd.Parameters)
					sqlCmd = sqlCmd.Replace("@" + param.ParameterName, "'{0}'".FormatWith(param.Value.ToString().Replace("'", "''")));

				cmd = new SqlCommand(sqlCmd, conn);

				using (cmd)
				{
					SqlCacheDependency sqlDep = null;
				CreateDependency:
					try
					{
						sqlDep = new SqlCacheDependency(cmd);
					}
					catch (DatabaseNotEnabledForNotificationException)
					{
						SqlCacheDependencyAdmin.EnableNotifications(sqlConnStr);
						goto CreateDependency;
					}
					catch (TableNotEnabledForNotificationException ex)
					{
						((string[])(ex.Data["Tables"]))
							.Where(table => !SqlCacheDependencyAdmin.GetTablesEnabledForNotifications(sqlConnStr).Contains(table)).ToList()
							.ForEach(table => SqlCacheDependencyAdmin.EnableTableForNotifications(sqlConnStr, table));
						goto CreateDependency;
					}

					var reader = cmd.ExecuteReader();
					string results = string.Empty;
					while (reader.Read())
					{

					}
					objCache = q.ToList();
					currentCache.Insert(cacheKey, objCache, sqlDep);
				}
			}
		}
		return objCache;
	}
}

public interface ICache
{
	object Get(string key);
	void Insert(string key, object value, CacheDependency dependencies);
}

public class HttpCacheWrapper : ICache
{
	public object Get(string key)
	{
		return HttpRuntime.Cache.Get(key);
	}

	public void Insert(string key, object value, CacheDependency dependencies)
	{
		HttpRuntime.Cache.Insert(key, value, dependencies);
	}
}
