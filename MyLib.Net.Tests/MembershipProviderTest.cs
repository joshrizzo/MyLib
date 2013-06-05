using System;
using System.Web.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyLib.Providers.Tests
{
	[TestClass()]
	public class MembershipProviderTest
	{
		private const string username = "TestUser";
		private const string password = "password";
		private const string email = "web@aggienetwork.com";
		private const string passwordQuestion = "The question was?";
		private const string passwordAnswer = "6x7";
		private const bool isApproved = true;

		private MembershipUser pUser;
		public MembershipUser User
		{
			get
			{
				if (pUser == null)
				{
					pUser = Membership.GetUser(username);
				}
				return pUser;
			}
			set
			{
				pUser = value;
			}
		}

		[ClassInitialize]
		public static void MyClassInitialize(TestContext context)
		{
			Membership.DeleteUser(username);

			MembershipCreateStatus status;
			var user = Membership.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, null, out status);
			if (status != MembershipCreateStatus.Success)
			{
				throw new Exception("CreateUser failed for some reason..." + status.ToString());
			}
		}

		[ClassCleanup]
		public static void MyClassCleanup()
		{
			Membership.DeleteUser(username, true);
		}

		[TestMethod]
		public void ChangePasswordTest()
		{
			string newPassword = "newPass";
			User.ChangePassword(password, newPassword);
			Assert.IsTrue(Membership.ValidateUser(username, newPassword));
			User.ChangePassword(newPassword, password);
		}

		[TestMethod]
		public void ChangePasswordQuestionAndAnswerTest()
		{
			string newPwdQuestion = "The answer is?";
			string newPwdAnswer = "42";
			User.ChangePasswordQuestionAndAnswer(password, newPwdQuestion, newPwdAnswer);
			string newPassword = User.ResetPassword(newPwdAnswer);
			Assert.IsTrue(!string.IsNullOrWhiteSpace(newPassword));
			User.ChangePassword(newPassword, password);
			User.ChangePasswordQuestionAndAnswer(password, passwordQuestion, passwordAnswer);
		}

		[TestMethod]
		public void CreateUserTest()
		{
			Assert.IsNotNull(User);
		}

		[TestMethod]
		public void DeleteUserTest()
		{
			Membership.DeleteUser(username, true);
			Assert.IsNull(Membership.GetUser(username));
			MembershipCreateStatus status;
			Membership.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, null, out status);
			User = Membership.GetUser(username);
		}

		[TestMethod]
		public void FindUsersByEmailTest()
		{
			int totalRecords = 0;
			var results = Membership.FindUsersByEmail(email, 0, 1, out totalRecords);
			Assert.AreEqual(1, totalRecords);
			Assert.AreEqual(Membership.GetUser(username, false).UserName, results[username].UserName);
		}

		[TestMethod]
		public void FindUsersByNameTest()
		{
			int totalRecords = 0;
			var results = Membership.FindUsersByName(username, 0, 1, out totalRecords);
			Assert.AreEqual(1, totalRecords);
			Assert.AreEqual(Membership.GetUser(username, false).UserName, results[username].UserName);
		}

		[TestMethod]
		public void GetAllUsersTest()
		{
			int totalRecords = 0;
			var results = Membership.GetAllUsers(0, 10, out totalRecords);
			Assert.IsTrue(10 >= totalRecords);
		}

		[TestMethod]
		public void GetNumberOfUsersOnlineTest()
		{
			Assert.IsTrue(Membership.GetNumberOfUsersOnline() > 0);
		}

		[TestMethod]
		public void GetPasswordTest()
		{
			if (Membership.EnablePasswordRetrieval)
			{
				Assert.AreEqual(password, User.GetPassword(passwordAnswer));
			}
			else
			{
				Assert.IsTrue(true);
			}
		}

		[TestMethod]
		public void GetUserTest()
		{
			Assert.IsNotNull(Membership.GetUser(username));
		}

		[TestMethod]
		public void GetUserNameByEmailTest()
		{
			Assert.AreEqual(username, Membership.GetUserNameByEmail(email));
		}

		[TestMethod]
		public void ResetPasswordTest()
		{
			string newPassword = User.ResetPassword(passwordAnswer);
			Assert.IsFalse(string.IsNullOrWhiteSpace(newPassword));
			User.ChangePassword(newPassword, password);
		}

		[TestMethod]
		public void UnlockUserTest()
		{
			for (int i = 0; i <= Membership.MaxInvalidPasswordAttempts; i++)
			{
				Membership.ValidateUser(username, "wrong password");
			}
			User = Membership.GetUser(username);
			Assert.IsTrue(User.IsLockedOut);
			User.UnlockUser();
			Assert.IsFalse(User.IsLockedOut);
		}

		[TestMethod]
		public void UpdateUserTest()
		{
			string newEmail = "testing@test.com";
			User.Email = newEmail;
			Membership.UpdateUser(User);
			Assert.AreEqual(User.Email, newEmail);
			User.Email = email;
			Membership.UpdateUser(User);
		}

		[TestMethod]
		public void ValidateUserTest()
		{
			Assert.IsTrue(Membership.ValidateUser(username, password));
		}
	}
}
