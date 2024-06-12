using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

using StarPx;
using Polly;
namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var retrySetting = RetrySetting(6,5);
            var apiClient = new Client("http://localhost:3000", retrySetting);

            try
            {
                var authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ...";


                var authData = "valid-api-key";
                var authResult = await apiClient.Authenticate("/authenticate", authData);
                Console.WriteLine(authResult);

                var postResult = await apiClient.PlateSolve("/platesolve", authToken);
                Console.WriteLine(postResult);

                var result = await apiClient.PlateSolveResult("/platesolve/platesolve123", authToken);
                Console.WriteLine(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        private static AsyncRetryPolicy<HttpResponseMessage> RetrySetting(int retryCount, int sleepDuration, string? retryMsg = null)
        {
            var retrySetting = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: retryCount, // Maximum number of retries
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(sleepDuration), // Exponential back-off
                    onRetryAsync: async (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log retry attempt
                        if (retryMsg != null)
                        {
                            Console.WriteLine(retryMsg);

                        }
                        else
                        {
                            Console.WriteLine($"Retrying in {timespan.TotalSeconds} seconds. Retry attempt {retryAttempt}");

                        }
                        await Task.CompletedTask;
                    });
            return retrySetting;
        }
    }
}
