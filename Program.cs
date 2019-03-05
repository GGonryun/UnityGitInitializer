using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Forms;

using static Interface;

namespace DLTest
{
    public static class Websites
    {

    }

    class Program
    {
        static string gitlfs = "https://github.com/git-lfs/git-lfs/releases/download/v2.7.1/git-lfs-windows-v2.7.1.exe";
        static string precommitHook = "https://raw.githubusercontent.com/kayy/git-pre-commit-hook-unity-assets/master/pre-commit";
        static string gitIgnore = "https://raw.githubusercontent.com/github/gitignore/master/Unity.gitignore";
        static string gitAttributes = "https://raw.githubusercontent.com/GGonryun/UnityGitInitializer/master/.gitattributes";
        static string git32 = "https://github.com/git-for-windows/git/releases/download/v2.21.0.windows.1/Git-2.21.0-32-bit.exe";
        static string git64 = "https://github.com/git-for-windows/git/releases/download/v2.21.0.windows.1/Git-2.21.0-64-bit.exe";
        static string programFiles = "%ProgramW6432%";
        static string textEditor = "notepad.exe";
        static string gitlfsExecutable = "git-lfs.exe";
        static string gitExecutable = "git.exe";

        [STAThread]
        static void Main(string[] args)
        {

            using (WebClient webClient = new WebClient())
            {
                InstallFile(webClient, gitlfsExecutable, Environment.ExpandEnvironmentVariables(programFiles), gitlfs);
                InstallFile(webClient, gitExecutable, Environment.ExpandEnvironmentVariables(programFiles), Environment.Is64BitOperatingSystem ? git64 : git32);
            }
         
        }
        private static void PlaceGitIgnore(WebClient wc, string directory)
        {
            FileManipulator.SaveFile(Path.Combine(directory, ".gitignore"), wc.DownloadString(gitIgnore));
        }
        private static void PlaceGitAttributes(WebClient wc, string directory)
        {
            FileManipulator.SaveFile(Path.Combine(directory, ".git", "info", "attributes"), wc.DownloadString(gitAttributes));
        }
        private static void PlacePrecommitHook(WebClient wc, string directory)
        {
            FileManipulator.SaveFile(Path.Combine(directory, ".git", "hooks", "pre-commit"), wc.DownloadString(precommitHook));
        }


        private static void InitializeGitRepository(string directory)
        {
            Dialogue("I will now initialize your Git Repository...");

            bool setupRemote = AskYesNo("Would you like to link and pull from a remote repository?");
            string origin = GitRepository.RequestOrigin(setupRemote);

            using (PowerShell powershell = PowerShell.Create())
            {
                IEnumerable<PSObject> results = GitRepository.CreateRepository(powershell, directory, setupRemote, origin);
            }

        }

        private static void UploadKeys()
        {
            string location;
            bool success = GetSSHKeys(out location);
            if(success)
            {
                Dialogue("I can open your prefered Git Hosting site & add your public key to your clipboard.");
                bool openKeys = AskYesNo("Should I proceed?");
                if(openKeys)
                {
                    SshKeyPairUtils.ClipSSHKeys(location);
                }
            }
        }

        private static bool GetSSHKeys(out string location)
        {
            Dialogue("Looking for SSH Keys...");
            IEnumerable<PSObject> results;
            bool success = SshKeyPairUtils.FindSSHKeys(out results);
            if(success)
            {
                location = results.ElementAt(0).ToString();
                Dialogue($"I have found your SSH Keys at {location}.");
                return true;
            }
            else
            {
                Dialogue("I was unable to locate your SSH Keys.", "I can create new SSH Keys for you.");
                bool shouldCreateSSHKeys = AskYesNo("Shall I proceed?");
                if(shouldCreateSSHKeys)
                {
                    Dialogue("Alright then, I will need a valid email address to create your SSH Keys.", "For example: example@mail.com");
                    string email = ValidateInput();
                    Dialogue("I will now proceed.", "This may take a few seconds...");
                    var keyPair = SshKeyPairUtils.CreateSSHKeyPair(email);
                    location = SshKeyPairUtils.SaveKeyPair(keyPair);
                    Dialogue("My apologies for the delay...", "Your SSH Keys are now ready.", $"I have saved them at {location}.");
                    return true;
                }
            }
            location = "";
            return false;
        }

        private static void InstallFile(WebClient webclient, string executableName, string baseSearchingDirectory, string fileWebsite)
        {
            bool skip = RequestDownload(executableName, baseSearchingDirectory);
            if (!skip)
            {
                FileDownloader downloader = new FileDownloader();
                string tempLocation = downloader.DownloadFile(webclient, fileWebsite);
                FileInstaller.Install(tempLocation);
                Directory.Delete(Path.GetDirectoryName(tempLocation), true);
            }
        }

        private static bool RequestDownload(string executableName, string baseSearchingDirectory)
        {
            using (var powershell = PowerShell.Create())
            {
                bool success = FileLocator.FindFile(out _, powershell, executableName, baseSearchingDirectory);
                if (success)
                {
                    Console.WriteLine($"I was able to locate [{executableName}] successfully!", "I will skip the installation...");
                    return true;
                }
                else
                {
                    Dialogue($"I was unable to locate [{executableName}].");
                    if (!AskYesNo("Would you like to download and install the file?"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

public static class GitRepository
{
    public static string RequestOrigin(bool setupRemote)
    {
        string origin = "";
        if (setupRemote)
        {
            Dialogue("Please enter the remote location, this location will be set as 'origin'.", "For example [git@github.com:GGonryun/UnityGitInitializer.git]");
            origin = Console.ReadLine();
        }

        return origin;
    }

    public static IEnumerable<PSObject> CreateRepository(PowerShell powershell, string directory, bool setupRemote = false, string origin = "")
    {
        powershell.AddScript(String.Format(@"cd {0}", directory));
        powershell.AddScript(@"git init");

        if (setupRemote)
        {
            powershell.AddScript($@"git remote add origin {origin}");
            powershell.AddScript(@"git pull origin master");
        }
        powershell.AddScript(@"git add *");
        powershell.AddScript(@"git commit -m 'Initializing Project'");
        if (setupRemote)
        {
            powershell.AddScript(@"git push -u origin master");
        }
        else
        {
            powershell.AddScript(@"git push");
        }
        return powershell.Invoke();
    }
}

public static class Interface
{
    public static string yes = "y";

    public static void Dialogue(params string[] dialogue)
    {
        foreach(string sentence in dialogue)
        {
            Console.WriteLine(sentence);
        }
    }

    public static bool AskYesNo(string question)
    {
        question += " (y/n)";
        Dialogue(question);

        string response = ReadInput();

        while (!response.Equals("y") && !response.Equals("n"))
        {
            Dialogue("Please enter 'y' or 'n' only.");
            response = ReadInput();
        }
        return response.Equals("y");
    }

    public static string ReadInput()
    {
        return Console.ReadLine().ToLower();
    }

    public static string ValidateInput()
    {
        string input = ReadInput();
        bool valid = false;
        while(!valid)
        {
            Dialogue($"The email you passed me is: {input}");
            valid = AskYesNo("Is this correct?");
        }
        return input;

    }

}

public static class WebLoader
{
    public static void Load(string website)
    {
        Process.Start(website);
    }
    public static void SaveText(string website, string location)
    {

    }
}

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
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
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
        return File.ReadAllText(filePath);
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
