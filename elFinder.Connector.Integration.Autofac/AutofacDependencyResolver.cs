using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using elFinder.Connector.Service;

namespace elFinder.Connector.Integration.Autofac
{
	public class AutofacDependencyResolver : IDependencyResolver
	{
		private readonly IContainer _container;

		#region IDependencyResolver Members

		public TService Resolve<TService>()
		{
			return _container.Resolve<TService>();
		}

		public IDisposable BeginResolverScope()
		{
			return _container.BeginLifetimeScope();
		}

		#endregion

		public AutofacDependencyResolver( IContainer container )
		{
			_container = container;
		}
	}
}
