using Juice.Extensions.Swagger;
using Microsoft.Extensions.DependencyInjection;
#if NET6_0
using Microsoft.OpenApi.Models;
#else
using Microsoft.OpenApi;
#endif
namespace Juice.BgService.Api
{
    public static class BgServiceCollectionExtensions
    {
        /// <summary>
        /// Configure SwaggerGen for Background Service
        /// <para>bgservice</para>
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureBgServiceSwaggerGen(this IServiceCollection services)
        {
            services.ConfigureSwaggerGen(c =>
            {
                c.SwaggerDoc("bgservice", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Background Service API",
                    Description = "Provide background service control API"
                });

                c.IncludeReferencedXmlComments();
            });
            return services;
        }

    }
}
