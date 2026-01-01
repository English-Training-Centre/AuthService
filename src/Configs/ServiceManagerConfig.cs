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
                    // --- Global por IP (opcional: pode virar policy também)
                    op.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    {
                        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: ip,
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 300,
                                Window = TimeSpan.FromSeconds(60),
                                SegmentsPerWindow = 6,
                                AutoReplenishment = true
                            });
                    });

                    // --- Policy: Limite de tentativas de login por IP
                    op.AddPolicy("LimitSignIn", httpContext =>
                    {
                        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"login-{ip}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 15,
                                Window = TimeSpan.FromSeconds(30),
                                SegmentsPerWindow = 6,
                                AutoReplenishment = true,
                                QueueLimit = 0
                            });
                    });

                    // --- Policy: Limite de refresh token por IP
                    op.AddPolicy("LimitRefreshToken", httpContext =>
                    {
                        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"refresh-{ip}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 10,
                                AutoReplenishment = true,
                                QueueLimit = 0
                            });
                    });

                    // --- Resposta personalizada quando rate limited
                    op.OnRejected = async (context, token) =>
                    {
                        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        
                        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        {
                            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("F0");
                            await context.HttpContext.Response.WriteAsync(
                                $"Too many requests. Try again in {retryAfter.TotalSeconds:F0} seconds.", token);
                        }
                        else
                        {
                            await context.HttpContext.Response.WriteAsync("Too many requests. Please slow down.", token);
                        }

                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Rate limited IP: {Ip} | Path: {Path} | Method: {Method}",
                            context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            context.HttpContext.Request.Path,
                            context.HttpContext.Request.Method);
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