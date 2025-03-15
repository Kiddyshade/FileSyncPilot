using System; // Provides base classes and fundamental types (e.g., exceptions, basic data types).
using System.IO; // Provides input/output functionalities, such as file operations and FileSystemWatcher.
using System.Threading; // Provides classes and methods for threading, including CancellationToken.
using System.Threading.Tasks; // Supports asynchronous programming with Task and async/await.
using Microsoft.Extensions.Hosting; // Provides abstractions for hosting, including BackgroundService which this class extends.
using Microsoft.Extensions.Logging; // Enables logging of information, warnings, and errors.
using Microsoft.Extensions.Options; // Enables access to configuration settings via IOptions<T> pattern.
using Microsoft.Extensions.Caching.Memory;
using FilesyncPilot.Models; // Provides custom model classes (like AppSettings) defined in the project for configuration.
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Concurrent;
using Polly;
using Polly.Retry;
using System.IO;
using System.Collections.Generic; // Provides generic collection classes (e.g., List<T>); not directly used here.
using System.Linq; // Provides LINQ functionality for querying collections; not directly used here.
using System.Text; // Provides classes for handling text encoding; not directly used here.
using System.Threading.Tasks; // (Duplicate) Supports asynchronous programming.s

namespace FilesyncPilot.Services
{
    // FileMonitorServices: A background service that monitors a specific folder for new files.
    // Inherits from BackgroundService, meaning its ExecuteAsync method is called by the hosting environment when the service starts.
    public class FileMonitorServices : BackgroundService
    {
        // This is a logger that helps us keep track of what's happening in our program
        // It's like a notebook where we can write down important events and errors
        private readonly ILogger<FileMonitorServices> _logger;

        // This holds the configuration values for our program, like folder paths
        // It's like a settings menu where we can adjust how our program works
        private readonly AppSettings _settings;

        // This is a tool that watches the file system for changes in a specific folder
        // It's like a sentinel that alerts us when something new happens in the folder
        private FileSystemWatcher _watcher;

        private static readonly ConcurrentDictionary<string, DateTime> _processedFiles = new();

        private static readonly MemoryCache ProcessedFilesCache = new MemoryCache(new MemoryCacheOptions());

        // Constructor: Called when an instance of FileMonitorServices is created.
        // The hosting environment injects the logger and configuration settings.
        public FileMonitorServices(
        // This is like getting a notebook (logger) to write down what happens in our service
        ILogger<FileMonitorServices> logger,
        // This is like getting a settings menu (configuration) for our program
        IOptions<AppSettings> settings)
        {
            // Store the notebook (logger) so we can use it later to write down events
            _logger = logger;

            // Open the settings menu and save the actual settings (like folder paths)
            _settings = settings.Value;

            // Now that we have our notebook and settings, set up the folder watcher
            setupWatcher(); // Like turning on a security camera for the folder
        }

        // setupWatcher: Initializes the FileSystemWatcher to monitor the folder specified in _settings.ImportantFolder.
        // This method is called from the constructor.
        // This method sets up a FileSystemWatcher to monitor a specific directory for changes
        // It's called from the constructor, so it runs when the service is created
        private void setupWatcher()
        {
            // Create a new FileSystemWatcher that watches the directory defined in the configuration
            // Think of it like setting up a security camera to watch a specific area
            _watcher = new FileSystemWatcher(_settings.ImportantFolder)
            {
                // Set the filter to watch all file types (not just specific ones like .txt or .docx)
                // It's like telling the camera to watch for any kind of movement, not just specific things
                Filter = "*.*",
                // Start raising events immediately so that changes are detected as soon as they happen
                // It's like turning on the camera and telling it to start alerting us right away
                EnableRaisingEvents = true
            };

            // Attach an event handler to the FileSystemWatcher
            // This means that when a specific event happens (like a new file being created), a certain method will be called automatically
            // It's like programming the camera to call a specific phone number when it detects movement
            _watcher.Created += OnNewFileCreated;
            // The OnNewFileCreated method is the one that will be called when a new file is created
            // It's like the camera calling the phone number and saying "Hey, I detected something!"

            // Log a message to say that the FileSystemWatcher is now actively monitoring the specified folder
            // It's like sending a notification to say "The camera is now watching the area"
            _logger.LogInformation("FileSystemWatcher is monitoring folder: {folder}", _settings.ImportantFolder);
        }

