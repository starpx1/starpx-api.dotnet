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
using StarPx.Models;
namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var retrySetting = RetrySetting(6,5);
            var apiClient = new Client("https://upload1.starpx.com", retrySetting);

            try
            {
                //var authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ...";


                var authData = "valid-api-key";
                var authResult = await apiClient.Authenticate("/authenticate", authData);
                Console.WriteLine($"Access Token: {authResult.access_token}");

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (authResult.expiry_time - currentTime < 300) // Token expires in less than 5 minutes
                {
                    Console.WriteLine("Token is about to expire, refreshing...");
                    authResult = await apiClient.RefreshToken("/authenticate", authData);
                    Console.WriteLine($"New Access Token: {authResult.access_token}");
                }

                var imagingSessionRequest = new ImagingSessionRequest
                {
                    local_time = "2024-05-27T10:00:00Z",
                    geolocation = new Geolocation { lat = 34.0522, lon = -118.2437 },
                    targetname = "Mars",
                    skycoordinates = new SkyCoordinates { ra = 14.66, dec = -60.835 }
                };

                var imagingSessionResult = await apiClient.ImagingSession("/imagingsession/start", authResult.access_token, imagingSessionRequest);
                Console.WriteLine($"Imaging Session ID: {imagingSessionResult.imaging_session_id}");

                var fileUploadStartRequest = new FileUploadStartRequest
                {
                    path = "C:\\Imaging\\M33_light_300s.fit",
                    compression = "zip",
                    size = 16777216,
                };
                for (int i = 0; i < 10; i++)
                {
                    await apiClient.UploadFileAsync($"/imagingsession/fileupload/start/{imagingSessionResult.imaging_session_id}", "/imagingsession/fileupload/finish", authResult.access_token, fileUploadStartRequest);

                }

                Console.WriteLine("All files have been queued for upload.");
                //var imagingSessionFileUploadResult = await apiClient.FileUploadStart($"/imagingsession/fileupload/start/{imagingSessionResult.imaging_session_id}", authResult.access_token, fileUploadStartRequest);
                //Console.WriteLine($"Imaging Session File ID: {imagingSessionFileUploadResult.file_id}");

                //var imagingSessionFileUploadFinishResult = await apiClient.FileUploadFinish("/imagingsession/fileupload/finish", authResult.access_token,imagingSessionFileUploadResult.file_id);
                //Console.WriteLine(imagingSessionFileUploadFinishResult);


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
