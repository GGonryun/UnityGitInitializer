using Miguel;
using Miguel.Environment;
using Miguel.SSH;
using Miguel.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using static Interface;

namespace UnityGitPreparer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Dialogue(
                    "Insufficient Arguments, I will show you a few examples of how to use me: ",
                    @"You must include a file location: 'UnityGitPreparer.exe C:\Some\File\Location\'.",
                    @"You may use the '-nokey' to skip making keys.",
                    @"You may use the '-noinstall' to skip installing.",
                    @"Secret Mode can be enabled via '-secret_mode' although you probably can't do much as a user."
                    );
                return;
            }

            EnvironmentVariables.Instance["userpath"] = args[0];

            foreach (string arg in args)
            {
                if (arg == "-nokey")
                {
                    EnvironmentVariables.Instance["initkey"] = "false";
                }
                else if (arg == "-noinstall")
                {
                    EnvironmentVariables.Instance["install"] = "false";
                }
                else if (arg == "-nogit")
                {
                    EnvironmentVariables.Instance["initgit"] = "false";

                }
                else if (arg == "-secret_mode")
                {
                    Dialogue("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "Loading Secret Mode:");
                    Random r = new Random();
                    for (int i = 0; i <= 1000; i++)
                    {
                        Dialogue($"\r{i / 50f:F2}%");
                        System.Threading.Thread.Sleep(r.Next(0, 15));
                    }
                    Dialogue("", "The secret is ready...");
                    System.Threading.Thread.Sleep(5000);
                    while (true)
                    {
                        Dialogue($"-f{WordLoop(r, 10, "a")}rt noise-");
                    }
                    return;
                }
            }

            if (EnvironmentVariables.Instance["install"] == "true")
            {
                using (WebClient webClient = new WebClient())
                {
                    InstallFile(webClient, EnvironmentVariables.Instance["gitlfsExecutable"], System.Environment.ExpandEnvironmentVariables(EnvironmentVariables.Instance["programFiles"]), EnvironmentVariables.Instance["gitlfs"]);
                    InstallFile(webClient, EnvironmentVariables.Instance["gitExecutable"], System.Environment.ExpandEnvironmentVariables(EnvironmentVariables.Instance["programFiles"]), System.Environment.Is64BitOperatingSystem ? EnvironmentVariables.Instance["git64"] : EnvironmentVariables.Instance["git32"]);
                }
            }
            if (EnvironmentVariables.Instance["initkey"] == "true")
            {
                UploadKeys();
            }
            if (EnvironmentVariables.Instance["initgit"] == "true")
            {
                InitializeGitRepository();
            }
        }

        private static string WordLoop(Random r, int v, string s)
        {
            string letter = "";
            int max = r.Next(0, v);
            for(int i = 0; i < max; i++)
            {
                letter += s;
            }
            return s;
        }

        private static void InitializeGitRepository()
        {
            EnvironmentVariables ev = EnvironmentVariables.Instance;

            Dialogue("I will now initialize your Git Repository...");

            bool setupRemote = AskYesNo("Would you like to link and pull from a remote repository?");
            string origin = GitRepository.RequestOrigin(setupRemote);

            bool uploadProject = AskYesNo("Would you like to upload your project immediately?");
            using (PowerShell powershell = PowerShell.Create())
            {
                IEnumerable<PSObject> results = GitRepository.CreateRepository(powershell, ev["userpath"], setupRemote, origin, uploadProject);
                foreach(var result in results)
                {
                    Console.WriteLine(result);
                }
            }
            Dialogue("Initialization Complete! Enjoy your project.");

        }

        private static void UploadKeys()
        {
            string location;
            bool success = GetSSHKeys(out location);
            if (success)
            {
                Dialogue("I can open your prefered Git Hosting site & add your public key to your clipboard.");
                bool openKeys = AskYesNo("Should I proceed?");
                if (openKeys)
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
            if (success)
            {
                location = results.ElementAt(0).ToString();
                Dialogue($"I have found your SSH Keys at {location}.");
                return true;
            }
            else
            {
                Dialogue("I was unable to locate your SSH Keys.", "I can create new SSH Keys for you.");
                bool shouldCreateSSHKeys = AskYesNo("Shall I proceed?");
                if (shouldCreateSSHKeys)
                {
                    Dialogue("Alright then, I will need a valid email address to create your SSH Keys.", "For example: example@mail.com");
                    string email = ReadInput();
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
            
            origin = ReadInput();
        }

        return origin;
    }

    public static IEnumerable<PSObject> CreateRepository(PowerShell powershell, string directory, bool setupRemote, string origin, bool upload)
    {
        powershell.AddScript(String.Format($"Set-Location \"{directory}\""));
        powershell.AddScript(@"git init");
        Collection<PSObject> results = powershell.Invoke();

        foreach(var result in results)
        {
            Console.WriteLine(result.ToString());
        }

        if (setupRemote)
        {
            powershell.AddScript($@"git remote add origin {origin}");
            powershell.AddScript(@"git pull origin master");
            Dialogue("I am now pulling the project from origin, please wait...");
            powershell.Invoke();
            System.Threading.Thread.Sleep(3000);
            
        }

        if (AskYesNo("Would you like to setup initial files?"))
        {
            SetUpGitFiles();
        }

        if (upload)
        {
            UploadProject(powershell, setupRemote);
        }
        return powershell.Invoke();
    }

    private static void SetUpGitFiles()
    {
        EnvironmentVariables ev = EnvironmentVariables.Instance;
        string precommitPath = Path.Combine(ev["userpath"], ev["precommitPath"]);
        string gitAttributesPath = Path.Combine(ev["userpath"], ev["gitAttributesPath"]);
        string gitIgnorePath = Path.Combine(ev["userpath"], ev["gitIgnorePath"]);
        string emptyDirectoryRemoverPath = Path.Combine(ev["userpath"], ev["emptyDirectoryRemoverPath"]);
        using (var wc = new WebClient())
        {
            WebLoader.SaveText(wc, ev["precommitHook"], precommitPath);
            Dialogue($"Precommit Hook has been placed @ {precommitPath}");
            WebLoader.SaveText(wc, ev["gitAttributes"], gitAttributesPath);
            Dialogue($"Git Attributes have been placed @ {gitAttributesPath}");
            WebLoader.SaveText(wc, ev["gitIgnore"], gitIgnorePath);
            Dialogue($"Git Ignore has been been placed @ {gitIgnorePath}");
            if (!DirectoryManipulator.Find(Path.Combine(ev["userpath"], ev["assetsFolder"])))
            {
                Dialogue("I was unable to locate an assets folder, I have created one for now.");
                DirectoryManipulator.CreateNew(Path.GetDirectoryName(emptyDirectoryRemoverPath));
            }
            WebLoader.ScrapeText(wc, ev["emptyDirectoryRemover"], emptyDirectoryRemoverPath);
            Dialogue($"EmptyDirectoryRemover has been placed @ {emptyDirectoryRemoverPath}");
        }
    }

    private static void UploadProject(PowerShell powershell, bool setupRemote)
    {
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
    }
}

public static class Interface
{
    public static string yes = "y";

    public static void Dialogue(params string[] dialogue)
    {
        foreach (string sentence in dialogue)
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

}


