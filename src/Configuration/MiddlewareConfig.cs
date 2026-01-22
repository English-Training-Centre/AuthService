using System.IO.Compression;
using AuthService.Services;
using AuthService.src.Application.Handlers;
using AuthService.src.Application.Interfaces;
using AuthService.src.Infrastructure.Persistence;
using AuthService.src.Infrastructure.Repositories;
using AuthService.src.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Grpc.Net.Compression;
using Libs.Core.Internal.Protos.UserService;
using Libs.Core.Internal.src.Interfaces;
using Npgsql;
using Polly;

namespace AuthService.src.Configuration;

public static class MiddlewareConfig
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var logger = LoggerFactory.Create(s => s.AddConsole()).CreateLogger<Program>();
        try
        {
            services.AddValidatorsFromAssembly(typeof(Program).Assembly);
            services.AddFluentValidationAutoValidation();

            services.AddSingleton(nd =>
            {
                var builder = new NpgsqlDataSourceBuilder(
                    PostgresDB.BuildConnectionStringFromEnvironment()
                );
                builder.UseLoggerFactory(nd.GetRequiredService<ILoggerFactory>());
                return builder.Build();
            });

            services.AddHealthChecks()
                    .AddNpgSql(nd => nd.GetRequiredService<NpgsqlDataSource>());

            services.AddScoped<IPostgresDB, PostgresDB>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserHandler, UserHandler>();
            services.AddScoped<IUserAuthGrpcService, UserGrpcServiceClient>();

            services.AddGrpcClient<UsersAuthGrpc.UsersAuthGrpcClient>(op =>
            {
                op.Address = new Uri(configuration["GrpcServices:UserService"] ?? "http://localhost:5284");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            })
            .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)))
            .ConfigureChannel(channelOptions =>
            {
                channelOptions.MaxReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
                channelOptions.MaxSendMessageSize = 5 * 1024 * 1024;     // 5 MB
                channelOptions.CompressionProviders = [new GzipCompressionProvider(CompressionLevel.Fastest)];
            });

            services.AddGrpc();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while configuring Middleware");
        }
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapGrpcService<AuthGrpcService>();

        return app;
    }
}