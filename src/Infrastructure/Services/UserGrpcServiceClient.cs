using Grpc.Core;
using Libs.Core.Internal.Protos.UserService;
using Libs.Core.Internal.src.DTOs.Responses;
using Libs.Core.Internal.src.Interfaces;
using Libs.Core.Shared.src.DTOs.Requests;

namespace AuthService.src.Infrastructure.Services;

public sealed class UserGrpcServiceClient(UsersAuthGrpc.UsersAuthGrpcClient client, ILogger<UserGrpcServiceClient> logger) : IUserAuthGrpcService
{
    private readonly UsersAuthGrpc.UsersAuthGrpcClient _client = client;
    private readonly ILogger<UserGrpcServiceClient> _logger = logger;

    public async Task<UserAuthResponse> UserAuthAsync(UserAuthRequest request, CancellationToken ct)
    {
        var grpcRequest = new GrpcUserAuthRequest
        {
            Username = request.Username,
            Password = request.Password
        };

        GrpcUserAuthResponse grpcResponse;
        try
        {
            grpcResponse = await _client.UserAuthAsync(grpcRequest, new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(10),
                cancellationToken: ct
            ));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "- UserGrpcServiceClient -> AuthAsync(...)");
            return new UserAuthResponse { IsSuccess = false };
        }

        return MapToResponse(grpcResponse);
    }

    public async Task<UserAuthResponse> GetUserAuthByIdAsync(Guid id, CancellationToken ct)
    {
        var grpcRequest = new GrpcUserAuthIdRequest
        {
            UserId = id.ToString()
        };

        GrpcUserAuthResponse grpcResponse;
        try
        {
            grpcResponse = await _client.GetUserAuthByIdAsync(grpcRequest, new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(10),
                cancellationToken: ct
            ));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "- UserGrpcServiceClient -> GetAuthByIdAsync(...)");
            return new UserAuthResponse { IsSuccess = false };
        }

        return MapToResponse(grpcResponse);
    }

    private static UserAuthResponse MapToResponse(GrpcUserAuthResponse grpcResponse)
    {
        var isSuccess = grpcResponse.IsSuccess;

        if (!isSuccess || string.IsNullOrWhiteSpace(grpcResponse.UserId) ||
            !Guid.TryParse(grpcResponse.UserId, out var userId))
        {
            return new UserAuthResponse { IsSuccess = false };
        }

        return new UserAuthResponse
        {
            IsSuccess = true,
            UserId = userId,
            FullName = grpcResponse.FullName ?? string.Empty,
            Username = grpcResponse.Username ?? string.Empty,
            Role = grpcResponse.Role ?? string.Empty
        };
    }
}