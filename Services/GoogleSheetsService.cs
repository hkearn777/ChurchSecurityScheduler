using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using ChurchSecurityScheduler.Models;

namespace ChurchSecurityScheduler.Services
{
    public class GoogleSheetsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleSheetsService> _logger;
        private SheetsService? _sheetsService;

        private readonly List<string> _positions = new()
        {
            "Worship Entry Door",
            "Preschool Door",
            "Covered Parking",
            "Worship Center Left",
            "Worship Center Right",
            "Welcome Center",
            "Discipleship Entry",
            "Fellowship Hall Entry"
        };

        public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<SheetsService> GetSheetsServiceAsync()
        {
            if (_sheetsService != null)
                return _sheetsService;

            var credentialsPath = _configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";

            // Read the JSON file and create credential from JSON string
            // Note: FromJson shows deprecation warning, but recommended replacements don't exist in current API version
#pragma warning disable CS0618
            var jsonString = await File.ReadAllTextAsync(credentialsPath);
            var credential = GoogleCredential.FromJson(jsonString)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
#pragma warning restore CS0618

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _configuration["GoogleSheets:ApplicationName"] ?? "Church Security Scheduler",
            });

            return _sheetsService;
        }

        public async Task<List<string>> GetAvailableDatesAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var dates = new List<string>();

            foreach (var sheet in spreadsheet.Sheets)
            {
                if (sheet.Properties.Title != "Template")
                {
                    dates.Add(sheet.Properties.Title);
                }
            }

            return dates.OrderBy(d => d).ToList();
        }

        public async Task<ScheduleSheet?> GetScheduleForDateAsync(string date)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var range = $"'{date}'!A1:D10";

            try
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                if (values == null || values.Count == 0)
                    return null;

                var schedule = new ScheduleSheet
                {
                    Date = date,
                    Positions = new List<SecurityPosition>()
                };

                // Skip header rows (row 1 and 2) and read positions from row 3 onward
                for (int i = 2; i < values.Count; i++)
                {
                    var row = values[i];
                    if (row.Count > 0)
                    {
                        schedule.Positions.Add(new SecurityPosition
                        {
                            Position = row.Count > 0 ? row[0].ToString() ?? "" : "",
                            TimeSlot8_30 = row.Count > 1 ? row[1].ToString() ?? "" : "",
                            TimeSlot9_45 = row.Count > 2 ? row[2].ToString() ?? "" : "",
                            TimeSlot11_00 = row.Count > 3 ? row[3].ToString() ?? "" : ""
                        });
                    }
                }

                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading schedule for date: {date}");
                return null;
            }
        }

        public async Task<bool> CreateSheetForDateAsync(string date)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                // Create new sheet
                var addSheetRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>
                    {
                        new Request
                        {
                            AddSheet = new AddSheetRequest
                            {
                                Properties = new SheetProperties
                                {
                                    Title = date
                                }
                            }
                        }
                    }
                };

                await service.Spreadsheets.BatchUpdate(addSheetRequest, spreadsheetId).ExecuteAsync();

                // Initialize sheet with headers and positions
                var range = $"'{date}'!A1:D10";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { $"Security Schedule - {date}", "", "", "" },
                        new List<object> { "Position", "8:30 AM", "9:45 AM", "11:00 AM" }
                    }
                };

                // Add all positions
                foreach (var position in _positions)
                {
                    valueRange.Values.Add(new List<object> { position, "", "", "" });
                }

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();

                _logger.LogInformation($"Created new sheet for date: {date}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating sheet for date: {date}");
                return false;
            }
        }

        public async Task<bool> UpdatePositionAsync(string date, string position, string timeSlot, string volunteerName)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                // Find the row for this position
                var rowIndex = _positions.IndexOf(position) + 3; // +3 because: 1-based, +1 for title row, +1 for header row
                
                // Determine column based on time slot
                var column = timeSlot switch
                {
                    "8:30" => "B",
                    "9:45" => "C",
                    "11:00" => "D",
                    _ => "B"
                };

                var range = $"'{date}'!{column}{rowIndex}";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { volunteerName } }
                };

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();

                _logger.LogInformation($"Updated {position} at {timeSlot} for {date} with {volunteerName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating position");
                return false;
            }
        }

        public List<string> GetPositions() => _positions;
    }
}