using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;
using AuthService.src.Application.Interfaces;
using Grpc.Core;
using UserService;

namespace AuthService.src.Infrastructure.Services;

public sealed class UserGrpcServiceClient(UsersAuthGrpc.UsersAuthGrpcClient client, ILogger<UserGrpcServiceClient> logger) : IUserGrpcServiceClient
{
    private readonly UsersAuthGrpc.UsersAuthGrpcClient _client = client;
    private readonly ILogger<UserGrpcServiceClient> _logger = logger;

    public async Task<UserServiceResponse> AuthAsync(UserAuthRequest request, CancellationToken ct)
    {
        var grpcRequest = new GrpcUserAuthRequest
        {
            Username = request.Username,
            Password = request.Password
        };

        GrpcUserAuthResponse grpcResponse;
        try
        {
            grpcResponse = await _client.AuthAsync(grpcRequest, new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(10),
                cancellationToken: ct
            ));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "- UserGrpcServiceClient -> AuthAsync(...)");
            return new UserServiceResponse { IsSuccess = false };
        }

        return MapToResponse(grpcResponse);
    }

    public async Task<UserServiceResponse> GetAuthByIdAsync(Guid id, CancellationToken ct)
    {
        var grpcRequest = new GrpcUserIdRequest
        {
            UserId = id.ToString()
        };

        GrpcUserAuthResponse grpcResponse;
        try
        {
            grpcResponse = await _client.GetAuthByIdAsync(grpcRequest, new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(10),
                cancellationToken: ct
            ));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "- UserGrpcServiceClient -> GetAuthByIdAsync(...)");
            return new UserServiceResponse { IsSuccess = false };
        }

        return MapToResponse(grpcResponse);
    }

    private static UserServiceResponse MapToResponse(GrpcUserAuthResponse grpcResponse)
    {
        var isSuccess = grpcResponse.IsSuccess;

        if (!isSuccess || string.IsNullOrWhiteSpace(grpcResponse.UserId) ||
            !Guid.TryParse(grpcResponse.UserId, out var userId))
        {
            return new UserServiceResponse { IsSuccess = false };
        }

        return new UserServiceResponse
        {
            IsSuccess = true,
            UserId = userId,
            FullName = grpcResponse.FullName ?? string.Empty,
            Username = grpcResponse.Username ?? string.Empty,
            Role = grpcResponse.Role ?? string.Empty
        };
    }
}