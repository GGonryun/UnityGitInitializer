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

namespace DLTest
{

    class Program
    {
        static ManualResetEvent MyMA = new ManualResetEvent(false);
        static string gitlfs = "https://github.com/git-lfs/git-lfs/releases/download/v2.7.1/git-lfs-windows-v2.7.1.exe";
        static string precommitHook = "https://raw.githubusercontent.com/kayy/git-pre-commit-hook-unity-assets/master/pre-commit";
        static string gitIgnore = "https://raw.githubusercontent.com/github/gitignore/master/Unity.gitignore";
        static string gitAttributes = "https://raw.githubusercontent.com/GGonryun/UnityGitInitializer/master/.gitattributes";

        [STAThread]
        static void Main(string[] args)
        {
            //TORM: TESTING STUFF REMOVE SOON.
            //LocateSSHKeys();
            //InstallGitLFS();
            //================================

            if (args.Length < 1)
            {
                Process.Start(Directory.GetCurrentDirectory());
                //OpenConsole(Directory.GetCurrentDirectory());
            }
            else
            {
                //InstallGitLFS();
                InitializeGitRepository(args[0]);
                
            }
        }


        private static void OpenConsole(string directory)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false
                }
            };

            process.Start();
        }

        private static void InitializeGitRepository(string directory)
        {
            //TODO: We assume that the user has developed their global repository, maybe we should also install git for our user :)?
            Console.WriteLine("Initializing Git Repository");
            Console.WriteLine("Would you like to link and pull from a remote repository?\ny/n");
            bool setupRemote = CheckUserYesOrNoResponse();
            string origin = "";
            if (setupRemote)
            {
                Console.WriteLine("Please pass remote location, this location will be set as 'origin'.");
                origin = Console.ReadLine();
            }

            LocateSSHKeys();

            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddScript(String.Format(@"cd {0}", directory));
                powershell.AddScript(@"git init");

                using (var wc = new WebClient())
                {
                    PlacePrecommitHook(wc, directory);
                    PlaceGitIgnore(wc, directory);
                    PlaceGitAttributes(wc, directory);
                }

                CreateRepository(setupRemote, origin, powershell);
                Collection<PSObject> results = powershell.Invoke();
            }
            
        }

        private static void PlaceGitIgnore(WebClient wc, string directory)
        {
            SaveFile(Path.Combine(directory, ".gitignore"), wc.DownloadString(gitIgnore));
            Console.WriteLine("Git Ignore has been created.");
        }

        private static void PlaceGitAttributes(WebClient wc, string directory)
        {
            SaveFile(Path.Combine(directory, ".git", "info", "attributes"), wc.DownloadString(gitAttributes));
            Console.WriteLine("Git Attributes have been created.");
        }

        private static void PlacePrecommitHook(WebClient wc, string directory)
        {
            SaveFile(Path.Combine(directory, ".git", "hooks", "pre-commit"), wc.DownloadString(precommitHook));
            Console.WriteLine("Git Precommit Hook has been created.");
        }

        private static void CreateRepository(bool setupRemote, string origin, PowerShell powershell)
        {
           
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
        }

        private static void LocateSSHKeys()
        {
            Console.WriteLine("Verifying SSH keys exist.");

            using (PowerShell powershell = PowerShell.Create())
            {
                IEnumerable<PSObject> sshKeyDirectory = LocateSSHKey(powershell, "id_rsa.pub");
                bool sshKeysExist = false;
                if (sshKeyDirectory.Count() > 0)
                {
                    Console.WriteLine("SSH Keys Located!");
                    sshKeysExist = true;
                }
                else
                {
                    sshKeysExist = CreateSSHKeys(sshKeysExist);
                }

                ViewSSHKeys(powershell, sshKeysExist);
            }

        }

        private static bool CreateSSHKeys(bool foundSSHKeys)
        {
            Console.WriteLine("Could not find SSH keys!");
            Console.WriteLine("Would you like to create SSH Keys? y/n");
            bool createSSHKeys = CheckUserYesOrNoResponse();

            if (createSSHKeys)
            {
                SaveKeyPair(RsaKeyPair());
                foundSSHKeys = true;
            }

            return foundSSHKeys;
        }

        private static void ViewSSHKeys(PowerShell powershell, bool sshKeysExist)
        {
            Console.WriteLine("Would you like to view & save your SSH keys? y/n");
            bool saveSSHKeys = CheckUserYesOrNoResponse();
            if (saveSSHKeys && sshKeysExist)
            {
                IEnumerable<PSObject> sshKeyDirectory = LocateSSHKey(powershell, "id_rsa.pub");
                string publicKey = File.ReadAllText(sshKeyDirectory.ElementAt(0).ToString());
                Clipboard.SetText(publicKey);
                Process.Start("https://github.com/settings/ssh/new");
            }
        }

        private static IEnumerable<PSObject> LocateSSHKey(PowerShell powershell, string fileName)
        {
            string script = $@"dir -Path $env:userprofile -Filter {fileName} ";
            script += " -Recurse | %{$_.FullName}";
            powershell.AddScript(script);
            Collection<PSObject> results = powershell.Invoke();
            var sshKeyDirectory = results.Where(value => value.ToString().ToLower().Contains(fileName));
            return sshKeyDirectory;
        }

        private static bool CheckUserYesOrNoResponse()
        {
            return Console.ReadLine() == "y" ? true : false;
        }

        private static void OpenSSHKeys(IEnumerable<PSObject> sshKeyDirectory)
        {
            Process.Start("notepad.exe", sshKeyDirectory.ElementAt(0).ToString());
        }

        private static void SaveKeyPair(SshKeyPair keyPair)
        {
            string directory = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), ".ssh");
            string publicKey = "id_rsa.pub";
            string privateKey = "id_rsa";

            Directory.CreateDirectory(directory);

            string publicKeyPath = Path.Combine(directory, publicKey);
            string privateKeyPath = Path.Combine(directory, privateKey);

            if(SaveFile(publicKeyPath, keyPair.PublicKey) && SaveFile(privateKey, keyPair.PrivateKey))
            {
                Console.WriteLine("SSH Keys have been saved successfully.");
            }
        }

        private static bool SaveFile(string path, string content)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, content);
                return true;
            }
            return false;
        }

        private static SshKeyPair RsaKeyPair()
        {
            Console.WriteLine("Now creating key pair, please wait...");
            Chilkat.SshKey key = new Chilkat.SshKey();
            bool success;
            int numBits;
            int exponent;
            numBits = 4096;
            exponent = 65537;
            success = key.GenerateRsaKey(numBits, exponent);
            var sshKeyPair = new SshKeyPair(key.ToOpenSshPublicKey(), key.ToOpenSshPrivateKey(false));
            Console.WriteLine("Please provide an email address. [migueliscool@example.com]");
            string email = Console.ReadLine();
            sshKeyPair.AppendEmail(email);
            if (!success) RsaKeyPair();
            Console.WriteLine("SSH Keys were created successfully.");
            return sshKeyPair;
        }

        private static void InstallGitLFS()
        {
            if (File.Exists(Path.Combine(@"C:\Program Files\Git LFS", "git-lfs.exe")))
            {
                Console.WriteLine("Git-LFS was located successfully, skipping installation...");
                return;
            }
            else
            {
                Console.WriteLine("Git-LFS was not found. Would you like to install it now? y/n");
                if(!CheckUserYesOrNoResponse())
                {
                    return;
                }
            }

            string gitLFSPath = "";
            bool success = DownloadGitLFS(out gitLFSPath);

            Console.WriteLine("Now installing GitLFS.");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = gitLFSPath
                }
            };
            process.Start();
            process.WaitForExit();
            Console.WriteLine("Installation completed.");
            Directory.Delete(Path.GetDirectoryName(gitLFSPath), true);
        }

        private static bool DownloadGitLFS(out string location)
        {
            string dir = GetTemporaryDirectory();
            string gitlfsFilePath = Path.Combine(dir, Path.GetFileName(gitlfs));
            Uri gitlfsURI = new Uri(gitlfs);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(OnDownloadCompleted);
                wc.DownloadFileAsync(gitlfsURI, gitlfsFilePath);
            }

            Console.WriteLine("Downloading the file to: {0}", Path.GetDirectoryName(gitlfsFilePath));

            MyMA.WaitOne();

            Console.WriteLine("Download Completed.");
            location = gitlfsFilePath;
            return true;
        }

        private static void OnDownloadCompleted(object sender, EventArgs e)
        {
            MyMA.Set();
        }

        private static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