        // OnNewFileCreated: Called automatically by the FileSystemWatcher when a new file is created in the monitored folder.
        // This event handler reads the file, checks its content, and moves it based on the content.
        // This method is called when a new file is created in the monitored directory
        private async void OnNewFileCreated(object sender, FileSystemEventArgs e)
        {

            // This line logs a message to our logging system that we've detected a new file
            // The full path of the file is included in the log message so we know exactly which file was found
            _logger.LogInformation("Detected new file: {file}", e.FullPath);

            if(!ProcessedFilesCache.TryGetValue(e.FullPath,out _))
            {
                ProcessedFilesCache.Set(e.FullPath, true, TimeSpan.FromSeconds(5));
            }

            else
            {
                _logger.LogInformation("Duplicate event for file {file}, skipping.", e.FullPath);
                return;
            }

            if (!File.Exists(e.FullPath))
            {
                _logger.LogWarning("File {file} does not exist at processing time, skipping event", e.FullPath);
                return;
            }


            try
            {
                    // Here we pause execution for 500 milliseconds (half a second)
                    // This delay gives the system time to finish writing the file completely
                    // Without this delay, we might try to open a file that's still being created
                    await Task.Delay(500);
                             

                // We create a variable named "content" that will hold the text from the file
                string content;

                // This line checks if the file extension is ".docx" (a Word document)
                // The check is case-insensitive, so ".DOCX", ".docx", etc. will all match
                if (Path.GetExtension(e.FullPath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    // If it's a Word document, we use a special helper method to extract the text
                    // Regular file reading won't work for Word docs since they're not plain text
                    content = ExtractTextFromDocx(e.FullPath);
                }
                else
                {
                    // For any other file type, we read all the text from the file
                    // The "await" keyword means we'll wait for the reading to complete before continuing
                    content = await File.ReadAllTextAsync(e.FullPath);
                }

                // Create a retry policy to handle file-moving errors
                // This gives us multiple chances to move files if something goes wrong
                AsyncRetryPolicy retryPolicy = Policy.Handle<IOException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        // Log a warning whenever we need to retry moving a file
                        _logger.LogWarning(exception, "Retry {RetryCount} after " +
                            "{Delay} due to error moving file: {file}",
                            retryCount, timeSpan, e.FullPath);
                    });

                if (!File.Exists(e.FullPath))
                {
                    _logger.LogWarning("File {file} no longer exists before move. Skipping", e.FullPath);
                    return;
                }

                // Now we check if the file content contains the specific text "This is important File"
                if (content.Contains("This is important File"))
                {
                    // If the text is found, we'll move the file to the success folder
                    // First we create the full path to the destination file
                    var destination = Path.Combine(_settings.SuccessFolder, Path.GetFileName(e.FullPath));

                    await retryPolicy.ExecuteAsync(() => Task.Run(() =>
                    {
                        if (File.Exists(e.FullPath))
                            File.Move(e.FullPath, destination);
                        else
                            throw new FileNotFoundException($"File not found before" +
                                $"moving.", e.FullPath);
                    }));

                    // Finally, we log that we've successfully moved the file
                    _logger.LogInformation("Moved file to success folder: {destination}", destination);
                    
                }
                else
                {
                    // If the text wasn't found, we'll move the file to the failure folder instead
                    // First we create the full path to the destination file in the failure folder
                    var destination = Path.Combine(_settings.FailureFolder, Path.GetFileName(e.FullPath));


                    await retryPolicy.ExecuteAsync(() => Task.Run(() =>
                    {
                        if (File.Exists(e.FullPath))
                            File.Move(e.FullPath, destination);
                        else
                            throw new FileNotFoundException($"File not found before" +
                                $"moving.", e.FullPath);
                    }));

                    // Finally, we log that we've moved the file to the failure folder
                    _logger.LogInformation("Moved file to failure folder: {destination}", destination);
                }
            }
            catch(FileNotFoundException fnfEx)
            {
                // This means the file was likely already processed by another event; log as a
                // warning
                _logger.LogWarning(fnfEx, "File {file} was not found during move operation. It may have" +
                    "been processed already.", e.FullPath);
            }
            catch (Exception ex)
            {
                // This entire block handles any errors that might occur during the process
                // The "catch" keyword catches any exceptions (errors) that happened in the "try" block

                // Here we log the error details, including what type of error occurred
                // We also include which file caused the problem to help with troubleshooting
                _logger.LogError(ex, "Error processing file: {file}", e.FullPath);
            }
        }

