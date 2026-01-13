namespace AuthService.src.Application.DTOs.Commands;

public sealed record UserAuthRequest(
    string Username,
    string Password
);