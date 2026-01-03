namespace AuthService.src.DTOs
{
    public sealed class UserRefreshTokenDTO
    {
        public bool IsSuccess { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        public static UserRefreshTokenDTO Failure() => new() { IsSuccess = false };
    }
}