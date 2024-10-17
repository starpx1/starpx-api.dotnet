using Polly;
using Polly.Retry;
using StarPx;
using StarPx.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TestApp
{
    public partial class StarpxExampleApp : Window
    {
        private Client _apiClient;
        private AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public StarpxExampleApp()
        {
            InitializeComponent();
            _retryPolicy = RetrySetting(6, 5);
            _apiClient = new Client("https://upload1.starpx.com", _retryPolicy);
        }

        private async void StartImagingButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter a valid API key.");
                return;
            }

            try
            {
                var authResult = await _apiClient.Authenticate("/authenticate", apiKey);
                LogTextBlock.Text += "Authentication: success\n";

                var imagingSessionRequest = new ImagingSessionRequest
                {
                    local_time = "2024-05-27T10:00:00Z",
                    geolocation = new Geolocation { lat = 34.0522, lon = -118.2437 },
                    targetname = "Mars",
                    skycoordinates = new SkyCoordinates { ra = 14.66, dec = -60.835 }
                };

                var imagingSessionResult = await _apiClient.ImagingSession("/imagingsession/start", authResult.access_token, imagingSessionRequest);
                LogTextBlock.Text += $"Starting imaging session: {imagingSessionResult.imaging_session_id}\n";

                StartImagingButton.Content = "Finish Imaging Session";
                StartImagingButton.Background = Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureButton.Content = "Capturing...";
            CaptureButton.Background = Brushes.Yellow;

            var files = new[] { "file1.fit", "file2.fit" };
            foreach (var file in files)
            {
                HighlightFile(file);
                await UploadFileAsync(file);
                await Task.Delay(5000); // Wait for 5 seconds
            }

            CaptureButton.Content = "Capture Image";
            CaptureButton.Background = Brushes.LightGray;
        }

        private void HighlightFile(string fileName)
        {
            foreach (ListBoxItem item in FileListBox.Items)
            {
                if (item.Content.ToString() == fileName)
                {
                    item.Background = Brushes.Yellow;
                }
                else
                {
                    item.Background = Brushes.White;
                }
            }
        }

        private async Task UploadFileAsync(string fileName)
        {
            var fileUploadStartRequest = new FileUploadStartRequest
            {
                path = $"C:\\Imaging\\{fileName}",
                compression = "zip",
                size = 16777216,
            };

            try
            {
                var authResult = await _apiClient.Authenticate("/authenticate", ApiKeyTextBox.Text);
                var imagingSessionResult = await _apiClient.ImagingSession("/imagingsession/start", authResult.access_token, new ImagingSessionRequest());

                await _apiClient.UploadFileAsync($"/imagingsession/fileupload/start/{imagingSessionResult.imaging_session_id}", "/imagingsession/fileupload/finish", authResult.access_token, fileUploadStartRequest);
                LogTextBlock.Text += $"Uploaded {fileName}\n";

                // Fetch live view image
                var liveViewUrl = "https://example.com/liveview"; // Replace with actual URL
                var bitmap = new BitmapImage(new Uri(liveViewUrl));
                LiveViewImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                LogTextBlock.Text += $"Error uploading {fileName}: {ex.Message}\n";
            }
        }

        private static AsyncRetryPolicy<HttpResponseMessage> RetrySetting(int retryCount, int sleepDuration, string? retryMsg = null)
        {
            var retrySetting = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(sleepDuration),
                    onRetryAsync: async (outcome, timespan, retryAttempt, context) =>
                    {
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
