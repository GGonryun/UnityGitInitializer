using System.Diagnostics;
using System.Net;

namespace Miguel.Web
{
    public static class WebLoader
    {
        public static void Load(string website)
        {
            Process.Start(website);
        }

        public static void SaveText(WebClient usingWebClient, string fromWebsite, string toPath)
        {
            FileManipulator.SaveFile(toPath, usingWebClient.DownloadString(fromWebsite));
        }
    }
}
