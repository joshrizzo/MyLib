using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Security;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MyLib.Providers
{
	public sealed class MongoRoleProvider : RoleProvider
	{
		public override string ApplicationName
		{
			get;
			set;
		}

		public string ConnectionString
		{
			get;
			set;
		}

		public override void Initialize(string name, NameValueCollection config)
		{
			if (config == null)
			{
				throw new ArgumentNullException("config");
			}

			if (String.IsNullOrWhiteSpace(config["connectionString"]))
			{
				throw new ProviderException("You must provide a connection string to your MongoDB server.");
			}

			if (name == null || name.Length == 0)
			{
				name = "MongoRoleProvider";
			}

			if (String.IsNullOrWhiteSpace(config["description"]))
			{
				config.Remove("description");
				config.Add("description", "A custom Role Provider for MongoDB.");
			}

			base.Initialize(name, config);

			ApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);

			ConnectionString = ConfigurationManager.ConnectionStrings[config["connectionString"]].ConnectionString;
			if (string.IsNullOrWhiteSpace(ConnectionString))
			{
				ConnectionString = config["connectionString"];
			}
		}

		private string GetConfigValue(string configValue, string defaultValue)
		{
			if (String.IsNullOrWhiteSpace(configValue))
			{
				return defaultValue;
			}

			return configValue;
		}

		public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		{
			CheckRolesExist(roleNames);

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var permissions = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name);

				foreach (string username in usernames)
				{
					MongoPermission permission =
						permissions.AsQueryable().SingleOrDefault(p => p.Username == username) ??
						new MongoPermission();
					permission.Username = username;
					permission.Roles.AddRangeUnique(roleNames);
					permissions.Save(permission);
				}
			}
		}

		public override void CreateRole(string roleName)
		{
			if (RoleExists(roleName))
			{
				throw new ProviderException("This role already exists.");
			}

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				db.GetCollection<MongoRole>(typeof(MongoRole).Name).Save(new MongoRole()
				{
					Name = roleName
				});
			}
		}

		public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			if (RoleExists(roleName))
			{
				var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
				{
					if (throwOnPopulatedRole && db.GetCollection<MongoPermission>(typeof(MongoPermission).Name).AsQueryable().ToList().Where(p => p.Roles.Contains(roleName)).Count() > 0)
					{
						throw new ProviderException("This role currently has users that belong to it.");
					}

					var roles = db.GetCollection<MongoRole>(typeof(MongoRole).Name);
					var role = roles.AsQueryable().SingleOrDefault(r => r.Name == roleName);
					roles.Remove(Query.EQ("_id", role._id));
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		{
			CheckRoleExists(roleName);

			string[] retval = new string[0];
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				retval = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name).AsQueryable().ToList()
					.Where(p => p.Roles.Contains(roleName) && Regex.IsMatch(p.Username, usernameToMatch))
					.Select(p => p.Username).ToArray();
			}
			return retval;
		}

		public override string[] GetAllRoles()
		{
			string[] retval = new string[0];
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				retval = db.GetCollection<MongoRole>(typeof(MongoRole).Name).AsQueryable().Select(r => r.Name).ToArray();
			}
			return retval;
		}

		public override string[] GetRolesForUser(string username)
		{
			string[] retval = new string[0];
			var db = MongoDatabase.Create(ConnectionString);
			using (db.RequestStart())
			{
				var permission = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name).AsQueryable().SingleOrDefault(p => p.Username == username);
				if (permission != null)
				{
					retval = permission.Roles.ToArray();
				}
			}
			return retval;
		}

		public override string[] GetUsersInRole(string roleName)
		{
			CheckRoleExists(roleName);

			string[] retval = new string[0];
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				retval = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name).AsQueryable().ToList()
					.Where(p => p.Roles.Contains(roleName)).Select(p => p.Username).ToArray();
			}
			return retval;
		}

		public override bool IsUserInRole(string username, string roleName)
		{
			CheckRoleExists(roleName);

			bool retval = false;
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var permission = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name).AsQueryable().SingleOrDefault(p => p.Username == username);
				if (permission != null)
				{
					retval = permission.Roles.Contains(roleName);
				}
			}
			return retval;
		}

		public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		{
			CheckRolesExist(roleNames);

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				foreach (string username in usernames)
				{
					var permissions = db.GetCollection<MongoPermission>(typeof(MongoPermission).Name);
					var permission = permissions.AsQueryable().SingleOrDefault(p => p.Username == username);
					if (permission != null)
					{
						permission.Roles.RemoveAll(r => roleNames.Contains(r));

						if (permission.Roles.Count == 0)
						{
							permissions.Remove(Query.EQ("_id", permission._id));
						}
						else
						{
							permissions.Save(permission);
						}
					}
				}
			}
		}

		public override bool RoleExists(string roleName)
		{
			bool retval = false;
			var db = MongoDatabase.Create(ConnectionString);
			using (db.RequestStart())
			{
				retval = db.GetCollection<MongoRole>(typeof(MongoRole).Name).AsQueryable().Count(r => r.Name == roleName) > 0;
			}
			return retval;
		}

		private void CheckRolesExist(string[] roleNames)
		{
			if (roleNames.IsNull()) throw new ArgumentNullException("roleNames");

			foreach (string role in roleNames)
			{
				if (!RoleExists(role))
				{
					throw new ProviderException("The role '" + role + "' does not exist.");
				}
			}
		}

		private void CheckRoleExists(string roleName)
		{
			if (!RoleExists(roleName))
			{
				throw new ProviderException("This role does not exist.");
			}
		}

		private class MongoRole
		{
			public ObjectId _id
			{
				get;
				set;
			}

			public string Name
			{
				get;
				set;
			}
		}

		private class MongoPermission
		{
			public ObjectId _id
			{
				get;
				set;
			}

			public string Username
			{
				get;
				set;
			}

			public IList<string> Roles
			{
				get;
				set;
			}

			public MongoPermission()
			{
				Roles = new List<string>();
			}
		}
	}
}