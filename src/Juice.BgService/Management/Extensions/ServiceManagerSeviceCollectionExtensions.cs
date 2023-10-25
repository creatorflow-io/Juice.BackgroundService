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

            services.AddSingleton<IServiceFactory, ServiceFactory>();

            services.AddHostedService(sp => sp.GetRequiredService<ServiceManager<TModel>>());

            return new BgServiceBuilder<TModel>(services, configuration);
        }

        public static BgServiceBuilder<TModel> UseFileStore<TModel>(this BgServiceBuilder<TModel> builder,
            IConfigurationSection configuration)
            where TModel : class, IServiceModel
        {
            builder.Services.ConfigureMutable<FileStoreOptions<TModel>>(configuration,
                options =>
                {
                    var cfg = configuration.GetScalaredConfig<FileStoreOptions<TModel>>();
                    if (cfg != null)
                    {
                        options.Services = cfg.Services;
                    }
                });
            builder.Services.AddSingleton<IServiceRepository<TModel>, FileStore<TModel>>();

            return builder;
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
