namespace AuthService.src.Application.DTOs.Commands;

public sealed record GenerateTokenRequest
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}