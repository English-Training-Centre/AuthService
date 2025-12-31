namespace AuthService.src.DTOs
{
    public sealed class AuthResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public static AuthResponseDTO Success(string message, Guid user_id, string username, string role) => new() { IsSuccess = true, Message = message, UserId = user_id, Username = username, Role = role };

        public static AuthResponseDTO Failure(string message) => new() { IsSuccess = false, Message = message };
    }
}