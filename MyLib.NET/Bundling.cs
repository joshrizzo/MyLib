using dotless.Core;
using Microsoft.Web.Optimization;

namespace MyLib
{
	public class LessMinify : CssMinify
	{
		public LessMinify() { }

		public override void Process(BundleResponse response)
		{
			response.Content = Less.Parse(response.Content);
			base.Process(response);
		}
	}
}
