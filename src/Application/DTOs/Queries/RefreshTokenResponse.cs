namespace AuthService.src.Application.DTOs.Queries;

public sealed record RefreshTokenResponse
(
    Guid UserId,
    string Token
);