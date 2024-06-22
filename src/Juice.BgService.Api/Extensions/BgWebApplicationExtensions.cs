using Microsoft.AspNetCore.Builder;

namespace Juice.BgService.Api
{
    public static class BgWebApplicationExtensions
    {
        /// <summary>
        /// Mapping Swagger UI for Background Service with endpoint /bgservice/swagger
        /// </summary>
        /// <param name="app"></param>
        public static void UseBgServiceSwaggerUI(this IApplicationBuilder app)
        {
            app.UseSwagger(options => options.RouteTemplate = "bgservice/swagger/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("bgservice-v1/swagger.json", "Background Service API V1");
                c.RoutePrefix = "bgservice/swagger";
            });
        }

    }
}
