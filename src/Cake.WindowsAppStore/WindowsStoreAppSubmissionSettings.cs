namespace Cake.WindowsAppStore
{
    using Cake.WindowsAppStore.Internal;

    /// <summary>
    /// Contains settings used by <see cref="WindowsStoreClient" />
    /// <para />
    /// For a detailed information look at the official <see href="https://docs.microsoft.com/en-us/windows/uwp/monetize/csharp-code-examples-for-the-windows-store-submission-api#create-app-submission">API Documentation</see>
    /// </summary>
    public class WindowsStoreAppSubmissionSettings : WindowsStoreSettingsBase
    {
        /// <summary>
        /// Gets or sets the version tag. 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the notes for certification.
        /// </summary>
        public string NotesForCertification { get; set; }

        /// <summary>
        /// Gets or sets the mandatory option for this version. Optional, default value is <c>null</c>.
        /// </summary>
        public bool? IsMandatory { get; set; }

        /// <summary>
        /// Gets or sets the publish mode. Optional, defaults to <see cref="Cake.WindowsAppStore.PublishMode.Manual"/>.
        /// </summary>
        public PublishMode PublishMode { get; set; }

        /// <summary>
        /// Gets or sets if this is a private version. Optional.
        /// </summary>
        public bool? IsPrivate { get; set; }
    }
}