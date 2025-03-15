using System; // Provides fundamental classes and base types, such as string.
using System.Collections.Generic; // Includes generic collection types (not used in this file).
using System.Linq; // Provides LINQ functionality (not used in this file).
using System.Text; // Provides classes for handling text (not used in this file).
using System.Threading.Tasks; // Provides support for asynchronous programming (not used in this file).

namespace FilesyncPilot.Services // Namespace grouping related service classes for the FilesyncPilot application.
{
    // Static class FileContentChecker contains helper methods for evaluating file content.
    // Being static means you don't need to create an instance to use its methods.
    public static class FileContentChecker
    {
        // IsImportant: Checks if the file content contains the specific important line.
        // This static method is designed to be called from anywhere in the application that needs to determine
        // if a file's content qualifies as "important". For example, it might be used in FileMonitorServices when processing files.
        public static bool IsImportant(string content)
        {
            // Returns true if the provided content string contains the exact phrase "This is important File".
            return content.Contains("This is important File");
        }
    }
}
