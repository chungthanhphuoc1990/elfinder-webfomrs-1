using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;

namespace elFinder.Connector
{
	public static class Extensions
	{
		public static void RegisterElFinderConnector( this ContainerBuilder builder )
		{
			builder.RegisterAssemblyTypes( typeof( Command.ICommand ).Assembly )
							.Where( t => t.IsAssignableTo<Command.ICommand>() && !t.IsAbstract )
							.AsImplementedInterfaces()
							.InstancePerLifetimeScope();

			builder.Register( c => Config.AppConnectorConfig.Instance )
				.As<Config.IConnectorConfig>().SingleInstance();

			builder.RegisterType<Service.DefaultVolumeManager>()
				.As<Service.IVolumeManager>().SingleInstance();

			builder.RegisterType<Service.Base64CryptoService>()
				.As<Service.ICryptoService>().SingleInstance();

			builder.RegisterAssemblyTypes( typeof( Service.IVolume ).Assembly )
				.Where( t => t.IsAssignableTo<Service.IVolume>() && !t.IsAbstract )
				.AsImplementedInterfaces()
				.InstancePerDependency();
		}

		public static void SetAsElFinderResolver( this IContainer contaner )
		{
			Connector.Container = contaner;
		}
	}
}
