using Juice.Extensions.Swagger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

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
