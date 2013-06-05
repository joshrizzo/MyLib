using System.Web.Mvc;

namespace MyLib
{
	/// <summary>
	/// Allows you to change the default directory of MVC Views.
	/// </summary>
	/// <example>
	/// ViewEngines.Engines.Clear();
	/// ViewEngines.Engines.Add(new CustomViewEngine("~/MyMVC/Views"));
	/// </example>
	public class CustomViewEngine : RazorViewEngine
	{
		public CustomViewEngine(string viewDirectory)
		{
			this.MasterLocationFormats = new string[] { 
				viewDirectory + "/Shared/{0}.cshtml", 
				viewDirectory + "/Shared/{0}.vbhtml", 
				viewDirectory + "/Shared/{0}.aspx", 
				viewDirectory + "/Shared/{0}.ascx" };

			this.ViewLocationFormats = new string[] { 
				viewDirectory + "/{1}/{0}.cshtml", 
				viewDirectory + "/{1}/{0}.vbhtml", 
				viewDirectory + "/{1}/{0}.aspx", 
				viewDirectory + "/{1}/{0}.ascx", 
				viewDirectory + "/Shared/{0}.cshtml", 
				viewDirectory + "/Shared/{0}.vbhtml", 
				viewDirectory + "/Shared/{0}.aspx", 
				viewDirectory + "/Shared/{0}.ascx" };

			this.PartialViewLocationFormats = new string[] { 
				viewDirectory + "/{1}/{0}.cshtml", 
				viewDirectory + "/{1}/{0}.vbhtml", 
				viewDirectory + "/{1}/{0}.aspx", 
				viewDirectory + "/{1}/{0}.ascx", 
				viewDirectory + "/Shared/{0}.cshtml", 
				viewDirectory + "/Shared/{0}.vbhtml", 
				viewDirectory + "/Shared/{0}.aspx", 
				viewDirectory + "/Shared/{0}.ascx" };
		}
	}
}
