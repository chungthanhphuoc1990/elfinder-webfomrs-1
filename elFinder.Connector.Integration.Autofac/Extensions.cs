using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using elFinder.Connector.Command;
using elFinder.Connector.Config;
using elFinder.Connector.Service;

namespace elFinder.Connector.Integration.Autofac
{
	public static class Extensions
	{
		public static void RegisterElFinderConnectorDefault( this ContainerBuilder builder )
		{
			builder.RegisterElFinderConnectorCore();

			builder.RegisterElFinderConnectorServices<DefaultVolumeManager, DefaultImageEditorService, Base64CryptoService>( AppConnectorConfig.Instance );
		}

		public static void RegisterElFinderConnectorCore( this ContainerBuilder builder )
		{
			builder.RegisterAssemblyTypes( typeof( ICommand ).Assembly )
				.Where( t => t.IsAssignableTo<ICommand>() && !t.IsAbstract )
				.AsImplementedInterfaces()
				.InstancePerLifetimeScope();

			builder.RegisterAssemblyTypes( typeof( IVolume ).Assembly )
				.Where( t => t.IsAssignableTo<IVolume>() && !t.IsAbstract )
				.AsImplementedInterfaces()
				.InstancePerDependency();
		}

		public static void RegisterElFinderConnectorServices<TConfig, TVolumeManager,TImageEditorService,TCryptoService>( this ContainerBuilder builder )
			where TConfig : IConnectorConfig
			where TVolumeManager : IVolumeManager
			where TImageEditorService : IImageEditorService
			where TCryptoService : ICryptoService
		{
			builder.RegisterType<TConfig>()
				.As<IConnectorConfig>().SingleInstance();

			builder.RegisterType<TVolumeManager>()
				.As<IVolumeManager>().SingleInstance();

			builder.RegisterType<TImageEditorService>()
				.As<IImageEditorService>().SingleInstance();

			builder.RegisterType<TCryptoService>()
				.As<ICryptoService>().SingleInstance();
		}

		public static void RegisterElFinderConnectorServices<TVolumeManager, TImageEditorService, TCryptoService>( this ContainerBuilder builder, IConnectorConfig configInstance )
			where TVolumeManager : IVolumeManager
			where TImageEditorService : IImageEditorService
			where TCryptoService : ICryptoService
		{
			builder.Register( c => configInstance )
				.As<IConnectorConfig>().SingleInstance();

			builder.RegisterType<TVolumeManager>()
				.As<IVolumeManager>().SingleInstance();

			builder.RegisterType<TImageEditorService>()
				.As<IImageEditorService>().SingleInstance();

			builder.RegisterType<TCryptoService>()
				.As<ICryptoService>().SingleInstance();
		}

		public static void SetAsElFinderDependencyResolver( this IContainer contaner )
		{
			DependencyResolver.SetResolver( new AutofacDependencyResolver( contaner ) );
		}
	}
}
