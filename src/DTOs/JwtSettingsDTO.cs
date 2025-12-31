namespace AuthService.src.DTOs
{
    public sealed class JwtSettingsDTO
    {
        public string? ValidAudience { get; set; }
        public string? ValidIssuer { get; set; }
        public string? SecurityKey { get; set; }
        public string? ExpiryTime { get; set; }
    }
}