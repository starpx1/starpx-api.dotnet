using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace StarPx
{
    public class Client
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public Client(string baseUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
        }

        public async Task<string> PlateSolveResult(string endpoint, string? authToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + endpoint);
            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> Authenticate(string endpoint, string data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
            request.Headers.Add("apiKey", data);
            //request.Content = new StringContent(data);
            //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PlateSolve(string endpoint, string? authToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);


            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
