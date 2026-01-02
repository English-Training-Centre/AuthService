using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace AuthService.src.Configs
{
    public static class SwaggerConfig
    {
        private const string apiTitle = "ETC - Auth Services";
        private const string apiUrl = "https://github.com/English-Training-Centre";

        public static void UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(op =>
            {
                var provider = app.ApplicationServices
                    .GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    op.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"{apiTitle} {description.GroupName.ToUpperInvariant()}"
                    );
                }

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
                using var sp = service.BuildServiceProvider();
                var provider = sp.GetRequiredService<IApiVersionDescriptionProvider>();

                ConfigureSwaggerDoc(op, provider);
                ConfigureJWTAuthentication(op);
            });
        }

        private static void ConfigureSwaggerDoc(
            SwaggerGenOptions options,
            IApiVersionDescriptionProvider provider)
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = apiTitle,
                    Version = description.ApiVersion.ToString(),
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