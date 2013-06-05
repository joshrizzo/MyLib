using System.Linq;
using System.Web.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyLib.Tests
{
	[TestClass()]
	public class RoleProviderTest
	{
		private static string[] userList = new string[] { "testUser1", "testUser2" };
		private static string[] roleList = new string[] { "testRole1", "testRole2" };

		[ClassInitialize()]
		public static void MyClassInitialize(TestContext context)
		{
			foreach (string role in roleList)
			{
				if (!Roles.RoleExists(role))
				{
					Roles.CreateRole(role);
				}
			}
		}

		[ClassCleanup()]
		public static void MyClassCleanup()
		{
			Roles.RemoveUsersFromRoles(userList, roleList);

			foreach (string role in roleList)
			{
				if (Roles.RoleExists(role))
				{
					Roles.DeleteRole(role);
				}
			}
		}

		[TestMethod()]
		public void AddUsersToRolesTest()
		{
			Roles.AddUsersToRoles(userList, roleList);
			Assert.IsTrue(Roles.IsUserInRole(userList[0], roleList[0]));
			Assert.IsTrue(Roles.IsUserInRole(userList[1], roleList[1]));
		}

		[TestMethod]
		public void CreateRoleAndRoleExistsTest()
		{
			string test = "testRole3";
			Roles.DeleteRole(test);
			Roles.CreateRole(test);
			Assert.IsTrue(Roles.RoleExists(test));
			Roles.DeleteRole(test);
		}

		[TestMethod()]
		public void FindUsersInRoleTest()
		{
			Roles.AddUserToRole(userList[0], roleList[0]);
			string[] results = Roles.FindUsersInRole(roleList[0], userList[0].Substring(1));
			Assert.IsTrue(results.Length == 1);
			Assert.IsTrue(results[0] == userList[0]);
		}

		[TestMethod()]
		public void GetAllRolesTest()
		{
			var roles = Roles.GetAllRoles();
			foreach (string role in roleList)
			{
				Assert.IsTrue(roles.Contains(role));
			}
		}

		[TestMethod()]
		public void GetRolesForUserTest()
		{
			Roles.AddUserToRoles(userList[0], roleList);
			var roles = Roles.GetRolesForUser(userList[0]) ?? new string[0];
			foreach (string role in roleList)
			{
				Assert.IsTrue(roles.Contains(role));
			}
		}

		[TestMethod()]
		public void GetUsersInRoleTest()
		{
			Roles.AddUsersToRole(userList, roleList[0]);
			var users = Roles.GetUsersInRole(roleList[0]);
			foreach (string user in userList)
			{
				Assert.IsTrue(users.Contains(user));
			}
		}

		[TestMethod()]
		public void IsUserInRoleTest()
		{
			Roles.AddUserToRole(userList[0], roleList[0]);
			Assert.IsTrue(Roles.IsUserInRole(userList[0], roleList[0]));
		}

		[TestMethod()]
		public void RemoveUsersFromRolesTest()
		{
			Roles.AddUserToRole(userList[0], roleList[0]);
			Roles.RemoveUserFromRole(userList[0], roleList[0]);
			Assert.IsFalse(Roles.IsUserInRole(userList[0], roleList[0]));
		}

		[TestMethod()]
		public void RoleExistsTest()
		{
			Assert.IsTrue(Roles.RoleExists(roleList[0]));
		}
	}
}
