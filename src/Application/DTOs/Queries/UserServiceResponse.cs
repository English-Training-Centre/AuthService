namespace AuthService.src.Application.DTOs.Queries;

public sealed record UserServiceResponse
{
    public bool IsSuccess { get; init; }
    public Guid? UserId { get; init; }
    public string? FullName { get; init; }
    public string? Username { get; init; }
    public string? Role { get; init; }
}