namespace AuthService.src.Application.DTOs.Requests;

public sealed record RefreshTokenRequest
(
    Guid UserId,
    string RefreshToken
);