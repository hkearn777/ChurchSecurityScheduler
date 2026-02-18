# 🛡️ Church Security Scheduler

A web-based application for managing church security volunteer schedules across multiple service times. Built with ASP.NET Core and integrated with Google Sheets for data storage.

## Features

- **Multiple Schedule Management**: Create and manage security schedules for different dates
- **Time Slot Organization**: Support for three service times (8:30 AM, 9:45 AM, 11:00 AM)
- **Position-Based Assignments**: Assign multiple volunteers to different security positions
- **Google Sheets Integration**: All data is stored and synced with Google Sheets
- **Responsive Design**: Mobile-friendly interface that works on all devices
- **Easy Volunteer Management**: Add or remove volunteers with simple button clicks

## Prerequisites

- .NET 10 SDK or later
- A Google Cloud Platform account with Sheets API enabled
- Google Sheets API credentials (service account JSON key)

## Getting Started

### Set Up Google Sheets API

1. Go to Google Cloud Console (console.cloud.google.com)
2. Create a new project or select an existing one
3. Enable the Google Sheets API
4. Create a service account and download the JSON key file
5. Share your Google Sheets spreadsheet with the service account email address (found in the JSON file)

### Configure the Application

Update the GoogleSheetsService.cs file with your:
- Google Sheets spreadsheet ID
- Path to your service account JSON key file
- Application name

### Run the Application

Use `dotnet run` to start the application. It will be available at http://localhost:5000 (or the port specified in your launch settings).

## Usage

### Creating a New Schedule

1. Navigate to the home page
2. Select a date using the date picker
3. Click "Create Schedule"
4. The system will create a new sheet in your Google Sheets document

### Managing Volunteers

1. Click on a date from the existing schedules list
2. Click "+ Add Person" in any position/time slot to add a volunteer
3. Enter the volunteer's name in the prompt
4. Click "Remove" next to a volunteer's name to remove them

### Viewing Schedules

- All existing schedules are listed on the home page
- Click any date to view and edit that schedule
- Each schedule shows all positions and time slots in a table format

## Project Structure

- **Models/** - Data models (Schedule, PositionSlot)
- **Services/** - Google Sheets integration service
- **Program.cs** - Application entry point and routing

## Technologies Used

- **ASP.NET Core 10.0**: Minimal API framework
- **Google Sheets API v4**: Data storage and synchronization
- **HTML/CSS/JavaScript**: Frontend interface
- **C# 14.0**: Programming language

## Configuration

The application uses the following time slots:
- 8:30 AM
- 9:45 AM
- 11:00 AM

Positions are defined in your Google Sheets spreadsheet and loaded dynamically.

## Contributing

1. Fork the repository
2. Create a feature branch (feature/amazing-feature)
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions or issues, please open an issue on the GitHub repository at https://github.com/hkearn777/ChurchSecurityScheduler

## Acknowledgments

- Built for church security team coordination
- Uses Google Sheets for easy data management and sharing