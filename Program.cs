using ChurchSecurityScheduler.Services;
using ChurchSecurityScheduler.Models;

namespace ChurchSecurityScheduler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddSingleton<GoogleSheetsService>();

            var app = builder.Build();

            // Home page - Date selection
            app.MapGet("/", async (GoogleSheetsService sheetsService) =>
            {
                var dates = await sheetsService.GetAvailableDatesAsync();

                var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Church Security Scheduler</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; margin: 0; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; font-size: 1.8em; margin-top: 0; }
        h3 { font-size: 1.2em; }
        p { font-size: 1em; }
        .date-list { margin: 20px 0; }
        .date-item { padding: 15px; margin: 10px 0; background: #f8f9fa; border-left: 4px solid #4285f4; cursor: pointer; font-size: 1em; }
        .date-item:hover { background: #e9ecef; }
        .day-of-week { color: #666; font-weight: normal; margin-left: 10px; }
        .btn { display: inline-block; padding: 12px 24px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; margin-top: 20px; border: none; font-size: 1em; cursor: pointer; }
        .btn:hover { background: #3367d6; }
        input[type='date'] { padding: 10px; font-size: 1em; border: 1px solid #ddd; border-radius: 4px; width: 100%; max-width: 300px; box-sizing: border-box; }
        
        @media (max-width: 600px) {
            body { padding: 10px; }
            .container { padding: 15px; }
            h1 { font-size: 1.5em; }
            input[type='date'] { max-width: 100%; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🛡️ Church Security Scheduler</h1>
        <p>Select a date to view or create a security schedule:</p>
        
        <h3>Create New Schedule</h3>
        <form method='get' action='/schedule/create'>
            <input type='date' name='date' required>
            <button type='submit' class='btn'>Create Schedule</button>
        </form>

        <h3>Existing Schedules</h3>
        <div class='date-list'>";

                if (dates.Count == 0)
                {
                    html += "<p>No schedules created yet. Create your first one above!</p>";
                }
                else
                {
                    foreach (var date in dates)
                    {
                        var dateObj = DateTime.Parse(date);
                        var dayOfWeek = dateObj.ToString("dddd");
                        html += $"<div class='date-item' onclick=\"window.location.href='/schedule/{date}'\">{date} <span class='day-of-week'>({dayOfWeek})</span></div>";
                    }
                }

                html += @"
        </div>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Create new schedule
            app.MapGet("/schedule/create", async (HttpContext context, GoogleSheetsService sheetsService) =>
            {
                var dateStr = context.Request.Query["date"].ToString();
                if (string.IsNullOrEmpty(dateStr))
                    return Results.Redirect("/");

                var date = DateTime.Parse(dateStr);
                var formattedDate = date.ToString("yyyy-MM-dd");

                await sheetsService.CreateSheetForDateAsync(formattedDate);
                return Results.Redirect($"/schedule/{formattedDate}");
            });

            // View/Edit schedule
            app.MapGet("/schedule/{date}", async (string date, GoogleSheetsService sheetsService) =>
            {
                var schedule = await sheetsService.GetScheduleForDateAsync(date);

                if (schedule == null)
                {
                    return Results.Text("<h1>Schedule not found</h1><a href='/'>← Back</a>", "text/html");
                }

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Security Schedule - {date}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 10px; background: #f5f5f5; margin: 0; }}
        .container {{ max-width: 1400px; margin: 0 auto; background: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; font-size: 1.5em; margin-top: 0; }}
        .table-wrapper {{ overflow: auto; -webkit-overflow-scrolling: touch; max-height: calc(100vh - 120px); position: relative; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; min-width: 600px; }}
        
        /* Sticky header row */
        th {{ 
            background: #4285f4; 
            color: white; 
            padding: 12px 8px; 
            text-align: left; 
            font-size: 0.9em; 
            position: sticky;
            top: 0;
            z-index: 10;
        }}
        
        /* Sticky first column (Position) */
        td:first-child,
        th:first-child {{            position: sticky;
            left: 0;
            z-index: 5;
            background: #f8f9fa;
        }}
        
        /* Top-left cell gets highest z-index */
        th:first-child {{            z-index: 15;
            background: #4285f4;
        }}
        
        td {{ padding: 10px 8px; border: 1px solid #ddd; vertical-align: top; background: white; }}
        tr:hover td {{ background: #f8f9fa; }}
        tr:hover td:first-child {{ background: #e9ecef; }}
        
        .position-cell {{ font-weight: bold; background: #f8f9fa; font-size: 0.9em; }}
        .slot-cell {{ min-height: 50px; }}
        .volunteer-list {{ margin: 5px 0; }}
        .volunteer-item {{ display: flex; justify-content: space-between; align-items: center; padding: 8px; margin: 5px 0; background: #e3f2fd; border-radius: 4px; gap: 5px; }}
        .volunteer-name {{ color: #2c5282; font-weight: bold; flex-grow: 1; font-size: 0.9em; word-break: break-word; }}
        .empty-slot {{ color: #999; font-style: italic; padding: 10px; font-size: 0.85em; }}
        .back-link {{ display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; font-size: 1em; }}
        .back-link:hover {{ text-decoration: underline; }}
        .btn {{ padding: 8px 12px; background: #4285f4; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 0.85em; white-space: nowrap; }}
        .btn:hover {{ background: #3367d6; }}
        .btn-small {{ padding: 5px 8px; font-size: 0.75em; }}
        .btn-remove {{ background: #d93025; }}
        .btn-remove:hover {{ background: #b52a1f; }}
        .btn-add {{ background: #34a853; width: 100%; margin-top: 5px; }}
        .btn-add:hover {{ background: #2d8e47; }}
        
        @media (max-width: 768px) {{
            body {{
                padding: 5px;
            }}
            .container {{
                padding: 10px;
                border-radius: 4px;
            }}
            h1 {{
                font-size: 1.2em;
            }}
            .table-wrapper {{
                max-height: calc(100vh - 100px);
            }}
            th {{
                padding: 8px 4px;
                font-size: 0.8em;
            }}
            td {{
                padding: 8px 4px;
            }}
            .position-cell {{
                font-size: 0.8em;
            }}
            .volunteer-name {{
                font-size: 0.85em;
            }}
            .volunteer-item {{
                padding: 6px;
            }}
            .btn {{
                padding: 6px 10px;
                font-size: 0.8em;
            }}
            .btn-small {{
                padding: 4px 6px;
                font-size: 0.7em;
            }}
            .btn-add {{
                font-size: 0.8em;
            }}
            table {{
                min-width: 500px;
            }}
        }}
        
        @media (max-width: 480px) {{
            h1 {{
                font-size: 1em;
            }}
            .table-wrapper {{
                max-height: calc(100vh - 80px);
            }}
            th {{
                padding: 6px 3px;
                font-size: 0.75em;
            }}
            td {{
                padding: 6px 3px;
            }}
            .position-cell {{
                font-size: 0.75em;
            }}
            .volunteer-name {{
                font-size: 0.8em;
            }}
            .btn {{
                font-size: 0.75em;
                padding: 5px 8px;
            }}
            .btn-small {{
                padding: 3px 5px;
                font-size: 0.65em;
            }}
            table {{
                min-width: 450px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <a href='/' class='back-link'>← Back to Dates</a>
        <h1>🛡️ Security Schedule - {date}</h1>
        <div class='table-wrapper'>
        <table>
            <tr>
                <th>Position</th>
                <th>8:30 AM</th>
                <th>9:45 AM</th>
                <th>11:00 AM</th>
                <th>6:00 PM</th>
            </tr>";

                foreach (var pos in schedule.Positions)
                {
                    var position = pos.Position;
                    
                    // Parse comma-separated volunteers
                    var volunteers830 = string.IsNullOrWhiteSpace(pos.TimeSlot8_30) 
                        ? new List<string>() 
                        : pos.TimeSlot8_30.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                    
                    var volunteers945 = string.IsNullOrWhiteSpace(pos.TimeSlot9_45) 
                        ? new List<string>() 
                        : pos.TimeSlot9_45.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                    
                    var volunteers1100 = string.IsNullOrWhiteSpace(pos.TimeSlot11_00) 
                        ? new List<string>() 
                        : pos.TimeSlot11_00.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                    
                    // ✅ Add this parsing for 6:00 PM
                    var volunteers600 = string.IsNullOrWhiteSpace(pos.TimeSlot6_00) 
                        ? new List<string>() 
                        : pos.TimeSlot6_00.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();

                    html += $@"
            <tr>
                <td class='position-cell'>{position}</td>
                <td class='slot-cell'>
                    {GenerateVolunteerListHtml(date, position, "8:30", volunteers830)}
                </td>
                <td class='slot-cell'>
                    {GenerateVolunteerListHtml(date, position, "9:45", volunteers945)}
                </td>
                <td class='slot-cell'>
                    {GenerateVolunteerListHtml(date, position, "11:00", volunteers1100)}
                </td>
                <td class='slot-cell'>
                    {GenerateVolunteerListHtml(date, position, "6:00", volunteers600)}
                </td>
            </tr>";
                }

                html += $@"
        </table>
        </div>
    </div>
    <script>
        function addPerson(position, timeSlot, currentNames) {{            const name = prompt('Enter volunteer name:');
            if (name !== null && name.trim() !== '') {{
                const newNames = currentNames ? currentNames + ', ' + name.trim() : name.trim();
                window.location.href = '/schedule/{date}/update?position=' + encodeURIComponent(position) + '&timeSlot=' + timeSlot + '&name=' + encodeURIComponent(newNames);
            }}
        }}

        function removePerson(position, timeSlot, personToRemove, currentNames) {{
            if (confirm('Remove ' + personToRemove + ' from this position?')) {{
                const names = currentNames.split(',').map(n => n.trim()).filter(n => n !== personToRemove);
                const newNames = names.join(', ');
                window.location.href = '/schedule/{date}/update?position=' + encodeURIComponent(position) + '&timeSlot=' + timeSlot + '&name=' + encodeURIComponent(newNames);
            }}
        }}
    </script>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Update position
            app.MapGet("/schedule/{date}/update", async (string date, HttpContext context, GoogleSheetsService sheetsService) =>
            {
                var position = context.Request.Query["position"].ToString();
                var timeSlot = context.Request.Query["timeSlot"].ToString();
                var name = context.Request.Query["name"].ToString();

                await sheetsService.UpdatePositionAsync(date, position, timeSlot, name);
                return Results.Redirect($"/schedule/{date}");
            });

            app.Run();
        }

        private static string GenerateVolunteerListHtml(string date, string position, string timeSlot, List<string> volunteers)
        {
            var html = "<div class='volunteer-list'>";
            
            if (volunteers.Count == 0)
            {
                html += "<div class='empty-slot'>No one assigned</div>";
            }
            else
            {
                var currentNames = string.Join(", ", volunteers);
                foreach (var volunteer in volunteers)
                {
                    var escapedVolunteer = volunteer.Replace("'", "\\'");
                    var escapedNames = currentNames.Replace("'", "\\'");
                    html += $@"
                    <div class='volunteer-item'>
                        <span class='volunteer-name'>{volunteer}</span>
                        <button class='btn btn-small btn-remove' onclick=""removePerson('{position}', '{timeSlot}', '{escapedVolunteer}', '{escapedNames}')"">Remove</button>
                    </div>";
                }
            }
            
            var currentNamesForAdd = volunteers.Count > 0 ? string.Join(", ", volunteers).Replace("'", "\\'") : "";
            html += $@"
                <button class='btn btn-add' onclick=""addPerson('{position}', '{timeSlot}', '{currentNamesForAdd}')"">+ Add Person</button>
            </div>";
            
            return html;
        }
    }
}
