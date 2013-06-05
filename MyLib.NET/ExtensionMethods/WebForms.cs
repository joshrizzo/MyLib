using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;

namespace MyLib.ExtensionMethods
{
	public static class WebFormsExtensions
	{
		public static void RenderPartial(this MasterPage page, string partialName, object model = null)
		{
			RenderPartial(partialName, model, HttpContext.Current.Response.Output);
		}

		public static string RenderPartialToString(this MasterPage page, string partialName, object model = null)
		{
			StringBuilder sb = new StringBuilder();
			using (StringWriter sw = new StringWriter(sb))
			{
				using (HtmlTextWriter tw = new HtmlTextWriter(sw))
				{
					RenderPartial(partialName, model, tw);
				}
			}

			return sb.ToString();
		}

		public static void RenderPartial(this Page page, string partialName, object model = null)
		{
			RenderPartial(partialName, model, HttpContext.Current.Response.Output);
		}

		public static string RenderPartialToString(this Page page, string partialName, object model = null)
		{
			StringBuilder sb = new StringBuilder();
			using (StringWriter sw = new StringWriter(sb))
			{
				using (HtmlTextWriter tw = new HtmlTextWriter(sw))
				{
					RenderPartial(partialName, model, tw);
				}
			}

			return sb.ToString();
		}

		public class WebFormController : Controller { }
		private static void RenderPartial(string partialName, object model, TextWriter output)
		{
			//get a wrapper for the legacy WebForm context
			var httpCtx = new HttpContextWrapper(HttpContext.Current);

			//create a mock route that points to the empty controller
			var rt = new RouteData();
			rt.Values.Add("controller", "WebFormController");

			//create a controller context for the route and http context
			var ctx = new ControllerContext(
				new RequestContext(httpCtx, rt), new WebFormController());

			//find the partial view using the view-engine
			var view = ViewEngines.Engines.FindPartialView(ctx, partialName).View;

			//create a view context and assign the model
			var vctx = new ViewContext(ctx, view,
				new ViewDataDictionary { Model = model },
				new TempDataDictionary(),
				httpCtx.Response.Output);

			//render the partial view
			view.Render(vctx, output);
		}
	}
}