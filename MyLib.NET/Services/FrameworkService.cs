using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace MyLib.Services
{
   /// <summary>
   /// Provides an instance wrapper for all framework dependencies.
   /// NOTE: This class should only be wrapper methods for framework 
   /// dependencies - it should not have logic that needs unit testing.
   /// </summary>
   public interface IFrameworkService : IService
   {
      HttpContextBase Context { get; }
      MembershipProvider Membership { get; }
      RoleProvider Roles { get; }
      bool CheckAD(string username, string password);
      void Forms_Signout();
      void Forms_SetAuthCookie(string username, bool persist);
      string GetDomain(bool useHttps = false);
      string Forms_HashPassword(string password, FormsAuthPasswordFormat format);
   }

   public class FrameworkService : IFrameworkService
   {
      public HttpContextBase Context
      {
         get
         {
            return new HttpContextWrapper(HttpContext.Current);
         }
      }

      public MembershipProvider Membership
      {
         get
         {
            return System.Web.Security.Membership.Provider;
         }
      }

      public RoleProvider Roles
      {
         get
         {
            return System.Web.Security.Roles.Provider;
         }
      }

      public bool CheckAD(string username, string password)
      {
         using (var pc = new PrincipalContext(ContextType.Domain, ConfigurationManager.AppSettings["ADDomain"]))
            return pc.ValidateCredentials(username, password);
      }


      // FormsAuthentication static method wrappers.
      public void Forms_Signout()
      {
         FormsAuthentication.SignOut();
      }

      public void Forms_SetAuthCookie(string username, bool persist)
      {
         FormsAuthentication.SetAuthCookie(username, persist);
      }

      public string Forms_HashPassword(string password, FormsAuthPasswordFormat format)
      {
         return FormsAuthentication.HashPasswordForStoringInConfigFile(password, format.ToString());
      }


      public string GetDomain(bool useHttps = false)
      {
         string returnValue = useHttps ? "https://" : "http://";
         if (HttpContext.Current.Request.ServerVariables["SERVER_NAME"].ToLower() != "localhost")
         {
            returnValue += HttpContext.Current.Request.ServerVariables["SERVER_NAME"];
         }
         else
         {
            returnValue += "localhost:" + HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
         }
         return returnValue;
      }
   }
}