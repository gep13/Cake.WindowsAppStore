namespace Cake.WindowsAppStore
{
    using System;
    using System.Diagnostics;
    using Core;
    using Core.Annotations;
    using Core.Diagnostics;
    using Core.IO;
    using Internal;

    /// <summary>
    /// <para>Contains functionality related to Windows Apps Store.</para>
    /// <para>
    /// It allows you to upload an app package to HockeyApp with just one line of code. In order to use the exposed
    /// commands you have to add the following line at top of your build.cake file.
    /// </para>
    /// <code>
    /// #addin Cake.WindowsAppStore
    /// </code>
    /// </summary>
    [CakeAliasCategory("WindowsAppStore")]
    public static class WindowsAppStoreAliases
    {
        public const string ClientId = "WINDOWSAPPSTORE_CLIENT_ID";
        public const string ClientSecret = "WINDOWSAPPSTORE_CLIENT_SECRET";
        public const string TenantId = "WINDOWSAPPSTORE_TENANT_ID";

        /// <summary>
        /// Creates a new submission based on the last submission with the new appx bundle.
        /// </summary>
        /// <param name="context">The Cake context</param>
        /// <param name="file">The app package.</param>
        /// <param name="settings">The upload settings</param>
        /// <example>
        /// <code>
        /// CreateWindowsStoreAppSubmission( pathToYourPackageFile, new WindowsStoreAppSubmissionSettings
        /// {
        ///     AppId = appId,
        ///     Version = "1.0.160901.1",
        ///     ShortVersion = "1.0-beta2",
        ///     Notes = "Uploaded via continuous integration."
        /// });
        /// </code>
        /// Do not checkin the HockeyApp API Token into your source control.
        /// Either use HockeyAppUploadSettings.ApiToken or set the WINDOWSAPPSTORE_CLIENT_ID and WINDOWSAPPSTORE_CLIENT_SECRET environment variables.
        /// </example>
        [CakeAliasCategory("Deployment")]
        [CakeMethodAlias]
        public static WindowsStoreAppSubmissionResult CreateWindowsStoreAppSubmission(this ICakeContext context, FilePath file, WindowsStoreAppSubmissionSettings settings)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif

            settings = settings ?? new WindowsStoreAppSubmissionSettings();

            var client = new WindowsStoreClient(context.Log);

            settings.ClientId = settings.ClientId ?? context.Environment.GetEnvironmentVariable(ClientId);
            settings.ClientSecret = settings.ClientSecret ?? context.Environment.GetEnvironmentVariable(ClientSecret);
            settings.TenantId = settings.TenantId ?? context.Environment.GetEnvironmentVariable(TenantId);

            try
            {
                var createAppSubmissionTask = client.CreateAppSubmission(file, settings);
                createAppSubmissionTask.Wait();

                return createAppSubmissionTask.Result;
            }
            catch (Exception ex)
            {
                do context.Log.Error(ex.Message); while ((ex = ex.InnerException) != null);

                throw new Exception("Failed to create Windows Store app submission.");
            }
        }
    }
}
