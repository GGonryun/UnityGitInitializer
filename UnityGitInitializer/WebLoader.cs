using HtmlAgilityPack;
using System.Diagnostics;
using System.Linq;
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

        public static void ScrapeText(WebClient usingWebClient, string fromWebsite, string toPath)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(fromWebsite);

            var code = doc.DocumentNode.SelectNodes(@"//pre").ToList();

            string codeString = "";
            foreach(var line in code)
            {
                codeString += $"{line.InnerHtml}\n".Replace("&quot;", @"""");
            }

            FileManipulator.SaveFile(toPath, codeString);
        }
    }
}
