using Grpc.Core;
using Libs.Core.Public.Protos.AuthService;
using Libs.Core.Public.src.DTOs.Requests;
using Libs.Core.Public.src.Interfaces;
using Libs.Core.Shared.src.DTOs.Requests;

namespace AuthService.Services;

public sealed class AuthGrpcService(IAuthGrpcService serviceAuth, ILogger<AuthGrpcService> logger) : AuthGrpc.AuthGrpcBase
{
    private readonly IAuthGrpcService _serviceAuth = serviceAuth;
    private readonly ILogger<AuthGrpcService> _logger = logger;

    public override async Task<GrpcAuthResponse> SignIn(GrpcAuthRequest request, ServerCallContext context)
    {
        try
        {
            var parameter = new UserAuthRequest
            (
                request.Username,
                request.Password
            );

            var result = await _serviceAuth.SignInAsync(parameter, context.CancellationToken);

            var protoResponse = new GrpcAuthResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = result.AccessToken ?? "",
                RefreshToken = result.RefreshToken ?? ""
            };

            return protoResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: AuthGrpcService -> SignIn(....)");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to SignIn"));
        }
    }

    public override async Task<GrpcAuthResponse> RefreshToken(GrpcRefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, $"User Id is not a valid GUID: '{request.UserId}'")
                );
            }

            var parameter = new RefreshTokenRequest
            (
                userId,
                request.RefreshToken
            );

            var result = await _serviceAuth.RefreshTokenAsync(parameter, context.CancellationToken);

            var protoResponse = new GrpcAuthResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = result.AccessToken ?? "",
                RefreshToken = result.RefreshToken ?? ""
            };

            return protoResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: AuthGrpcService -> RefreshTokn(....)");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to RefreshToken"));
        }
    }

    public override async Task<GrpcGetTokenResponse> GetValidRefreshToken(GrpcTokenRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _serviceAuth.GetValidRefreshTokenAsync(request.RefreshToken, context.CancellationToken) ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "token is null"));

            var protoResponse = new GrpcGetTokenResponse
            {
                UserId = result.UserId.ToString()
            };

            return protoResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: AuthGrpcService -> GetValidRefreshToken(....)");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to GetValidRefreshToken"));
        }
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> RevokeRefreshToken(GrpcTokenRequest request, ServerCallContext context)
    {
        try
        {
            await _serviceAuth.RevokeRefreshTokenAsync(request.RefreshToken, context.CancellationToken);

            return new Google.Protobuf.WellKnownTypes.Empty();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RevokeRefreshToken");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to RevokeRefreshToken"));
        }
    }

    public override async Task<GrpcSessionResponse> CheckSession(GrpcSessionRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, $"User Id is not a valid GUID: '{request.UserId}'")
                );
            }

            var result = await _serviceAuth.CheckSessionAsync(userId, context.CancellationToken);

            var protoResponse = new GrpcSessionResponse
            {
                IsSuccess = result.IsSuccess,
                AccessToken = result.AccessToken ?? ""
            };

            return protoResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: AuthGrpcService -> CheckSession(....)");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to CheckSession"));
        }
    }
}