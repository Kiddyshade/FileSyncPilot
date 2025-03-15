# FileSync Pilot

A .NET Core Worker Service that monitors a specified folder for new files, determines if they contain a specific line of text, and moves them to either a “success” or “failure” folder. The service can also handle .docx files (using the Open XML SDK for text extraction), log events to Seq or another logging provider, and run continuously as a Windows Service.

## Features

- **Continuous Monitoring**: Uses `FileSystemWatcher` to watch an "Important" folder for new files.  
- **Conditional File Moving**: Moves files to "Success" or "Failure" folders based on their content.  
- **.docx Support**: Integrates the Open XML SDK to extract text from `.docx` files.  
- **Resilience**: Implements retries using Polly for handling transient file I/O errors.  
- **Duplicate Event Handling**: Debounces duplicate file events to avoid multiple moves for the same file.  
- **Health Checks**: (Optional) Provides a `/health` endpoint for monitoring service status.  
- **Logging**: Uses Serilog with optional centralized logging in Seq.

## Tech Stack

- **.NET 6/7/8** (Worker Service / BackgroundService)
- **C#** (Main language)
- **FileSystemWatcher** (Monitoring file changes)
- **Polly** (Retry library for resilience)
- **Open XML SDK** (Text extraction from .docx files)
- **Serilog** (Logging) + **Serilog.Sinks.Seq** (Optional for Seq logging)
- **Microsoft.Extensions.Diagnostics.HealthChecks** (Optional health checks)

## Prerequisites

1. **.NET SDK (6.0 or higher)**: Make sure you have the .NET SDK installed.  
2. **Optional: Seq** (for centralized logging): If you’d like to visualize logs in Seq, install Seq and configure an API key if needed.

## Installation & Setup

1. **Clone or Download** this repository to your local machine.
2. **Open the Project** in your favorite IDE (e.g., Visual Studio, Visual Studio Code).
3. **Install Dependencies** using the .NET CLI:
   ```bash
   dotnet restore
   ```
4. **Configure Folders**:
   - Edit `appsettings.json` to specify the `ImportantFolder`, `SuccessFolder`, and `FailureFolder` paths. 
   - (Optional) For Seq logging, add your Seq URL and API key in `appsettings.json` or directly in `Program.cs`.
5. **Build & Run**:
   ```bash
   dotnet build
   dotnet run
   ```
   The service will start monitoring the specified folder, moving files based on their contents.

## How It Works

1. **FileSystemWatcher** monitors the `ImportantFolder`.  
2. When a file arrives:  
   - If it’s a `.docx`, the service extracts text using the Open XML SDK.  
   - If the text contains `"This is important File"`, the file is moved to the success folder. Otherwise, it’s moved to the failure folder.  
3. **Polly** wraps file operations to retry on `IOException`.  
4. Logs are written to console and optionally forwarded to Seq.

## Deploying as a Windows Service

1. **Publish** the project:
   ```bash
   dotnet publish -c Release -o C:\publish\FileSyncPilot
   ```
2. **Register as a Windows Service** (in an elevated command prompt):
   ```bash
   sc create "FileSyncPilot" binPath= "C:\publish\FileSyncPilot\FileSyncPilot.exe" start=auto
   sc start "FileSyncPilot"
   ```
3. The service will now start automatically on system reboot.

## Contributing

1. **Fork** this repo and create a new branch for your feature or bug fix.  
2. **Open a Pull Request** describing your changes.

## License

This project is licensed under the [MIT License](LICENSE). See the `LICENSE` file for details.
