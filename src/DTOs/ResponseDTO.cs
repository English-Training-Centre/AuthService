namespace AuthService.src.DTOs
{
    public sealed class ResponseDTO
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty;

        public static ResponseDTO Success() => new() { IsSuccess = true };

        public static ResponseDTO Failure(string message) => new() { IsSuccess = false, Message = message };
    }
}