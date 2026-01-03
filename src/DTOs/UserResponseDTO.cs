namespace AuthService.src.DTOs
{
    public sealed class UserResponseDTO
    {
        public bool IsSuccess { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }        

        public static UserResponseDTO Failure() => new() { IsSuccess = false };
    }
}