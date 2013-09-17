using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Autofac;
using elFinder.Connector.Integration.Autofac;

namespace elFinder.WebTest
{
	public class Global : System.Web.HttpApplication
	{
		private static IContainer _container;

		void Application_Start( object sender, EventArgs e )
		{
			// Code that runs on application startup

			// register IoC
			var builder = new ContainerBuilder();
			builder.RegisterElFinderConnectorDefault();
			_container = builder.Build();
			// need also to set container in elFinder module
			_container.SetAsElFinderDependencyResolver();
		}

		void Application_End( object sender, EventArgs e )
		{
			//  Code that runs on application shutdown

		}

		void Application_Error( object sender, EventArgs e )
		{
			// Code that runs when an unhandled error occurs

		}

		void Session_Start( object sender, EventArgs e )
		{
			// Code that runs when a new session is started

		}

		void Session_End( object sender, EventArgs e )
		{
			// Code that runs when a session ends. 
			// Note: The Session_End event is raised only when the sessionstate mode
			// is set to InProc in the Web.config file. If session mode is set to StateServer 
			// or SQLServer, the event is not raised.

		}

	}
}
