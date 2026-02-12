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
    <title>Church Security Scheduler</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 50px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; }
        .date-list { margin: 20px 0; }
        .date-item { padding: 15px; margin: 10px 0; background: #f8f9fa; border-left: 4px solid #4285f4; cursor: pointer; }
        .date-item:hover { background: #e9ecef; }
        .btn { display: inline-block; padding: 12px 24px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; margin-top: 20px; }
        .btn:hover { background: #3367d6; }
        input[type='date'] { padding: 10px; font-size: 16px; border: 1px solid #ddd; border-radius: 4px; }
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
                        html += $"<div class='date-item' onclick=\"window.location.href='/schedule/{date}'\">{date}</div>";
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
    <title>Security Schedule - {date}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1400px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; }}
        th {{ background: #4285f4; color: white; padding: 15px; text-align: left; font-size: 16px; }}
        td {{ padding: 15px; border: 1px solid #ddd; }}
        tr:hover {{ background: #f8f9fa; }}
        .position-cell {{ font-weight: bold; background: #f8f9fa; }}
        .slot-cell {{ text-align: center; cursor: pointer; min-height: 50px; }}
        .slot-cell:hover {{ background: #e3f2fd; }}
        .volunteer-name {{ color: #2c5282; font-weight: bold; }}
        .empty-slot {{ color: #999; font-style: italic; }}
        .back-link {{ display: inline-block; margin-bottom: 20px; color: #4285f4; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
        input {{ width: 90%; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }}
        .btn {{ padding: 8px 16px; background: #4285f4; color: white; border: none; border-radius: 4px; cursor: pointer; margin: 5px; }}
        .btn-clear {{ background: #d93025; }}
    </style>
</head>
<body>
    <div class='container'>
        <a href='/' class='back-link'>← Back to Dates</a>
        <h1>🛡️ Security Schedule - {date}</h1>
        <table>
            <tr>
                <th>Position</th>
                <th>8:30 AM</th>
                <th>9:45 AM</th>
                <th>11:00 AM</th>
            </tr>";

                foreach (var pos in schedule.Positions)
                {
                    var position = pos.Position;
                    var positionId = position.Replace(" ", "");
                    var slot830 = pos.TimeSlot8_30;
                    var slot945 = pos.TimeSlot9_45;
                    var slot1100 = pos.TimeSlot11_00;

                    html += $@"
            <tr>
                <td class='position-cell'>{position}</td>
                <td class='slot-cell' id='slot-{positionId}-830'>
                    {(string.IsNullOrEmpty(slot830) ? "<span class='empty-slot'>Click to assign</span>" : $"<span class='volunteer-name'>{slot830}</span>")}
                    <br>
                    <button class='btn' onclick=""editSlot('{position}', '8:30', '{slot830}')"">Edit</button>
                    {(string.IsNullOrEmpty(slot830) ? "" : $"<button class='btn btn-clear' onclick=\"clearSlot('{position}', '8:30')\">Clear</button>")}
                </td>
                <td class='slot-cell' id='slot-{positionId}-945'>
                    {(string.IsNullOrEmpty(slot945) ? "<span class='empty-slot'>Click to assign</span>" : $"<span class='volunteer-name'>{slot945}</span>")}
                    <br>
                    <button class='btn' onclick=""editSlot('{position}', '9:45', '{slot945}')"">Edit</button>
                    {(string.IsNullOrEmpty(slot945) ? "" : $"<button class='btn btn-clear' onclick=\"clearSlot('{position}', '9:45')\">Clear</button>")}
                </td>
                <td class='slot-cell' id='slot-{positionId}-1100'>
                    {(string.IsNullOrEmpty(slot1100) ? "<span class='empty-slot'>Click to assign</span>" : $"<span class='volunteer-name'>{slot1100}</span>")}
                    <br>
                    <button class='btn' onclick=""editSlot('{position}', '11:00', '{slot1100}')"">Edit</button>
                    {(string.IsNullOrEmpty(slot1100) ? "" : $"<button class='btn btn-clear' onclick=\"clearSlot('{position}', '11:00')\">Clear</button>")}
                </td>
            </tr>";
                }

                html += $@"
        </table>
    </div>
    <script>
        function editSlot(position, timeSlot, currentName) {{
            const name = prompt('Enter volunteer name:', currentName);
            if (name !== null) {{
                window.location.href = '/schedule/{date}/update?position=' + encodeURIComponent(position) + '&timeSlot=' + timeSlot + '&name=' + encodeURIComponent(name);
            }}
        }}

        function clearSlot(position, timeSlot) {{
            if (confirm('Clear this assignment?')) {{
                window.location.href = '/schedule/{date}/update?position=' + encodeURIComponent(position) + '&timeSlot=' + timeSlot + '&name=';
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
    }
}
