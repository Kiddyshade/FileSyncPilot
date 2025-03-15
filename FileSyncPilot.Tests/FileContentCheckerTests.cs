using Xunit; // Imports the xUnit testing framework used for writing and running tests.
using FilesyncPilot.Services; // Imports the FileContentChecker class from the application services to be tested.

namespace FileSyncPilot.Tests // Declares the namespace for the test classes.
{
    // This class contains unit tests for the FileContentChecker class.
    public class FileContentCheckerTests
    {
        /// [Fact] This line tells the testing framework (xUnit) that this is a test method
        [Fact]
        public void IsImportant_ReturnsTrue_WhenContentContainsImportantLine()
        {
            // Arrange: Prepare the test data
            // Create a sample string that includes the important line "This is important File"
            // This string simulates the content of a file that should be recognized as important
            var content = "Some text\nThis is important File\nMore text";

            // Act: Perform the action being tested
            // Call the IsImportant method from the FileContentChecker class to test its behavior
            // This method is called directly because it's a static helper method
            var result = FileContentChecker.IsImportant(content);

            // Assert: Verify the expected result
            // Check that the method returns true when the content contains the important line
            // If the result is not true, the test will fail
            Assert.True(result);
        }

        // [Fact] indicates that this is another test case to be executed by xUnit.
        [Fact]
        // This test method checks that the IsImportant method returns false when the content does not contain the important line
        public void IsImportant_ReturnFalse_WhenConnectionDoesNotContainImportantLine()
        {
            // Arrange: Prepare the test data
            // Create a sample string that does not include the important line
            // This simulates the content of a file that should not be recognized as important
            var content = "Some text without the special line.";

            // Act: Perform the action being tested
            // Call the IsImportant method with the sample content
            var result = FileContentChecker.IsImportant(content);

            // Assert: Verify the expected result
            // Check that the method returns false when the important line is not present
            // The test passes if the result is false
            Assert.False(result);
        }
    }
}
