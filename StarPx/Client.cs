using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
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
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Func<Task>> _uploadQueue;
        public Client(string baseUrl, AsyncRetryPolicy<HttpResponseMessage> policy, int maxConcurrentUploads = 6)
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
            _semaphore = new SemaphoreSlim(maxConcurrentUploads);
            _uploadQueue = new ConcurrentQueue<Func<Task>>();
        }
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
        
        public async Task<FileUploadStartResponse> FileUploadStart(string endpoint, string? authToken, FileUploadStartRequest requestBody)
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
                var returnValue = JsonSerializer.Deserialize<FileUploadStartResponse>(content);
                return returnValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send request after maximum retries. Error: {ex.Message}");
                _logger.Error(ex.Message);

                throw;
            }

        }
        public async Task<string> FileUploadFinish(string endpoint, string? authToken, string fileId)
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint + "/" + fileId);
                    if (!string.IsNullOrEmpty(authToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    return SendRequestWithoutRetryAsync(request);
                });
                response.EnsureSuccessStatusCode();
                return "Successfully uploaded!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send request after maximum retries. Error: {ex.Message}");
                _logger.Error(ex.Message);

                throw;
            }

        }
        public async Task<string> UploadFileAsync(string startEndpoint, string finishEndpoint, string? authToken, FileUploadStartRequest requestBody)
        {
            var tcs = new TaskCompletionSource<string>();

            _uploadQueue.Enqueue(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    var startResponse = await FileUploadStart(startEndpoint, authToken, requestBody);
                    await FileUploadFinish(finishEndpoint, authToken, startResponse.file_id);
                    var successMessage = $"Successfully uploaded file {startResponse.file_id}";
                    Console.WriteLine(successMessage);
                    tcs.SetResult(successMessage);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    _semaphore.Release();
                    ProcessQueue();
                }
            });

            ProcessQueue();
            return await tcs.Task;
        }

        private void ProcessQueue()
        {
            if (_uploadQueue.TryDequeue(out var uploadTask))
            {
                Task.Run(uploadTask);
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
