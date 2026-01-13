namespace AuthService.src.Application.DTOs.Queries;

public sealed record CheckSessionResponse
{
    public bool IsSuccess { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? AccessToken { get; set; }

    public static CheckSessionResponse Failure() => new() { IsSuccess = false };
}