using System; // Provides fundamental classes and base types, such as string.
using System.Collections.Generic; // Provides classes for generic collections (not used in this file).
using System.Linq; // Provides LINQ functionality for querying collections (not used in this file).
using System.Text; // Provides classes for text encoding (not used in this file).
using System.Threading.Tasks; // Supports asynchronous programming (not used in this file).

namespace FilesyncPilot.Models // Defines the namespace grouping related model classes for the FilesyncPilot application.
{
    // AppSettings: This class represents the configuration settings for the application.
    // It holds the folder paths needed by the application to monitor, process, and store files.
    public class AppSettings
    {
        // ImportantFolder:
        // The folder path where the application will monitor for incoming files.
        // Files placed in this folder are considered for processing.
        public string ImportantFolder { get; set; }

        // SuccessFolder:
        // The folder path where files identified as "important" (e.g., containing a specific text) are moved.
        // This setting helps the application know where to store files that pass the content check.
        public string SuccessFolder { get; set; }

        // FailureFolder:
        // The folder path where files that do not meet the "important" criteria are moved.
        // This allows the application to segregate files that do not have the required content.
        public string FailureFolder { get; set; }
    }
}
