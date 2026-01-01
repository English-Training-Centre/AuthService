namespace AuthService.src.DTOs
{
    public sealed class UserServiceAuthDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}