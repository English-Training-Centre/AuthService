using AuthService.src.Application.DTOs.Commands;
using AuthService.src.Application.DTOs.Queries;

namespace AuthService.src.Application.Interfaces;

public interface IUserGrpcServiceClient
{
    Task<UserServiceResponse> AuthAsync(UserAuthRequest request, CancellationToken ct);
    Task<UserServiceResponse> GetAuthByIdAsync(Guid id, CancellationToken ct);
}