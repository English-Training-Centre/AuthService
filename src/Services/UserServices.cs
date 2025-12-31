using System.Net.Sockets;
using System.Text.Json;
using AuthService.src.DTOs;
using AuthService.src.Interfaces;

namespace AuthService.src.Services
{
    public class UserServices (HttpClient http, ILogger<UserServices> logger) : IUserServices
    {
        private readonly HttpClient _http = http;
        private readonly ILogger<UserServices> _logger = logger;

        public async Task<AuthResponseDTO> AuthUserAsync(AuthRequestDTO request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var response = await _http.PostAsJsonAsync("api/Users/v1/auth", request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Falha na autenticação: {response.StatusCode}");
                }

                return await response.Content.ReadFromJsonAsync<AuthResponseDTO>()
                    ?? throw new InvalidOperationException("Resposta inesperada do servidor.");
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException)
            {
                throw new InvalidOperationException("O serviço de usuários está fora do ar no momento. Tente novamente mais tarde.");
            }
            catch (TaskCanceledException)
            {
                throw new InvalidOperationException("O serviço de usuários não respondeu a tempo. Tente novamente.");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Erro interno ao tentar autenticar. Tente novamente mais tarde.");
            }
        }
    }
}