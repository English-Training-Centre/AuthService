namespace AuthService.src.Application.DTOs.Requests;

public sealed record GenerateTokenRequest
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}