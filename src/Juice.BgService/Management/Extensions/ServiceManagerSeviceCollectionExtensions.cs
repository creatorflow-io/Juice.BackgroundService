using Juice.BgService.Builder;
using Juice.BgService.Management.File;
using Juice.Extensions.Configuration;
using Juice.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.BgService.Management
{
    public static class ServiceManagerSeviceCollectionExtensions
    {
        public static BgServiceBuilder<ServiceModel> AddBgService(this IServiceCollection services, IConfigurationSection configuration)
            => services.AddBgService<ServiceModel>(configuration);

        public static BgServiceBuilder<TModel> AddBgService<TModel>(this IServiceCollection services, IConfigurationSection configuration)
            where TModel : class, IServiceModel
        {
            services.ConfigureMutable<ServiceManagerOptions>(configuration);

            services.AddSingleton<ServiceManager<TModel>>();

            services.AddSingleton<IServiceManager>(sp => sp.GetRequiredService<ServiceManager<TModel>>());

            services.AddSingleton<ServiceFactory>();

            services.AddHostedService(sp => sp.GetRequiredService<ServiceManager<TModel>>());

            return new BgServiceBuilder<TModel>(services, configuration);
        }

        public static BgServiceBuilder<TModel> UseFileStore<TModel>(this BgServiceBuilder<TModel> builder,
            IConfigurationSection configuration)
            where TModel : class, IServiceModel
        {
            builder.Services.ConfigureMutable<FileStoreOptions<TModel>>(configuration);
            builder.Services.AddSingleton<IServiceRepository<TModel>, FileStore<TModel>>();

            return builder;
        }

        /// <summary>
        /// Registers a typed factory for a specific background service type <typeparamref name="TService"/>.
        /// <see cref="ServiceFactory"/> will delegate instantiation of <typeparamref name="TService"/>
        /// to <typeparamref name="TFactory"/> instead of using the default reflection-based path.
        /// </summary>
        public static IServiceCollection AddServiceFactory<TService, TFactory>(this IServiceCollection services)
            where TService : class, IManagedService
            where TFactory : class, IServiceFactory<TService>
        {
            services.AddSingleton<IServiceFactory<TService>, TFactory>();
            return services;
        }

        public static void SeparateStoreFile<TModel>(this BgServiceBuilder<TModel> builder, string name, IConfigurationBuilder configuration,
            string? env = default)
            where TModel : class, IServiceModel
        {
            if (string.IsNullOrEmpty(env))
            {

                builder.Services.UseOptionsMutableFileStore<FileStoreOptions<TModel>>($"appsettings.{name}.json");
                configuration.AddJsonFile($"appsettings.{name}.json");
            }
            else
            {
                builder.Services.UseOptionsMutableFileStore<FileStoreOptions<TModel>>($"appsettings.{name}.{env}.json");
                configuration.AddJsonFile($"appsettings.{name}.{env}.json");
            }
        }
    }
}
