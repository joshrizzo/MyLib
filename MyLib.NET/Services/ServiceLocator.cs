
using System;
using System.Collections.Generic;
namespace MyLib.Services
{
	public interface IService
	{

	}

	public interface IServiceLocator
	{
		T GetService<T>() where T : IService;
	}

	public static class ServiceLocator
	{
		public static IServiceLocator Instance { get; set; }

		/// <summary>
		/// Used to get a service dependency.
		/// </summary>
		/// <typeparam name="T">The interface for the service that you need.</typeparam>
		/// <returns>
		/// <![CDATA[
		/// public IFrameworkService framework { get { return ServiceLocator.Get<IFrameworkService>(); } }
		/// ]]>
		/// </returns>
		public static T Get<T>() where T : IService
		{
			return Instance.GetService<T>();
		}
	}

	/// <summary>
	/// Used to locate services for an application instance, given a specific service interface.
	/// Should be setup when an application starts.
	/// </summary>
	/// <example>
	/// <![CDATA[
	/// var serviceLocator = new MyServiceLocator();
	/// serviceLocator.Services.Add(typeof(IFrameworkService), new FrameworkService());
	/// ServiceLocator.Instance = serviceLocator;
	/// ]]>
	/// </example>
	public class MyServiceLocator : IServiceLocator
	{
		public IDictionary<Type, IService> Services = new Dictionary<Type, IService>();

		public T GetService<T>() where T : IService
		{
			try
			{
				return (T)Services[typeof(T)];
			}
			catch (KeyNotFoundException)
			{
				throw new ApplicationException("The requested service is not registered");
			}
		}
	}
}
