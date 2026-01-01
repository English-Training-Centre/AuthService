namespace AuthService.src.DTOs
{
    public sealed class AuthUserDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}