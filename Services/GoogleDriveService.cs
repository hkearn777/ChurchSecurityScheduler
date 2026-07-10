using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace ChurchSecurityScheduler.Services
{
    public class GoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveService> _logger;
        private DriveService? _driveService;

        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<DriveService> GetDriveServiceAsync()
        {
            if (_driveService != null)
                return _driveService;

            string jsonString;

            // Try to read from environment variable first (Cloud Run)
            var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

            if (!string.IsNullOrEmpty(credentialsJson))
            {
                // Running in Cloud Run - use environment variable
                jsonString = credentialsJson;
                _logger.LogInformation("Using credentials from environment variable");
            }
            else
            {
                // Running locally - use file
                var credentialsPath = _configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";
                jsonString = await File.ReadAllTextAsync(credentialsPath);
                _logger.LogInformation("Using credentials from file");
            }

            // Create credential with Drive scope
#pragma warning disable CS0618
            var credential = GoogleCredential.FromJson(jsonString)
                .CreateScoped(DriveService.Scope.DriveReadonly);
#pragma warning restore CS0618

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _configuration["GoogleSheets:ApplicationName"] ?? "Church Security Scheduler",
            });

            return _driveService;
        }

        public async Task<(Stream fileStream, string fileName)?> GetFileByNameAsync(string fileName)
        {
            try
            {
                var service = await GetDriveServiceAsync();

                // Search for the file by name, ordered by modified date (newest first)
                var listRequest = service.Files.List();
                listRequest.Q = $"name='{fileName}' and trashed=false";
                listRequest.Fields = "files(id, name, mimeType, modifiedTime)";
                listRequest.OrderBy = "modifiedTime desc";  // Most recently modified first

                var files = await listRequest.ExecuteAsync();

                if (files.Files == null || files.Files.Count == 0)
                {
                    _logger.LogWarning($"File '{fileName}' not found in Google Drive");
                    return null;
                }

                // Get the first matching file (most recent if duplicates exist)
                var file = files.Files[0];
                _logger.LogInformation($"Found file: {file.Name} (ID: {file.Id}, Modified: {file.ModifiedTimeDateTimeOffset})");

                // Download the file
                var request = service.Files.Get(file.Id);
                var stream = new MemoryStream();
                await request.DownloadAsync(stream);
                stream.Position = 0;

                return (stream, file.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving file '{fileName}' from Google Drive");
                return null;
            }
        }
    }
}