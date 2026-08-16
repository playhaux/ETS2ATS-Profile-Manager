using ntfy.Requests;
using ntfy;
using System.Reflection;
using System.IO;

namespace EAPM
{
    internal class NtfyUsage
    {
        public static async void SendUsage(string titletext, string messagetext)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string? appPath = Environment.ProcessPath;
            DateTime buildDate = !string.IsNullOrEmpty(appPath) && File.Exists(appPath)
                ? new FileInfo(appPath).LastWriteTime
                : DateTime.Now;

            // Create a new client
            var client = new Client("https://ntfy.sh");

            // Publish a message to the topic
            var message = new SendingMessage
            {
                Title = titletext,
                Message = string.Format("{0}\nVersion: {1} ({2})", messagetext, version.ToString(), buildDate.ToString())
            };
            await client.Publish("3cQsIJnRdqimEACu", message);
        }
    }
}
