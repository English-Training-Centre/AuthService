namespace AuthService.src.Application.DTOs.Responses;

public sealed record CheckSessionResponse
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }

    public static CheckSessionResponse Failure() => new() { IsSuccess = false };
}