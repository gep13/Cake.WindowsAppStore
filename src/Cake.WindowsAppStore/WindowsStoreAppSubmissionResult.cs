namespace Cake.WindowsAppStore
{
    /// <summary>
    /// Result returned after succesfull upload.
    /// <para />
    /// This class can slowly be expanded if needed by using the documentation at https://docs.microsoft.com/en-us/windows/uwp/monetize/create-an-app-submission.
    /// </summary>
    public class WindowsStoreAppSubmissionResult
    {
        /// <summary>
        /// Gets the id of this submission.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Friendly name of this submission.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Status of the submission.
        /// </summary>
        public string Status { get; set; }
    }
}