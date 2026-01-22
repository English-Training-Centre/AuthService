namespace AuthService.src.Application.DTOs.Responses;

public sealed record AuthResponse
{
    public bool IsSuccess { get; init; }
    public string? AccessToken { get; init; } 
    public string? RefreshToken { get; init; }

    public static AuthResponse Failure() => new() { IsSuccess = false };
}