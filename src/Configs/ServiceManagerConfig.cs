using System.Threading.RateLimiting;
using AuthService.src.DB;
using AuthService.src.Interfaces;
using AuthService.src.Repositories;
using AuthService.src.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Npgsql;

namespace AuthService.src.Configs
{
    public static class ServiceManagerConfig
    {
        public static void Configure(IServiceCollection service, IConfiguration configuration)
        {
            var logger = LoggerFactory.Create(s => s.AddConsole()).CreateLogger<Program>();

            try
            {
                service.AddOpenApi();
                service.AddEndpointsApiExplorer();
                service.AddSwaggerConfiguration();
                service.AddJWTAuthentication(configuration, logger);
                service.AddHttpContextAccessor();

                // FluentValidation
                service.AddValidatorsFromAssembly(typeof(Program).Assembly);
                service.AddControllers().Services.AddFluentValidationAutoValidation();
                // end

                service.AddSingleton<NpgsqlDataSource>(sp =>
                {
                    var builder = new NpgsqlDataSourceBuilder(
                        PostgresDB.BuildConnectionStringFromEnvironment());

                    builder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());

                    return builder.Build();
                });

                service.AddHealthChecks()
                    .AddNpgSql(sp => sp.GetRequiredService<NpgsqlDataSource>());

                service.AddHttpClient<IUserServices, UserServices>(client =>
                {
                    client.BaseAddress = new Uri(configuration["ServicesUrl:UserService"] ?? "http://localhost:5116/");
                    client.Timeout = TimeSpan.FromSeconds(5);
                });

                service.AddScoped<IPostgresDbData, PostgresDB>();
                service.AddScoped<IAuthRepository, AuthRepository>();

                // Rate limitting: Sliding Windows
                service.AddRateLimiter(op =>
                {
                    // 1. Global: 100 requests in any 60-second window (per user or IP)
                    op.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    {
                        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: ip,
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 300, //Each IP: 300 req / 60 sec
                                Window = TimeSpan.FromSeconds(60),
                                SegmentsPerWindow = 6,
                                AutoReplenishment = true
                            }
                        );
                    });

                    // 2. Login Limit - Per Username (or IP if anonymous)
                    op.AddPolicy("SignInPolicy", httpContext =>
                    {
                        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"sigin-{ip}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromSeconds(60),
                                SegmentsPerWindow = 6,
                                AutoReplenishment = true,
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst                                
                            }
                        );                        
                    });

                    // 3. Failed Response Handling
                    op.OnRejected = async (context, token) =>
                    {
                        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        {
                            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                            await context.HttpContext.Response.WriteAsync($"Too many requests. Retry in {retryAfter.TotalSeconds:F0} seconds.", token);
                        }
                        else
                        {
                            await context.HttpContext.Response.WriteAsync("Too many requests.", token);
                        }

                        // Log abuse
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Rate limited: {IP} on {Path} : ", context.HttpContext.Connection.RemoteIpAddress, context.HttpContext.Request.Path);
                    };                    
                });

                string? audience = configuration["JWTSettings:validAudience"];
                if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("JWTSettings:validAudience is missing in configuration.");
                
                service.AddCors(op =>
                {
                    op.AddPolicy("CorsPolicy",
                    c =>
                    {
                        c.WithOrigins(audience)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                    });
                });
            }
            catch (Exception ex)
            {
                logger.LogError("An error occurred while configuring services: {Message}", ex.Message );
            }
        }
    }
}