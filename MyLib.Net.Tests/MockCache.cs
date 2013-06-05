using System.Collections.Generic;

namespace MyLib.Test
{
	public class MockCache : ICache
	{
		private Dictionary<string, object> cache;

		public MockCache()
		{
			cache = new Dictionary<string, object>();
		}

		public object Get(string key)
		{
			return cache.ContainsKey(key) ? cache[key] : null;
		}

		public void Insert(string key, object value, System.Web.Caching.CacheDependency dependencies)
		{
			cache.Add(key, value);
		}
	}
}
