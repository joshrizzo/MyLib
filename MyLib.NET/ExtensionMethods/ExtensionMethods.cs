using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using AutoMapper;
using MyLib.Services;

public static class ExtensionMethods
{
   public static IEnumerable<SelectListItem> ToSelectList(this Enum enumObj, Enum Selected)
   {
      var values = from Enum e in Enum.GetValues(enumObj.GetType())
                   select new SelectListItem()
                   {
                      Text = e.ToString().CammelCaseToTitleCase(),
                      Value = e.ToString(),
                      Selected = e == Selected
                   };

      return values;
   }

   public static bool IsNew(this IRepoData data)
   {
      return data.Id.IsDefault();
   }

   #region Strings

   public static string GetMd5Sum(this string str)
   {
      // First we need to convert the string into bytes, which
      // means using a text encoder.
      Encoder enc = System.Text.Encoding.Unicode.GetEncoder();

      // Create a buffer large enough to hold the string
      byte[] unicodeText = new byte[str.Length * 2];
      enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

      // Now that we have a byte array we can ask the CSP to hash it
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] result = md5.ComputeHash(unicodeText);

      // Build the final string by converting each byte
      // into hex and appending it to a StringBuilder
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < result.Length; i++)
      {
         sb.Append(result[i].ToString("X2"));
      }

      // And return it
      return sb.ToString();
   }

   public static string ToCommaDelimitedString(this string[] thisString)
   {
      if (thisString.Length == 0)
      {
         return string.Empty;
      }
      else if (thisString.Length == 1)
      {
         return thisString[0];
      }
      else if (thisString.Length == 2)
      {
         return thisString[0] + " and " + thisString[1];
      }
      else
      {
         string delimiter = ", ";
         string retval = thisString.Join(delimiter);
         return retval.Insert(retval.LastIndexOf(delimiter) + delimiter.Length, "and ");
      }
   }

   public static string[] Trim(this string[] strings)
   {
      strings.ForEach(s => s = s.Trim());
      return strings;
   }

   public static string[] ToLower(this string[] strings)
   {
      strings.ForEach(s => s = s.ToLower());
      return strings;
   }

   public static string CammelCaseToTitleCase(this string cammelCase)
   {
      if (cammelCase.IsEmptyOrWhiteSpace())
         return string.Empty;
      else
      {
         var matches = Regex.Matches(cammelCase, "[A-Z][a-z]+");
         if (matches.Count > 0)
         {
            return matches
            .OfType<Match>()
            .Select(match => match.Value)
            .Aggregate((acc, b) => acc + " " + b)
            .TrimStart(' ');
         }
         else return cammelCase;
      }
   }

   public static string GetSQLConnectionString(this string entityFrameworkConnectionString)
   {
      var regex = new Regex(@"provider connection string='(?<val>.+)'");
      string sqlConnStr = regex.Match(entityFrameworkConnectionString).Groups["val"].Value;
      return sqlConnStr;
   }

   public static T ParseAsEnum<T>(this string value) // where T : Enum
   {
      if (string.IsNullOrEmpty(value))
         throw new ArgumentNullException("Can't parse an empty string.");

      Type enumType = typeof(T);
      if (!enumType.IsEnum)
         throw new InvalidOperationException("T must be an enum.");

      return Enum.Parse(enumType, value, true).CastTo<T>();
   }

   #endregion

   #region Object

   public static T ChangeType<T>(this object obj)
   {
      return (T)Convert.ChangeType(obj, typeof(T));
   }

   public static string WriteToHTML(this object obj)
   {
      StringBuilder content = new StringBuilder();
      if (obj is IEnumerable)
      {
         foreach (object o in ((IEnumerable)(obj)))
         {
            content.Append(o.WriteToHTML());
         }
      }
      else
      {
         string className = TypeDescriptor.GetClassName(obj);
         content.AppendLine("<br /><br />");
         content.Append("<h1>");
         content.Append(className.Substring(className.LastIndexOf('.') + 1).CammelCaseToTitleCase());
         content.AppendLine("</h1>");
         content.AppendLine("<table>");
         foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
         {
            string name = descriptor.Name.CammelCaseToTitleCase();
            object val = descriptor.GetValue(obj);
            content.AppendLine("<tr>");
            content.Append("<td><b>");
            content.Append(name);
            content.AppendLine(":</b></td>");
            content.Append("<td>");
            content.Append(val != null ? val.ToString() : string.Empty);
            content.AppendLine("</td>");
            content.AppendLine("</tr>");
         }
         content.AppendLine("</table>");

         foreach (FieldInfo field in obj.GetType().GetFields())
         {
            content.Append(field.GetValue(obj).WriteToHTML());
         }
      }
      return content.ToString();
   }

   public static bool IsNullable(this object obj)
   {
      Type t = obj.GetType();
      return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
   }

   /// <summary>
   /// Returns the value of the object or a default if the object is null.
   /// </summary>
   public static T DefaultIfNull<T>(this T obj)
   {
      return (obj.IsNullable() && obj == null) ? default(T) : obj;
   }

   public static bool IsNotDefault<T>(this T obj)
   {
      return !Equals(obj, default(T));
   }

   public static bool IsDefault<T>(this T obj)
   {
      return Equals(obj, default(T));
   }

   public static bool IsNotNullOrDefault<T>(this T obj)
   {
      return obj.IsNotNull() && obj.IsNotDefault();
   }

   public static bool IsNullOrDefault<T>(this T obj)
   {
      return obj.IsNull() || obj.IsDefault();
   }

   #endregion

   #region Controller

   /// <summary>
   /// Returns a rendered view as a string.
   /// </summary>
   /// <param name="controller">The MVC  Controller making the View call.</param>
   /// <param name="viewName">The view that you want to render.</param>
   /// <param name="model">The model to pass to the view.</param>
   /// <returns>Returns the specified view, rendered as a string.</returns>
   public static string RenderPartialViewToString(this Controller controller, string viewName, object model)
   {
      controller.ViewData.Model = model;
      try
      {
         using (StringWriter sw = new StringWriter())
         {
            ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
            ViewContext viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
            viewResult.View.Render(viewContext, sw);

            return sw.GetStringBuilder().ToString();
         }
      }
      catch (Exception ex)
      {
         return ex.ToString();
      }
   }

   public static string GetView(this RouteData routeData, string fileType = "cshtml")
   {
      var action = string.Empty;
      try { action = routeData.GetRequiredString("action"); }
      catch (Exception) { }

      var controller = string.Empty;
      try { controller = routeData.GetRequiredString("controller"); }
      catch (Exception) { }

      var area = string.Empty;
      try { area = routeData.GetRequiredString("area"); }
      catch (Exception) { }

      if (action.IsEmptyOrWhiteSpace()) action = "Index";

      if (area.IsNotEmptyOrWhiteSpace())
         return "~/Areas/{0}/Views/{1}/{2}.{3}".FormatWith(area, controller, action, fileType);
      else
         return "~/Views/{0}/{1}.{3}".FormatWith(controller, action, fileType);
   }

   /// <summary>
   /// Used as an alternative to returning the View() method on MVC Controller Actions.
   /// Renders an MVC Razor View using a Web Forms Master Page.
   /// </summary>
   /// <param name="viewName">The View that you want to render (leave blank or null for the default View).</param>
   /// <param name="model">The model to pass to the view.</param>
   /// <returns>A ViewResult for use in an MVC Controller Action.</returns>
   /// <remarks>
   /// Include this line in a Web Form Content Page named RazorView.aspx: 
   /// <![CDATA[<% Html.RenderPartial((string)ViewBag._ViewName); %>]]>
   /// </remarks>
   public static ViewResult RazorMasterPageView(this Controller controller, string razorRenderingView, string viewName = null, object model = null)
   {
      if (model != null) controller.ViewData.Model = model;
      controller.ViewBag._ViewName = viewName.IsNotEmptyOrWhiteSpace() ? viewName : controller.RouteData.GetView();
      var razorView = new WebFormView(controller.ControllerContext, razorRenderingView);

      return new ViewResult
      {
         View = razorView,
         ViewData = controller.ViewData,
         TempData = controller.TempData
      };
   }

   /// <summary>
   /// Gets the HTML Helper object normally available in associated Views.
   /// </summary>
   /// <returns></returns>
   public static HtmlHelper GetHtmlHelper(this Controller controller)
   {
      var viewContext = new ViewContext(controller.ControllerContext, new FakeView(), controller.ViewData, controller.TempData, TextWriter.Null);
      return new HtmlHelper(viewContext, new ViewPage());
   }

   private class FakeView : IView
   {
      public void Render(ViewContext viewContext, TextWriter writer)
      {
         throw new InvalidOperationException();
      }
   }

   #endregion

   #region DateTime

   public static DateTime DropMillisecods(this DateTime dateTime)
   {
      return new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond), dateTime.Kind);
   }

   public static DateTime? ToLocalTime(this DateTime? dateTime)
   {
      if (dateTime.HasValue)
         return dateTime.Value.ToLocalTime();
      else
         return null;
   }

   public static DateTime? ToUniversalTime(this DateTime? dateTime)
   {
      if (dateTime.HasValue)
         return dateTime.Value.ToUniversalTime();
      else
         return null;
   }

   #endregion

   #region AutoMapper

   public static IMappingExpression<TSource, TDestination> IgnoreAllNonExisting<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
   {
      var sourceType = typeof(TSource);
      var destinationType = typeof(TDestination);
      var existingMaps = Mapper.GetAllTypeMaps().First(x => x.SourceType.Equals(sourceType)
         && x.DestinationType.Equals(destinationType));
      foreach (var property in existingMaps.GetUnmappedPropertyNames())
      {
         expression.ForMember(property, opt => opt.Ignore());
      }
      return expression;
   }

   public static IMappingExpression<TSource, TDestination> IgnoreNullValues<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
   {
      var sourceType = typeof(TSource);
      var destinationType = typeof(TDestination);
      var existingMaps = Mapper.GetAllTypeMaps().First(x => x.SourceType.Equals(sourceType)
         && x.DestinationType.Equals(destinationType));
      foreach (var propertyMap in existingMaps.GetPropertyMaps())
      {
         propertyMap.ApplyCondition(a => !a.IsSourceValueNull);
      }
      return expression;
   }

   #endregion

   #region Uri

   public static Uri SetQueryParam(this Uri uri, string key, string value)
   {
      return new UriBuilder(uri).SetQueryParam(key, value).Uri;
   }

   /// <summary>
   /// Sets the specified query parameter key-value pair of the URI.
   /// If the key already exists, the value is overwritten.
   /// </summary>
   public static UriBuilder SetQueryParam(this UriBuilder uri, string key, string value)
   {
      var collection = uri.ParseQuery();

      // add (or replace existing) key-value pair
      collection.Set(key, value);

      string query = collection
         .AsKeyValuePairs()
         .ToConcatenatedString(pair =>
            pair.Key == null
            ? pair.Value
            : pair.Key + "=" + pair.Value, "&");

      uri.Query = query;

      return uri;
   }

   /// <summary>
   /// Gets the query string key-value pairs of the URI.
   /// Note that the one of the keys may be null ("?123") and
   /// that one of the keys may be an empty string ("?=123").
   /// </summary>
   public static IEnumerable<KeyValuePair<string, string>> GetQueryParams(this UriBuilder uri)
   {
      return uri.ParseQuery().AsKeyValuePairs();
   }

   /// <summary>
   /// Converts the legacy NameValueCollection into a strongly-typed KeyValuePair sequence.
   /// </summary>
   static IEnumerable<KeyValuePair<string, string>> AsKeyValuePairs(this NameValueCollection collection)
   {
      foreach (string key in collection.AllKeys)
      {
         yield return new KeyValuePair<string, string>(key, collection.Get(key));
      }
   }

   /// <summary>
   /// Parses the query string of the URI into a NameValueCollection.
   /// </summary>
   static NameValueCollection ParseQuery(this UriBuilder uri)
   {
      return HttpUtility.ParseQueryString(uri.Query);
   }

   #endregion

   #region IEnumerable

   /// <summary>
   /// Creates a string from the sequence by concatenating the result
   /// of the specified string selector function for each element.
   /// </summary>
   public static string ToConcatenatedString<T>(this IEnumerable<T> source, Func<T, string> stringSelector)
   {
      return source.ToConcatenatedString(stringSelector, String.Empty);
   }

   /// <summary>
   /// Creates a string from the sequence by concatenating the result
   /// of the specified string selector function for each element.
   /// </summary>
   ///<param name="separator">The string which separates each concatenated item.</param>
   public static string ToConcatenatedString<T>(this IEnumerable<T> source, Func<T, string> stringSelector, string separator)
   {
      var b = new StringBuilder();
      bool needsSeparator = false; // don't use for first item

      foreach (var item in source)
      {
         if (needsSeparator)
            b.Append(separator);

         b.Append(stringSelector(item));
         needsSeparator = true;
      }

      return b.ToString();
   }

   #endregion
}
