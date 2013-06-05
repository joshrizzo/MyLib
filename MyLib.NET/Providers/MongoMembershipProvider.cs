using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Web.Profile;
using System.Web.Security;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MyLib.Providers
{
	public sealed class MongoMembershipProvider : MembershipProvider
	{
		#region Properties

		private string pApplicationName;
		private bool pEnablePasswordReset;
		private bool pEnablePasswordRetrieval;
		private bool pRequiresQuestionAndAnswer;
		private bool pRequiresUniqueEmail;
		private int pMaxInvalidPasswordAttempts;
		private int pPasswordAttemptWindow;
		private MembershipPasswordFormat pPasswordFormat;
		private int pMinRequiredNonAlphanumericCharacters;
		private int pMinRequiredPasswordLength;
		private string pPasswordStrengthRegularExpression;

		public override string ApplicationName
		{
			get
			{
				return pApplicationName;
			}
			set
			{
				pApplicationName = value;
			}
		}

		public override bool EnablePasswordReset
		{
			get
			{
				return pEnablePasswordReset;
			}
		}

		public override bool EnablePasswordRetrieval
		{
			get
			{
				return pEnablePasswordRetrieval;
			}
		}

		public override bool RequiresQuestionAndAnswer
		{
			get
			{
				return pRequiresQuestionAndAnswer;
			}
		}

		public override bool RequiresUniqueEmail
		{
			get
			{
				return pRequiresUniqueEmail;
			}
		}

		public override int MaxInvalidPasswordAttempts
		{
			get
			{
				return pMaxInvalidPasswordAttempts;
			}
		}

		public override int PasswordAttemptWindow
		{
			get
			{
				return pPasswordAttemptWindow;
			}
		}

		public override MembershipPasswordFormat PasswordFormat
		{
			get
			{
				return pPasswordFormat;
			}
		}

		public override int MinRequiredNonAlphanumericCharacters
		{
			get
			{
				return pMinRequiredNonAlphanumericCharacters;
			}
		}

		public override int MinRequiredPasswordLength
		{
			get
			{
				return pMinRequiredPasswordLength;
			}
		}

		public override string PasswordStrengthRegularExpression
		{
			get
			{
				return pPasswordStrengthRegularExpression;
			}
		}

		public int NewPasswordLength
		{
			get;
			set;
		}

		public string ConnectionString
		{
			get;
			set;
		}

		#endregion

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
				name = "MongoMembershipProvider";
			}

			if (String.IsNullOrWhiteSpace(config["description"]))
			{
				config.Remove("description");
				config.Add("description", "A custom Membership Provider for MongoDB.");
			}

			base.Initialize(name, config);

			pApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
			pMaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
			pPasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
			pMinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "0"));
			pMinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "6"));
			pPasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
			pEnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
			pEnablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "false"));
			pRequiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
			pRequiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
			NewPasswordLength = Convert.ToInt32(GetConfigValue(config["newPasswordLength"], "8"));

			switch (config["passwordFormat"])
			{
				case "Hashed":
				case null:
					pPasswordFormat = MembershipPasswordFormat.Hashed;
					break;
				case "Encrypted":
					pPasswordFormat = MembershipPasswordFormat.Encrypted;
					break;
				case "Clear":
					pPasswordFormat = MembershipPasswordFormat.Clear;
					break;
				default:
					throw new ProviderException("Password format not supported.");
			}

			try
			{
				ConnectionString = WebConfigurationManager.ConnectionStrings[config["connectionString"]].ConnectionString;
			}
			catch (NullReferenceException)
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

		public override bool ChangePassword(string username, string oldPwd, string newPwd)
		{
			bool retval = false;
			if (ValidateUser(username, oldPwd))
			{
				ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPwd, true);
				OnValidatingPassword(args);

				if (args.Cancel)
				{
					if (args.FailureInformation != null)
					{
						throw args.FailureInformation;
					}
					else
					{
						throw new MembershipPasswordException("Change password canceled due to new password validation failure.");
					}
				}

				MongoUser user = null;
				var db = MongoDatabase.Create(ConnectionString);
				using (db.RequestStart())
				{
					var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
					user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);
					if (user == null)
					{
						throw new ProviderException("This user does not exist.");
					}
					user.Password = EncodePassword(newPwd);
					user.LastPasswordChangedDate = DateTime.Now;
					users.Save(user);
				}
				retval = true;
			}
			return retval;
		}

		public override bool ChangePasswordQuestionAndAnswer(string username,
					  string password,
					  string newPwdQuestion,
					  string newPwdAnswer)
		{
			bool retval = false;

			if (ValidateUser(username, password))
			{
				var db = MongoDatabase.Create(ConnectionString);
				using (db.RequestStart())
				{
					var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
					var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);
					user.PasswordQuestion = newPwdQuestion;
					user.PasswordAnswer = EncodePassword(newPwdAnswer);
					users.Save(user);
				}

				retval = true;
			}

			return retval;
		}

		public override MembershipUser CreateUser(string username,
				 string password,
				 string email,
				 string passwordQuestion,
				 string passwordAnswer,
				 bool isApproved,
				 object providerUserKey,
				 out MembershipCreateStatus status)
		{
			MembershipCreateStatus tempStatus = new MembershipCreateStatus();
			MembershipUser retval = null;

			ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
			OnValidatingPassword(args);

			if (args.Cancel)
			{
				tempStatus = MembershipCreateStatus.InvalidPassword;
			}
			else if (RequiresUniqueEmail && !string.IsNullOrWhiteSpace(GetUserNameByEmail(email)))
			{
				tempStatus = MembershipCreateStatus.DuplicateEmail;
			}
			else
			{
				MembershipUser user = GetUser(username, false);

				if (user == null)
				{
					ObjectId id = default(ObjectId);
					string key = providerUserKey != null ? providerUserKey.ToString() : null;
					if (!string.IsNullOrWhiteSpace(key) && !ObjectId.TryParse(key, out id))
					{
						tempStatus = MembershipCreateStatus.InvalidProviderUserKey;
					}
					else
					{
						var now = DateTime.Now;
						var newUser = new MongoUser()
						{
							_id = id,
							UserName = username,
							Password = EncodePassword(password),
							Email = email,
							PasswordQuestion = passwordQuestion,
							PasswordAnswer = passwordAnswer,
							IsApproved = isApproved,
							CreationDate = now,
							LastPasswordChangedDate = now,
							LastActivityDate = now,
							LastLockoutDate = now,
							FailedPasswordAttemptWindowStart = now,
							FailedPasswordAnswerAttemptWindowStart = now,
							Comment = string.Empty,
							ProviderName = this.Name
						};

						var db = MongoDatabase.Create(ConnectionString);
						using (db.RequestStart())
						{
							db.GetCollection<MongoUser>(typeof(MongoUser).Name).Insert(newUser);
						}

						retval = newUser.ToMembershipUser();
						tempStatus = MembershipCreateStatus.Success;
					}
				}
				else
				{
					tempStatus = MembershipCreateStatus.DuplicateUserName;
				}
			}

			status = tempStatus;
			return retval;
		}

		public override bool DeleteUser(string username, bool deleteAllRelatedData)
		{
			bool retval = false;

			var db = MongoDatabase.Create(ConnectionString);
			using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);
				if (user != null)
				{
					users.Remove(Query.EQ("_id", user._id));
					retval = true;

					if (deleteAllRelatedData)
					{
						try
						{
							Roles.RemoveUserFromRoles(username, Roles.GetRolesForUser(username));
						}
						catch (Exception ex)
						{
							Console.WriteLine("Something happened with the Role provider...you may or may not care...");
							Console.WriteLine(ex.Message);
						}

						try
						{
							ProfileManager.DeleteProfile(username);
						}
						catch (Exception ex)
						{
							Console.WriteLine("Something happened with the Profile provider...you may or may not care...");
							Console.WriteLine(ex.Message);
						}

						// TODO: Delete WebParts crap if anyone cares.
					}
				}
			}

			return retval;
		}

		public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
		{
			MembershipUserCollection users = new MembershipUserCollection();
			List<MongoUser> results = null;

			var db = MongoDatabase.Create(ConnectionString);
			using (db.RequestStart())
			{
				results = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().ToList();
			}

			if (pageSize == 0) pageSize = results.Count;

			int counter = 0;
			int startIndex = pageSize * pageIndex;
			int endIndex = startIndex + pageSize - 1;

			results.ForEach(u =>
			{
				if (counter >= startIndex && counter <= endIndex)
				{
					users.Add(u.ToMembershipUser());
					counter++;
				}
			});

			totalRecords = counter - startIndex;
			return users;
		}

		public override int GetNumberOfUsersOnline()
		{
			TimeSpan onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
			DateTime compareTime = DateTime.Now.Subtract(onlineSpan);
			int count = 0;
			var db = MongoDatabase.Create(ConnectionString);
			using (db.RequestStart())
			{
				count = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().Where(u => u.LastActivityDate > compareTime).Count();
			}
			return count;
		}

		public override string GetPassword(string username, string answer)
		{
			if (!EnablePasswordRetrieval)
			{
				throw new ProviderException("Password Retrieval Not Enabled.");
			}
			else if (PasswordFormat == MembershipPasswordFormat.Hashed)
			{
				throw new ProviderException("Cannot retrieve Hashed passwords.");
			}

			MongoUser user = null;
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				user = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().SingleOrDefault(u => u.UserName == username);
			}

			if (user == null)
			{
				throw new MembershipPasswordException("The supplied user name is not found.");
			}
			else if (user.IsLockedOut)
			{
				throw new MembershipPasswordException("The supplied user is locked out.");
			}
			else if (RequiresQuestionAndAnswer && !CheckPassword(answer, user.PasswordAnswer))
			{
				UpdateFailureCount(username, "passwordAnswer");
				throw new MembershipPasswordException("Incorrect password answer.");
			}

			return UnEncodePassword(user.Password);
		}

		public override MembershipUser GetUser(string username, bool userIsOnline)
		{
			MembershipUser retval = null;

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);

				if (user != null)
				{
					retval = user.ToMembershipUser();
					if (userIsOnline)
					{
						user.LastActivityDate = DateTime.Now;
						users.Save(user);
					}
				}
			}

			return retval;
		}

		public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
		{
			MembershipUser retval = null;
			ObjectId id = default(ObjectId);
			string key = providerUserKey.ToString();
			if (!string.IsNullOrWhiteSpace(key) && !ObjectId.TryParse(key, out id))
			{
				throw new Exception("The providerUserKey parameter cannot be converted to a valid ObjectID.");
			}

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u._id == id);

				if (user != null)
				{
					retval = user.ToMembershipUser();
					if (userIsOnline)
					{
						user.LastActivityDate = DateTime.Now;
						users.Save(user);
					}
				}
			}

			return retval;
		}

		public override bool UnlockUser(string username)
		{
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);
				user.IsLockedOut = false;
				users.Save(user);
			}
			return true;
		}

		public override string GetUserNameByEmail(string email)
		{
			string retval = null;
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var user = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().SingleOrDefault(u => u.Email == email);
				retval = user != null ? user.UserName : null;
			}
			return retval;
		}

		public override string ResetPassword(string username, string answer)
		{
			if (!EnablePasswordReset)
			{
				throw new ProviderException("Password Reset Not Enabled.");
			}
			else if (answer == null && RequiresQuestionAndAnswer)
			{
				UpdateFailureCount(username, "passwordAnswer");
				throw new ProviderException("Password answer required for password reset.");
			}

			string newPassword = Membership.GeneratePassword(NewPasswordLength, MinRequiredNonAlphanumericCharacters);

			ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
			OnValidatingPassword(args);
			if (args.Cancel)
			{
				if (args.FailureInformation != null)
				{
					throw args.FailureInformation;
				}
				else
				{
					throw new MembershipPasswordException("Reset password canceled due to password validation failure.");
				}
			}

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);

				if (string.IsNullOrWhiteSpace(user.UserName))
				{
					throw new MembershipPasswordException("The supplied user name is not found.");
				}
				else if (user.IsLockedOut)
				{
					throw new MembershipPasswordException("The supplied user is locked out.");
				}
				else if (RequiresQuestionAndAnswer && !CheckPassword(answer, user.PasswordAnswer))
				{
					UpdateFailureCount(username, "passwordAnswer");
					throw new MembershipPasswordException("Incorrect password answer.");
				}

				user.Password = EncodePassword(newPassword);
				user.LastPasswordChangedDate = DateTime.Now;
				users.Save(user);
			}

			return newPassword;
		}

		public override void UpdateUser(MembershipUser user)
		{
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var dbUser = users.AsQueryable().SingleOrDefault(u => u._id == ((ObjectId)(user.ProviderUserKey)));
				dbUser.Comment = user.Comment;
				dbUser.Email = user.Email;
				dbUser.UserName = user.UserName;
				dbUser.CreationDate = user.CreationDate;
				dbUser.IsApproved = user.IsApproved;
				dbUser.LastActivityDate = user.LastActivityDate;
				dbUser.LastLockoutDate = user.LastLockoutDate;
				dbUser.LastLoginDate = user.LastLoginDate;
				dbUser.LastPasswordChangedDate = user.LastPasswordChangedDate;

				users.Save(dbUser);
			}
		}

		public override bool ValidateUser(string username, string password)
		{
			bool isValid = false;

			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);

				if (user != null && !string.IsNullOrWhiteSpace(user.UserName) && user.IsApproved && !user.IsLockedOut)
				{
					if (CheckPassword(password, user.Password))
					{
						user.LastLoginDate = DateTime.Now;
						users.Save(user);
						isValid = true;
					}
					else
					{
						UpdateFailureCount(username, "password");
					}
				}
			}

			return isValid;
		}

		private void UpdateFailureCount(string username, string failureType)
		{
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				var users = db.GetCollection<MongoUser>(typeof(MongoUser).Name);
				var user = users.AsQueryable().SingleOrDefault(u => u.UserName == username);

				DateTime now = DateTime.Now;
				DateTime windowStart = new DateTime();
				int failureCount = 0;
				switch (failureType)
				{
					case "password":
						failureCount = user.FailedPasswordAttemptCount;
						windowStart = user.FailedPasswordAttemptWindowStart;
						break;
					case "passwordAnswer":
						failureCount = user.FailedPasswordAnswerAttemptCount;
						windowStart = user.FailedPasswordAnswerAttemptWindowStart;
						break;
				}
				DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

				if (failureCount == 0 || now > windowEnd)
				{
					// First password failure or outside of PasswordAttemptWindow. 
					// Start a new password failure count from 1 and a new window starting now.

					switch (failureType)
					{
						case "password":
							user.FailedPasswordAttemptCount++;
							user.FailedPasswordAttemptWindowStart = now;
							break;
						case "passwordAnswer":
							user.FailedPasswordAnswerAttemptCount++;
							user.FailedPasswordAnswerAttemptWindowStart = now;
							break;
					}
				}
				else if (failureCount++ >= MaxInvalidPasswordAttempts)
				{
					// Password attempts have exceeded the failure threshold. Lock out
					// the user.

					user.IsLockedOut = true;
					user.LastLockoutDate = now;
				}
				else
				{
					// Password attempts have not exceeded the failure threshold. Update
					// the failure counts. Leave the window the same.

					switch (failureType)
					{
						case "password":
							user.FailedPasswordAttemptCount++;
							break;
						case "passwordAnswer":
							user.FailedPasswordAnswerAttemptCount++;
							break;
					}
				}

				users.Save(user);
			}
		}

		private bool CheckPassword(string password, string dbpassword)
		{
			bool retval = false;

			string pass1 = password;
			string pass2 = dbpassword;

			switch (PasswordFormat)
			{
				case MembershipPasswordFormat.Encrypted:
					pass2 = UnEncodePassword(dbpassword);
					break;
				case MembershipPasswordFormat.Hashed:
					pass1 = EncodePassword(password);
					break;
				default:
					break;
			}

			if (pass1 == pass2)
			{
				retval = true;
			}

			return retval;
		}

		private string EncodePassword(string password)
		{
			string encodedPassword = password;

			switch (PasswordFormat)
			{
				case MembershipPasswordFormat.Clear:
					break;
				case MembershipPasswordFormat.Encrypted:
					encodedPassword = Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
					break;
				case MembershipPasswordFormat.Hashed:
					encodedPassword = MachineKey.Encode(Encoding.Unicode.GetBytes(password), MachineKeyProtection.Validation);
					break;
				default:
					throw new ProviderException("Unsupported password format.");
			}

			return encodedPassword;
		}

		private string UnEncodePassword(string encodedPassword)
		{
			string password = encodedPassword;

			switch (PasswordFormat)
			{
				case MembershipPasswordFormat.Clear:
					break;
				case MembershipPasswordFormat.Encrypted:
					password = Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
					break;
				case MembershipPasswordFormat.Hashed:
					password = Encoding.Unicode.GetString(MachineKey.Decode(encodedPassword, MachineKeyProtection.Validation));
					break;
				default:
					throw new ProviderException("Unsupported password format.");
			}

			return password;
		}

		public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			MembershipUserCollection users = new MembershipUserCollection();
			List<MongoUser> results = null;
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				results = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().Where(u => u.UserName == usernameToMatch).ToList();
			}

			int counter = 0;
			int startIndex = pageSize * pageIndex;
			int endIndex = startIndex + pageSize - 1;

			results.ForEach(u =>
			{
				if (counter >= startIndex && counter <= endIndex)
				{
					users.Add(u.ToMembershipUser());
					counter++;
				}
			});

			totalRecords = counter - startIndex;
			return users;
		}

		public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			MembershipUserCollection users = new MembershipUserCollection();
			List<MongoUser> results = null;
			var db = MongoDatabase.Create(ConnectionString); using (db.RequestStart())
			{
				results = db.GetCollection<MongoUser>(typeof(MongoUser).Name).AsQueryable().Where(u => u.Email == emailToMatch).ToList();
			}

			int counter = 0;
			int startIndex = pageSize * pageIndex;
			int endIndex = startIndex + pageSize - 1;

			results.ForEach(u =>
			{
				if (counter >= startIndex && counter <= endIndex)
				{
					users.Add(u.ToMembershipUser());
					counter++;
				}
			});

			totalRecords = counter - startIndex;
			return users;
		}

		private class MongoUser
		{
			public ObjectId _id
			{
				get;
				set;
			}

			public string UserName
			{
				get;
				set;
			}

			public string Password
			{
				get;
				set;
			}

			public string Email
			{
				get;
				set;
			}

			public bool IsApproved
			{
				get;
				set;
			}

			public DateTime CreationDate
			{
				get;
				set;
			}

			public DateTime LastActivityDate
			{
				get;
				set;
			}

			public DateTime LastPasswordChangedDate
			{
				get;
				set;
			}

			public string PasswordQuestion
			{
				get;
				set;
			}

			public string PasswordAnswer
			{
				get;
				set;
			}

			public bool IsLockedOut
			{
				get;
				set;
			}

			public DateTime LastLockoutDate
			{
				get;
				set;
			}

			public int FailedPasswordAttemptCount
			{
				get;
				set;
			}

			public DateTime FailedPasswordAttemptWindowStart
			{
				get;
				set;
			}

			public int FailedPasswordAnswerAttemptCount
			{
				get;
				set;
			}

			public DateTime FailedPasswordAnswerAttemptWindowStart
			{
				get;
				set;
			}

			public DateTime LastLoginDate
			{
				get;
				set;
			}

			public string ProviderName
			{
				get;
				set;
			}

			public string Comment
			{
				get;
				set;
			}

			public MembershipUser ToMembershipUser()
			{
				return new MembershipUser(
					ProviderName,
					UserName,
					_id,
					Email,
					PasswordQuestion,
					Comment,
					IsApproved,
					IsLockedOut,
					CreationDate,
					LastLoginDate,
					LastActivityDate,
					LastPasswordChangedDate,
					LastLockoutDate);
			}
		}
	}
}