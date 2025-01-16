using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CadastroClientesUI.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<T> PostAsync<T>(string url, T data)
        {
            try
            {
                Console.WriteLine($"Sending POST request to: {_httpClient.BaseAddress}{url}");
                var response = await _httpClient.PostAsJsonAsync($"{_httpClient.BaseAddress}{url}", data);

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error during POST request: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during POST request: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}{url}");

                // Garantir que a resposta é bem-sucedida antes de tentar desserializar
                response.EnsureSuccessStatusCode();

                // Verificar o tipo de conteúdo da resposta
                if (response.Content.Headers.ContentType?.MediaType != "application/json")
                {
                    throw new Exception($"Resposta não é JSON: {response.Content.Headers.ContentType?.MediaType}");
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> DeleteAsync(string url)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_httpClient.BaseAddress}{url}");

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<T> PutAsync<T>(string url, T data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_httpClient.BaseAddress}{url}", data);

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error during PUT request: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during PUT request: {ex.Message}");
                throw;
            }
        }

    }
}
