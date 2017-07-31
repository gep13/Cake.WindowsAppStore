namespace Cake.WindowsAppStore.Internal
{
    using System;
    using Core.Diagnostics;
    using Core.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using Newtonsoft.Json.Linq;

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

            var appId = settings.ApplicationId;
            var clientId = settings.ClientId;
            var clientSecret = settings.ClientSecret;
            var serviceEndpoint = settings.ServiceUrl;
            var tokenEndpoint = settings.TokenEndpoint;

            _log.Debug("Getting access token for the Windows Store");

            var accessToken = await IngestionClient.GetClientCredentialAccessToken(tokenEndpoint, clientId, clientSecret);

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

            var lastPublishedDate = app.lastPublishedApplicationSubmission;
            if (lastPublishedDate == null)
            {
                throw new InvalidOperationException("You need at least one published submission to create new submissions through API");
            }
            
            _log.Information($"App was last published on {lastPublishedDate}");

            var pendingSubmission = app.pendingApplicationSubmission;
            if (pendingSubmission != null)
            {
                _log.Warning($"Detected pending application submission, deleting that one first (no worries, this will only delete submission created via an API, never manual submissions)");

                var submissionId = app.pendingApplicationSubmission.id.Value as string;

                // Try deleting it. If it was NOT created via the API, then you need to manually
                // delete it from the dashboard. This is done as a safety measure to make sure that a
                // user and an automated system don't make conflicting edits.
                client.Invoke<dynamic>(
                    HttpMethod.Delete,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.GetSubmissionUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        submissionId),
                    requestContent: null).Wait();
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

            packagesToProcess.Add(new
            {
                fileStatus = "PendingUpload",
                fileName = "package.appxupload",
            });

            clonedSubmission.applicationPackages = JToken.FromObject(packagesToProcess.ToArray());

            _log.Information("Uploading new package to the cloned submission, this can take a while...");

            var fileUploadUrl = clonedSubmission.fileUploadUrl.Value as string;

            await IngestionClient.UploadFileToBlob(file.FullPath, fileUploadUrl);

            _log.Information("Updating the cloned submission");

            await client.Invoke<dynamic>(
                HttpMethod.Put,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.UpdateUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId,
                    clonedSubmissionId),
                requestContent: clonedSubmission);

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

            _log.Information("Waiting for the submission commit processing to complete.This may take a couple of minutes");

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
            }
            while ("CommitStarted".Equals(submissionStatus));

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
    }
}
