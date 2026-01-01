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

        public async Task<UserServiceAuthDTO> AuthUserAsync(AuthRequestDTO auth)
        {
            ArgumentNullException.ThrowIfNull(auth);

            try
            {
                var response = await _http.PostAsJsonAsync("api/Users/v1/auth", auth);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Falha na autenticação: {response.StatusCode}");
                }

                return await response.Content.ReadFromJsonAsync<UserServiceAuthDTO>()
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

        public async Task<UserServiceAuthDTO> GetUserByIdAsync(Guid id)
        {
            HttpResponseMessage response;

            try
            {
                response = await _http.GetAsync($"api/Users/v1/get-user/{id}");
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException)
            {
                throw new InvalidOperationException(
                    "O serviço de usuários está fora do ar no momento.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    "O serviço de usuários não respondeu a tempo.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Falha ao obter usuário. Status: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<UserServiceAuthDTO>()
                ?? throw new InvalidOperationException("Resposta inválida do serviço de usuários.");
        }
    }
}