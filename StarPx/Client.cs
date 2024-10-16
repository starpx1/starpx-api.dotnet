using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Serilog;
using Serilog.Events;
using StarPx.Models;
namespace StarPx
{
    public class Client
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILogger _logger;

        public Client(string baseUrl, AsyncRetryPolicy<HttpResponseMessage> policy)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            // Configure the back-off retry policy

            _retryPolicy = policy;
                
            _logger = Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.Console()
           .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
           .WriteTo.File("warningLog.txt",restrictedToMinimumLevel: LogEventLevel.Warning,rollingInterval: RollingInterval.Day)
           .MinimumLevel.Debug()
           .CreateLogger();
        }
        //public async Task<string> PlateSolveResult(string endpoint, string? authToken)
        //{
        //    //var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + endpoint);
        //    //if (!string.IsNullOrEmpty(authToken))
        //    //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        //    try
        //    {
        //        var response = await _retryPolicy.ExecuteAsync(() =>
        //        {
        //            var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + endpoint);
        //            if (!string.IsNullOrEmpty(authToken))
        //                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        //            return SendRequestWithoutRetryAsync(request);
        //        });
        //        response.EnsureSuccessStatusCode();
        //        return await response.Content.ReadAsStringAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Failed to send request after maximum retries. Error: {ex.Message}");
        //        _logger.Error(ex.Message);

        //        throw;
        //    }

        //}
        public async Task<AuthResult> Authenticate(string endpoint, string data)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
                    request.Headers.Add("apiKey", data);
                    return SendRequestWithoutRetryAsync(request);
                });
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var returnValue = JsonSerializer.Deserialize<AuthResult>(content);
                return returnValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send request after maximum retries. Error: {ex.Message}");
                _logger.Error(ex.Message);

                throw;
            }

        }

        public async Task<ImagingSessionResponse> ImagingSession(string endpoint, string? authToken, ImagingSessionRequest requestBody)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
                    if (!string.IsNullOrEmpty(authToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    request.Content = new StringContent(JsonSerializer.Serialize(requestBody));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return SendRequestWithoutRetryAsync(request);
                });
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var returnValue = JsonSerializer.Deserialize<ImagingSessionResponse>(content);
                return returnValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send request after maximum retries. Error: {ex.Message}");
                _logger.Error(ex.Message);
                throw;
            }

        }
        public async Task<AuthResult> RefreshToken(string endpoint, string data)
        {
            return await Authenticate(endpoint, data);
        }
        private async Task<HttpResponseMessage> SendRequestWithoutRetryAsync(HttpRequestMessage request)
        {
            // Send the actual HTTP request
            return await _httpClient.SendAsync(request);
        }
    }
    
    
}
