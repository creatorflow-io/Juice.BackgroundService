using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.BgService.Builder
{
    public class BgServiceBuilder<TModel>
        where TModel : class, IServiceModel
    {
        public BgServiceBuilder(IServiceCollection services, IConfigurationSection configuration)
        {
            Services = services;
            Configuration = configuration;
        }
        public IServiceCollection Services { get; }
        public IConfigurationSection Configuration { get; }
    }
}
