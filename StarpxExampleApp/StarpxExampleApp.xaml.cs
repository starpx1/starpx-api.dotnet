using Polly;
using Polly.Retry;
using StarPx;
using StarPx.Models;
using System;
using System.IO;
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
        private AuthResult? _authResult;
        private ImagingSessionResponse? _imagingSessionResult;
        private bool _isCaptureComplete = false;
        private bool _isImagingSessionStarted = false;
        private bool _isSessionPendingFinish = false;
        public StarpxExampleApp()
        {
            InitializeComponent();
            _retryPolicy = RetrySetting(6, 5);
            _apiClient = new Client("https://upload1.starpx.com", _retryPolicy);
            LoadFilesToListBox();
        }

        private async void StartImagingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSessionPendingFinish)
            {
                await FinishImagingSession();
                return;
            }
            var apiKey = ApiKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter a valid API key.");
                return;
            }

            try
            {
                _authResult = await _apiClient.Authenticate("/authenticate", apiKey);
                LogTextBlock.Text += "Authentication: success\n";

                var imagingSessionRequest = new ImagingSessionRequest
                {
                    local_time = "2024-05-27T10:00:00Z",
                    geolocation = new Geolocation { lat = 34.0522, lon = -118.2437 },
                    targetname = "Mars",
                    skycoordinates = new SkyCoordinates { ra = 14.66, dec = -60.835 }
                };

                _imagingSessionResult = await _apiClient.ImagingSessionStart("/imagingsession/start", _authResult.access_token, imagingSessionRequest);
                LogTextBlock.Text += $"Starting imaging session: {_imagingSessionResult.imaging_session_id}\n";

                StartImagingButton.Content = "Finish Imaging Session";
                StartImagingButton.Background = Brushes.Green;
                StartImagingButton.Foreground = Brushes.White;
                _isImagingSessionStarted = true;

                //var finishMessage = await _apiClient.ImagingSessionFinish("/imagingsession/finish", _authResult.access_token, _imagingSessionResult.imaging_session_id);
                //LogTextBlock.Text += $"{finishMessage}\n";

                //StartImagingButton.Content = "Start Imaging";
                //StartImagingButton.Background = Brushes.LightGray;
                //_isCaptureComplete = false;
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
            StartImagingButton.IsEnabled = false;
            StartImagingButton.Foreground = Brushes.Black;
            var files = GetFilesFromResources();
            foreach (var file in files)
            {
                HighlightFile(file);
                await UploadFileAsync(file);
                await Task.Delay(5000); // Wait for 5 seconds
            }

            CaptureButton.Content = "Capture Image";
            CaptureButton.Background = Brushes.LightGray;
            StartImagingButton.IsEnabled = true;
            _isCaptureComplete = true;

            if (_isImagingSessionStarted)
            {
                _isSessionPendingFinish = true;
                StartImagingButton.Content = "Finish Imaging Session";
                StartImagingButton.Background = Brushes.Green;
                StartImagingButton.Foreground = Brushes.White;
            }
        }
        private async Task FinishImagingSession()
        {
            try
            {
                var finishMessage = await _apiClient.ImagingSessionFinish("/imagingsession/finish", _authResult?.access_token, _imagingSessionResult?.imaging_session_id);
                LogTextBlock.Text += $"{finishMessage}\n";

                StartImagingButton.Content = "Start Imaging";
                StartImagingButton.Background = Brushes.LightGray;
                StartImagingButton.Foreground = Brushes.Black;
                _isImagingSessionStarted = false;
                _isCaptureComplete = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        private void HighlightFile(string fileName)
        {
            string fileNameOnly = Path.GetFileName(fileName);
            foreach (ListBoxItem item in FileListBox.Items)
            {
                if (item.Content.ToString() == fileNameOnly)
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
                path = fileName,
                compression = "zip",
                size = new FileInfo(fileName).Length,
            };

            try
            {

                var uploadResult = await _apiClient.UploadFileAsync($"/imagingsession/fileupload/start/{_imagingSessionResult?.imaging_session_id}", "/imagingsession/fileupload/finish", _authResult?.access_token, fileUploadStartRequest);
                LogTextBlock.Text += $"{uploadResult.SuccessMessage}\n";

                if (!string.IsNullOrEmpty(uploadResult.UploadUrl))
                {
                    var bitmap = new BitmapImage(new Uri(uploadResult.UploadUrl));
                    LiveViewImage.Source = bitmap;
                }
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
        private string[] GetFilesFromResources()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = Path.Combine(projectDirectory, "..", "..", "..", "Resources", "TestFiles");
            return Directory.GetFiles(resourcesPath, "*.fits");
        }
        private void LoadFilesToListBox()
        {
            var files = GetFilesFromResources();
            foreach (var file in files)
            {
                FileListBox.Items.Add(new ListBoxItem { Content = Path.GetFileName(file) });
            }
        }
        private void ApiKeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ApiKeyTextBox.Text == "Enter API key here...")
            {
                ApiKeyTextBox.Text = string.Empty;
                ApiKeyTextBox.Foreground = Brushes.Gray;
            }
        }

        private void ApiKeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
            {
                ApiKeyTextBox.Text = "Enter API key here...";
                ApiKeyTextBox.Foreground = Brushes.Gray;
            }
        }
    }
}
