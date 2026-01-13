namespace AuthService.src.Application.DTOs.Commands;

public sealed record RefreshTokenRequest
(
    Guid UserId,
    string Token
);