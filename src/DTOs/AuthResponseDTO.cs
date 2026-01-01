namespace AuthService.src.DTOs
{
    public sealed class AuthResponseDTO
    {
        public bool IsSuccess { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}