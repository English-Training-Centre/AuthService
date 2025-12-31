using AuthService.src.DTOs;
using AuthService.src.Interfaces;

namespace AuthService.src.Repositories
{
    public class AuthRepository(IPostgresDbData db, ILogger<AuthRepository> logger) : IAuthRepository
    {
        private readonly IPostgresDbData _db = db;
        private readonly ILogger<AuthRepository> _logger = logger;
    }
}