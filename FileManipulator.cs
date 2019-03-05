using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;

namespace Miguel
{
    public static class DirectoryManipulator
    {
        public static string CreateTemporary()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static bool CreateNew(string directory)
        {
            if (!Find(directory))
            {
                Directory.CreateDirectory(directory);
                return true;
            }
            return false;
        }

        public static bool Find(string directory)
        {
            return Directory.Exists(directory);
        }
    }

    public static class FileManipulator
    {
        public static bool SaveFile(string path, string content)
        {
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, content);
                return true;
            }
            return false;
        }

        public static void OpenFile(string executable, string directory)
        {
            Process.Start(executable, directory);
        }

        public static string ReadFile(string filePath)
        {
            return System.IO.File.ReadAllText(filePath);
        }

    }

    public static class FileLocator
    {
        public static bool FindFile(out IEnumerable<PSObject> installations, PowerShell powershell, string fileName, string baseDirectory)
        {
            Console.WriteLine($"Looking for {fileName}...");

            installations = LocateFile(powershell, fileName, baseDirectory);
            if (installations.Count() > 0)
            {
                return true;
            }
            return false;
        }

        private static IEnumerable<PSObject> LocateFile(PowerShell powershell, string fileName, string baseDirectory)
        {
            string script = $"dir -Path \"{baseDirectory}\" -Filter {fileName} ";
            script += " -Recurse | %{$_.FullName}";

            powershell.AddScript(script);
            Collection<PSObject> results = powershell.Invoke();

            return results.Where(value => value.ToString().ToLower().Contains(fileName));
        }
    }

    public class FileDownloader
    {
        ManualResetEvent MyMA = new ManualResetEvent(false);

        public FileDownloader()
        {
        }

        public string DownloadFile(WebClient wc, string executableLocation)
        {
            string tempDir = DirectoryManipulator.CreateTemporary();
            string filePath = Path.Combine(tempDir, Path.GetFileName(executableLocation));
            Uri fileURI = new Uri(executableLocation);

            wc.DownloadFileCompleted += new AsyncCompletedEventHandler(OnDownloadCompleted);
            wc.DownloadFileAsync(fileURI, filePath);

            Console.WriteLine("Downloading the file to: {0}", Path.GetDirectoryName(filePath));

            MyMA.WaitOne();

            Console.WriteLine("Download Completed.");
            return filePath;
        }

        private void OnDownloadCompleted(object sender, EventArgs e)
        {
            MyMA.Set();
        }
    }

    public static class FileInstaller
    {
        public static void Install(string fileLocation)
        {
            Console.WriteLine($"Now Installing File @ {fileLocation}");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileLocation
                }
            };

            process.Start();
            process.WaitForExit();
            Console.WriteLine("Installation has been completed.");

        }
    }

}
