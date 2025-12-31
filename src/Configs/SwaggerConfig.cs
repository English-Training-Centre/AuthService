using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace AuthService.src.Configs
{
    public static class SwaggerConfig
    {
        private const string apiVersion = "v1";
        private const string apiTitle = "ETC - Auth Services";
        private const string apiUrl = "https://github.com/English-Training-Centre";

        public static void UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(op =>
            {
                op.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", $"{apiTitle} - {apiVersion}");
                op.RoutePrefix = string.Empty;
                op.DocumentTitle = apiTitle;
                op.DisplayRequestDuration();
                op.EnableFilter();
                op.EnableValidator();
                op.DisplayOperationId();
                op.DocExpansion(DocExpansion.None);
            });
        }

        public static void AddSwaggerConfiguration(this IServiceCollection service)
        {
            service.AddSwaggerGen(op =>
            {
                ConfigureSwaggerDoc(op);
                ConfigureJWTAuthentication(op);
            });
        }

        private static void ConfigureSwaggerDoc(SwaggerGenOptions op)
        {
            op.SwaggerDoc(apiVersion, new OpenApiInfo
            {
                Title = apiTitle,
                Version = apiVersion,
                Description = "English Training Centre",
                TermsOfService = new Uri(apiUrl),
                Contact = new OpenApiContact
                {
                    Name = "Ramadan Ismael",
                    Email = "ramadan.ismael02@gmail.com",
                    Url = new Uri(apiUrl)
                },
                License = new OpenApiLicense
                {
                    Name = "Apache 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                }
            });
        }

        private static void ConfigureJWTAuthentication(SwaggerGenOptions op)
        {
            op.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Name = "JWT Authorization",
                Description = "Enter JWT 'Bearer {token}' to access this API",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"                
            });

            op.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        }
    }
}