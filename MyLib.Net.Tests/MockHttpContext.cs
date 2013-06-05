using System.Web;
using Moq;

namespace MyLib.Test
{
	public static class MockHttpContextHelper
	{
		public static Mock<HttpContextBase> HttpContext(string url)
		{
			var mockContext = new Mock<HttpContextBase>();
			var mockRequest = new Mock<HttpRequestBase>();
			var mockResponse = new Mock<HttpResponseBase>();

			mockContext.Setup(a => a.Request).Returns(mockRequest.Object);
			mockRequest.Setup(a => a.AppRelativeCurrentExecutionFilePath).Returns(url);
			mockContext.Setup(a => a.Cache).Returns(HttpRuntime.Cache);
			mockContext.Setup(a => a.Response).Returns(mockResponse.Object);
			mockResponse.Setup(a => a.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(a => a);

			return mockContext;
		}
	}
}