        private string ExtractTextFromDocx(string filepath)
        {
            // This line declares a method (a function) named "ExtractTextFromDocx" that takes a file path as input
            // Think of this like creating a recipe that needs to know which ingredient (file) to work with

            StringBuilder text = new StringBuilder();
            // Here we create a special container called StringBuilder to hold text pieces
            // This is like getting an empty bucket ready to collect water drops - each drop is a piece of text
            // We use StringBuilder instead of regular string because it's more efficient for building text piece by piece

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filepath, false))
            {
                // This line opens the Word document for reading (the "false" means "don't allow changes")
                // It's like opening a book to read it, but promising not to write in it
                // The "using" statement ensures the book gets closed properly when we're done

                var body = wordDoc.MainDocumentPart.Document.Body;
                // Here we're navigating to the main content of the document
                // This is like finding the main chapters of a book, skipping the cover, table of contents, etc.
                // - wordDoc is the whole Word file
                // - MainDocumentPart is like the section containing the actual content (not styles or settings)
                // - Document is the overall structure of that content
                // - Body is where the actual text lives (like the pages of a book)

                text.Append(body.InnerText);
                // This line extracts all the text content and adds it to our StringBuilder container
                // It's like copying all the words from our book into our bucket
                // "InnerText" gives us just the readable text, ignoring formatting instructions
            }
            // At this point, the document automatically closes because of the "using" statement
            // This is like making sure we close the book and put it back on the shelf

            return text.ToString();
            // Finally, we convert our collected text pieces into a single string and return it
            // It's like pouring all the collected water drops from our bucket into a single glass
            // The ToString() converts our StringBuilder into a regular string that other code can use
        }

        // ExecuteAsync: The main method that runs when the background service starts.
        // Called automatically by the hosting environment.
        // This method is where the background service starts executing
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Log a message to say that the FileMonitorService has started running
            _logger.LogInformation("FileMonitorService is running");

            // Set up a callback that will run when the service is stopping
            // This callback will log a message to say that the service is stopping
            stoppingToken.Register(() => _logger.LogInformation("FileMonitorService is stopping."));

            // Since this service uses a FileSystemWatcher to handle the work,
            // it doesn't need to run a continuous loop to keep itself busy
            // So, it can just return a completed Task to indicate that it's done
            return Task.CompletedTask;
        }

        // Dispose: Cleans up resources when the service is shutting down.
        // Called automatically by the hosting environment during service disposal.
        // This method is used to clean up and release any system resources when the service is stopped
        public override void Dispose()
        {
            // Dispose of the FileSystemWatcher to release system resources
            // This is like turning off a machine to save energy
            _watcher.Dispose();

            // Call the base class Dispose method for any additional cleanup
            // This is like making sure the whole house is clean, not just one room
            base.Dispose();
        }
    }
}


