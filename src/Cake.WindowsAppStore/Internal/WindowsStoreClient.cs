namespace Cake.WindowsAppStore.Internal
{
    using System;
    using Core.Diagnostics;
    using Core.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Compression;
    using System.Net.Http;
    using Newtonsoft.Json.Linq;

    // Note: For details on the implementation, see https://docs.microsoft.com/en-us/windows/uwp/monetize/csharp-code-examples-for-the-windows-store-submission-api

    internal class WindowsStoreClient
    {
        private readonly ICakeLog _log;

        public WindowsStoreClient(ICakeLog log)
        {
            _log = log;
        }

        public async Task<WindowsStoreAppSubmissionResult> CreateAppSubmission(FilePath file, WindowsStoreAppSubmissionSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ApplicationId))
            {
                throw new ArgumentNullException("settings.ApplicationId", $"You have to specify the ApplicationId.");
            }

            if (string.IsNullOrEmpty(settings.ClientId))
            {
                throw new ArgumentNullException("settings.ClientId", $"You have to either specify a ClientId or define the {WindowsAppStoreAliases.ClientId} environment variable.");
            }

            if (string.IsNullOrEmpty(settings.ClientSecret))
            {
                throw new ArgumentNullException("settings.ClientSecret", $"You have to either specify a ClientSecret or define the {WindowsAppStoreAliases.ClientSecret} environment variable.");
            }

            if (string.IsNullOrEmpty(settings.TenantId))
            {
                throw new ArgumentNullException("settings.TenantId", $"You have to either specify a TenantId or define the {WindowsAppStoreAliases.TenantId} environment variable.");
            }

            var handledErrors = new HashSet<string>();
            var handledWarnings = new HashSet<string>();

            var appId = settings.ApplicationId;
            var clientId = settings.ClientId;
            var clientSecret = settings.ClientSecret;
            var serviceEndpoint = settings.ServiceUrl;
            var tokenEndpoint = settings.TokenEndpoint;

            _log.Information("Starting app submission");

            _log.Debug("Getting access token for the Windows Store");

            var accessToken = await IngestionClient.GetClientCredentialAccessToken(tokenEndpoint, clientId, clientSecret, settings.Scope);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException($"No access token could be retrieved via OAuth, double check your tokens or create a new client secret");
            }

            _log.Debug("Getting application from the Windows Store");

            var client = new IngestionClient(accessToken, serviceEndpoint);

            var app = await client.Invoke<dynamic>(HttpMethod.Get,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.GetApplicationUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId),
                requestContent: null);

            var lastPublishedApplicationSubmission = app.lastPublishedApplicationSubmission;
            if (lastPublishedApplicationSubmission == null)
            {
                throw new InvalidOperationException("You need at least one published submission to create new submissions through API");
            }

            var pendingSubmission = app.pendingApplicationSubmission;
            if (pendingSubmission != null)
            {
                _log.Warning($"Detected pending application submission, deleting that one first (no worries, this will only delete submissions created via an API, never manual submissions)");

                var submissionId = app.pendingApplicationSubmission.id.Value as string;

                // Try deleting it. If it was NOT created via the API, then you need to manually
                // delete it from the dashboard. This is done as a safety measure to make sure that a
                // user and an automated system don't make conflicting edits.
                await client.Invoke<dynamic>(
                    HttpMethod.Delete,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.GetSubmissionUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        submissionId),
                    requestContent: null);
            }

            _log.Information("Cloning last submission");

            var clonedSubmission = await client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.CreateSubmissionUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId),
                requestContent: null);

            LogStatusDetails(clonedSubmission.statusDetails, handledWarnings, handledErrors);

            var clonedSubmissionId = clonedSubmission.id.Value as string;

            _log.Information("Cloned submission, updating specified fields");

            clonedSubmission.notesForCertification = settings.NotesForCertification;
            //clonedSubmission.releaseNotes = settings.ReleaseNotes;

            _log.Information("Deleting existing packages in cloned submission");

            var packagesToProcess = new List<dynamic>();

            foreach (var applicationPackage in clonedSubmission.applicationPackages)
            {
                applicationPackage.fileStatus = "PendingDelete";

                packagesToProcess.Add(applicationPackage);
            }

            var fileName = file.GetFilename().FullPath;

            packagesToProcess.Add(new
            {
                fileName = fileName,
                fileStatus = "PendingUpload",
                minimumDirectXVersion = "None",
                minimumSystemRam = "None"
            });

            clonedSubmission.applicationPackages = JToken.FromObject(packagesToProcess.ToArray());

            using (var temporaryFilesContext = new TemporaryFilesContext())
            {
                _log.Information($"Packing the file(s) to a zip package in preparation for upload");

                var storeUploadFolder = temporaryFilesContext.GetDirectory("StoreUpload");

                // Copy all files into the temporary folder
                var targetAppxUploadFileName = System.IO.Path.Combine(storeUploadFolder, fileName);
                System.IO.File.Copy(file.FullPath, targetAppxUploadFileName, true);

                // Finally zip them
                var storeUploadFile = temporaryFilesContext.GetFile("StoreUpload.zip", true);
                ZipFile.CreateFromDirectory(storeUploadFolder, storeUploadFile);

                _log.Information($"Uploading new package '{file.FullPath}' to the cloned submission, this can take a while (depending on size & internet speed)...");

                var fileUploadUrl = clonedSubmission.fileUploadUrl.Value as string;

                await IngestionClient.UploadFileToBlob(storeUploadFile, fileUploadUrl);
            }

            _log.Information("Updating the cloned submission");

            var updatedSubmission = await client.Invoke<dynamic>(
                HttpMethod.Put,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.UpdateUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId,
                    clonedSubmissionId),
                requestContent: clonedSubmission);

            LogStatusDetails(updatedSubmission.statusDetails, handledWarnings, handledErrors);
            
            _log.Information("Committing the submission");

            await client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.CommitSubmissionUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId,
                    clonedSubmissionId),
                requestContent: null);

            _log.Information("Waiting for the submission commit processing to complete. This may take a couple of minutes...");

            string submissionStatus = null;

            do
            {
                await Task.Delay(5000);

                var statusResource = await client.Invoke<dynamic>(
                    HttpMethod.Get,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.ApplicationSubmissionStatusUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        clonedSubmissionId),
                    requestContent: null);

                submissionStatus = statusResource.status.Value as string;

                _log.Debug("Current status: " + submissionStatus);

                LogStatusDetails(statusResource.statusDetails, handledWarnings, handledErrors);
            }
            while ("CommitStarted".Equals(submissionStatus));

            _log.Information($"Final submission status: '{submissionStatus}'");

            var result = new WindowsStoreAppSubmissionResult
            {
                Id = clonedSubmissionId,
                FriendlyName = clonedSubmission.friendlyName,
                Status = submissionStatus
            };

            if ("CommitFailed".Equals(submissionStatus))
            {
                _log.Error("Submission has failed. Please check the errors in the Windows Dev Center");
            }
            else
            {
                _log.Information("Submission has succeeded");

                // We could retrieve the latest submission info again if we want
                //var submission = await client.Invoke<dynamic>(
                //    HttpMethod.Get,
                //    relativeUrl: string.Format(
                //        CultureInfo.InvariantCulture,
                //        IngestionClient.GetSubmissionUrlTemplate,
                //        IngestionClient.Version,
                //        IngestionClient.Tenant,
                //        appId,
                //        clonedSubmissionId),
                //    requestContent: null);
                //Console.WriteLine("Packages: " + submission.applicationPackages);
                //Console.WriteLine("en-US description: " + submission.listings["en-us"].baseListing.description);
                //Console.WriteLine("Images: " + submission.listings["en-us"].baseListing.images);
            }

            return result;
        }

        private void LogStatusDetails(dynamic statusDetails, HashSet<string> handledWarnings, HashSet<string> handledErrors)
        {
            if (statusDetails == null)
            {
                return;
            }

            var warnings = statusDetails.warnings;
            if (warnings != null)
            {
                foreach (var warning in warnings)
                {
                    var warningCode = warning.code.Value;

                    if (!handledWarnings.Contains(warningCode))
                    {
                        _log.Warning($"[{warningCode}] {warning.details.Value}");

                        handledWarnings.Add(warningCode);
                    }
                }
            }

            var errors = statusDetails.errors;
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    var errorCode = error.code.Value;

                    if (!handledErrors.Contains(errorCode))
                    {
                        _log.Error($"[{errorCode}] {error.details.Value}");

                        handledErrors.Add(errorCode);
                    }
                }
            }
        }
    }
}
