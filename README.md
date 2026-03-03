# 🛡️ Church Security Scheduler

A web-based application for managing church security volunteer schedules across multiple service times. Built with ASP.NET Core and integrated with Google Sheets for data storage.

## Features

### Schedule Management
- **Multiple Schedule Management**: Create and manage security schedules for different dates
- **Day of Week Display**: Quickly see which day of the week each schedule is for
- **Dynamic Position Loading**: Positions are loaded from a configurable "Positions" tab in Google Sheets
- **Four Time Slots**: Support for morning and evening services (8:30 AM, 9:45 AM, 11:00 AM, 6:00 PM)

### Volunteer Assignment
- **Multi-Person Assignments**: Assign multiple volunteers to the same position and time slot
- **Easy Add/Remove**: Add volunteers with a simple dialog, remove individuals with dedicated buttons
- **Comma-Separated Storage**: Multiple volunteers stored as comma-separated values in Google Sheets

### User Experience
- **Sticky Headers & Columns**: Excel-like "freeze panes" - position column and time slot headers stay visible when scrolling
- **Fully Responsive Design**: Mobile-friendly interface that works perfectly on phones, tablets, and desktops
- **Touch-Optimized**: Smooth scrolling on mobile devices with touch-friendly buttons
- **Professional Layout**: Clean, modern interface with color-coded elements

### Data & Integration
- **Google Sheets Integration**: All data is stored and synced with Google Sheets in real-time
- **Dynamic Position Management**: Add or modify positions in Google Sheets without code changes
- **Automatic Day Calculation**: System automatically calculates and displays day of week for each schedule

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

### Configure Google Sheets

Your spreadsheet should have:
1. **Positions Tab**: A sheet named "Positions" with position names in column A (one per row)
2. **Schedule Tabs**: Individual date tabs will be created automatically (format: YYYY-MM-DD)

Example Positions tab:

| Column A   |
|------------|
| Position 1 |
| Position 2 |
| Position 3 |

### Configure the Application

Update your `appsettings.json` or environment variables with:
- `GoogleSheets:SpreadsheetId` - Your Google Sheets spreadsheet ID
- `GoogleSheets:CredentialsPath` - Path to your service account JSON key file (for local)
- `GOOGLE_CREDENTIALS_JSON` - Full JSON credentials as environment variable (for Cloud Run)
- `GoogleSheets:ApplicationName` - Your application name

### Run the Application Locally

To be added...

The application will be available at http://localhost:5000 (or the port specified in your launch settings).

### Deploy to Google Cloud Run

## Usage

### Creating a New Schedule

1. Navigate to the home page
2. Select a date using the date picker
3. Click "Create Schedule"
4. The system will create a new sheet in your Google Sheets document with all positions from the Positions tab

### Managing Volunteers

1. Click on a date from the existing schedules list
2. Click "+ Add Person" in any position/time slot to add a volunteer
3. Enter the volunteer's name in the prompt
4. To add multiple volunteers to the same slot, simply add more people - they'll be displayed on separate lines
5. Click "Remove" next to a volunteer's name to remove them individually

### Managing Positions

1. Open your Google Sheets spreadsheet
2. Navigate to the "Positions" tab
3. Add, edit, or remove position names in column A
4. Changes will automatically appear when creating new schedules or viewing existing ones

### Mobile Usage

- **Horizontal Scrolling**: Swipe left/right to see all time slots; the position column stays visible
- **Vertical Scrolling**: Scroll up/down through positions; the time slot headers stay visible
- **Touch-Friendly Buttons**: Large, easy-to-tap buttons optimized for mobile use

## Project Structure

- **Models/** - Data models (ScheduleSheet, SecurityPosition)
- **Services/** - Google Sheets integration service
- **Program.cs** - Application entry point, routing, and HTML generation

## Technologies Used

- **ASP.NET Core 10.0**: Minimal API framework
- **Google Sheets API v4**: Data storage and synchronization
- **HTML/CSS/JavaScript**: Frontend interface with responsive design
- **C# 14.0**: Programming language

## Configuration

The application uses the following time slots:
- 8:30 AM (Morning service)
- 9:45 AM (Morning service)
- 11:00 AM (Late morning service)
- 6:00 PM (Evening service)

Positions are dynamically loaded from the "Positions" tab in your Google Sheets spreadsheet.

## Recent Enhancements

✅ **Sticky Headers & Columns** - Excel-like freeze panes for better mobile experience  
✅ **6:00 PM Time Slot** - Full support for evening service scheduling  
✅ **Dynamic Position Loading** - Positions automatically sync from Google Sheets  
✅ **Multi-Person Assignments** - Multiple volunteers per position with individual remove buttons  
✅ **Day of Week Display** - Schedule list shows day names for quick reference  
✅ **Mobile Optimization** - Touch-friendly interface with smooth scrolling

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions or issues, please open an issue on the GitHub repository at https://github.com/hkearn777/ChurchSecurityScheduler

## Acknowledgments

- Built for church security team coordination
- Uses Google Sheets for easy data management and sharing
- Designed with mobile-first approach for on-the-go schedule management