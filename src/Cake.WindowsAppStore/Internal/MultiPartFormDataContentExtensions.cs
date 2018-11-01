namespace Cake.WindowsAppStore.Internal
{
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;

    internal static class MultiPartFormDataContentExtensions
    {
        public static void AddIfNotEmpty(this MultipartFormDataContent content, string name, string value)
        {
            if (!name.StartsWith("\""))
            {
                name = $"\"{name}\"";
            }

            if (!string.IsNullOrEmpty(value))
            {
                content.Add(new StringContent(value), name);
            }
        }

        public static void AddIfNotEmpty(this MultipartFormDataContent content, string name, string fileName, Stream stream)
        {
            if (!name.StartsWith("\""))
            {
                name = $"\"{name}\"";
            }

            if (!fileName.StartsWith("\""))
            {
                fileName = $"\"{fileName}\"";
            }

            var streamContent = new StreamContent(stream);

            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = name,
                FileName =fileName
            };

            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            content.Add(streamContent);
        }
    }
}
