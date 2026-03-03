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
        // private List<string> _positions; // Changed back to non-nullable, initialized in constructor

        public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            // _positions = new List<string>();
        }

        private async Task<SheetsService> GetSheetsServiceAsync()
        {
            if (_sheetsService != null)
                return _sheetsService;

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

            // Read the JSON and create credential from JSON string
            // Note: FromJson shows deprecation warning, but recommended replacements don't exist in current API version
#pragma warning disable CS0618
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

        // UPDATED: Load positions from the "Positions" tab (no caching)
        private async Task<List<string>> LoadPositionsFromSheetAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var range = "Positions!A:A"; // Read column A from "Positions" tab

            try
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                var positions = new List<string>();

                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0].ToString()))
                        {
                            positions.Add(row[0].ToString()!);
                        }
                    }
                }

                // Fallback: If no positions found in sheet, use defaults
                if (positions.Count == 0)
                {
                    _logger.LogWarning("No positions found in 'Positions' tab, using defaults");
                    positions = new List<string>
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
                }

                _logger.LogInformation($"Loaded {positions.Count} positions from sheet");
                return positions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading positions from sheet, using defaults");
                return new List<string>
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
            }
        }

        public async Task<List<string>> GetAvailableDatesAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var dates = new List<string>();

            foreach (var sheet in spreadsheet.Sheets)
            {
                // Exclude "Positions" tab and "Template" from date list
                if (sheet.Properties.Title != "Template" && sheet.Properties.Title != "Positions")
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
            var range = $"'{date}'!A:E"; // ✅ Changed from A:D to A:E

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
                    // Only add rows that have a position name
                    if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0].ToString()))
                    {
                        schedule.Positions.Add(new SecurityPosition
                        {
                            Position = row.Count > 0 ? row[0].ToString() ?? "" : "",
                            TimeSlot8_30 = row.Count > 1 ? row[1].ToString() ?? "" : "",
                            TimeSlot9_45 = row.Count > 2 ? row[2].ToString() ?? "" : "",
                            TimeSlot11_00 = row.Count > 3 ? row[3].ToString() ?? "" : "",
                            TimeSlot6_00 = row.Count > 4 ? row[4].ToString() ?? "" : ""  // ✅ Add this line
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
            var positions = await LoadPositionsFromSheetAsync(); // Load from sheet!

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
                var range = $"'{date}'!A1:E{positions.Count + 2}"; // Dynamic range based on position count (now includes E column)
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { $"Security Schedule - {date}", "", "", "", "" },
                        new List<object> { "Position", "8:30 AM", "9:45 AM", "11:00 AM", "6:00 PM" }
                    }
                };

                // Add all positions from the Positions tab
                foreach (var position in positions)
                {
                    valueRange.Values.Add(new List<object> { position, "", "", "", "" });
                }

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();

                _logger.LogInformation($"Created new sheet for date: {date} with {positions.Count} positions");
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
                // Read the actual sheet to find the position's row
                var schedule = await GetScheduleForDateAsync(date);
                if (schedule == null)
                {
                    _logger.LogWarning($"Schedule not found for date: {date}");
                    return false;
                }

                // Find the exact row index by searching through actual data
                var rowIndex = -1;
                for (int i = 0; i < schedule.Positions.Count; i++)
                {
                    if (schedule.Positions[i].Position.Equals(position, StringComparison.OrdinalIgnoreCase))
                    {
                        rowIndex = i + 3; // +3 because: 1-based, +1 for title row, +1 for header row
                        break;
                    }
                }

                if (rowIndex == -1)
                {
                    _logger.LogWarning($"Position not found in schedule: {position}");
                    return false;
                }

                // Determine column based on time slot
                var column = timeSlot switch
                {
                    "8:30" => "B",
                    "9:45" => "C",
                    "11:00" => "D",
                    "6:00" => "E",
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

                _logger.LogInformation($"Updated {position} at {timeSlot} for {date} to {volunteerName} in cell {column}{rowIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating position: {position} at {timeSlot}");
                return false;
            }
        }

        // PUBLIC method to get positions (in case you need it elsewhere)
        public async Task<List<string>> GetPositionsAsync()
        {
            return await LoadPositionsFromSheetAsync();
        }
    }
}