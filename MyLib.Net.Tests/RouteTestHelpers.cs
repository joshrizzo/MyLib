using System.Web;
using Moq;

namespace MyLib.TestHelpers
{
	public static class RouteTestHelpers
	{
		/// <summary>
		/// Use this to test MVC routing.
		/// </summary>
		/// <param name="url">The URl to request.</param>
		/// <example><code>
		/// var routes = new RouteCollection();
		/// MvcApplication.RegisterRoutes(routes);
		/// var mockHttpContext = BuildMockHttpContext(url);
		/// return routes.GetRouteData(mockHttpContext.Object);
		/// </code></example>
		public static Mock<HttpContextBase> BuildMockHttpContext(string url)
		{
			// Create a mock instance of the HttpContext class.
			var mockHttpContext = new Mock<HttpContextBase>();

			// Decorate our mock object with the desired behavior.
			var mockRequest = new Mock<HttpRequestBase>();
			mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
			mockRequest.Setup(x => x.AppRelativeCurrentExecutionFilePath).Returns(url);

			var mockResponse = new Mock<HttpResponseBase>();
			mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
			mockResponse.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(x => x);

			return mockHttpContext;
		}
	}
}
