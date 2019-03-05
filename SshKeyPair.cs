using Chilkat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Windows.Forms;

namespace DLTest
{
    public class SshInfo
    {
        public static string userProfile = Environment.GetEnvironmentVariable("userprofile");
        public static string defaultSSHDirectory = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), ".ssh");
        public static string privateKeyDefaultFilePath = Path.Combine(defaultSSHDirectory, "id_rsa");
        public static string publicKeyDefaultFilePath = Path.Combine(defaultSSHDirectory, "id_rsa.pub");
        public static string sshKeyWebsite = "https://github.com/settings/ssh/new";
    }

    public static class SshKeyPairUtils
    {
        public static void ClipSSHKeys(string location)
        {
            Clipboard.SetText(FileManipulator.ReadFile(location));
            WebLoader.Load(SshInfo.sshKeyWebsite);
        }

        public static bool FindSSHKeys(out IEnumerable<PSObject> results)
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                return FileLocator.FindFile(out results, powershell, Path.GetFileName(SshInfo.publicKeyDefaultFilePath), SshInfo.userProfile);
            }
        }

        public static SshKeyPair CreateSSHKeyPair(string email)
        {
            var keyPair = SshKeyPair.Generate(4096);
            keyPair.AppendEmail(email);
            return keyPair;
        }

        public static string SaveKeyPair(SshKeyPair keyPair)
        {
            DirectoryManipulator.CreateNew(SshInfo.defaultSSHDirectory);
            FileManipulator.SaveFile(SshInfo.publicKeyDefaultFilePath, keyPair.PublicKey);
            FileManipulator.SaveFile(SshInfo.privateKeyDefaultFilePath, keyPair.PrivateKey);
            return SshInfo.publicKeyDefaultFilePath;
        }
    }



    public class SshKeyPair
    {
        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }

        private SshKeyPair(string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public void AppendEmail(string email)
        {
            PublicKey += $" {email}";
        }

        public static SshKeyPair Generate(int bits)
        {
            SshKey key = new SshKey();
            int numBits = bits;
            int exponent = 65537;
            bool success = key.GenerateRsaKey(numBits, exponent);
            var sshKeyPair = new SshKeyPair(key.ToOpenSshPublicKey(), key.ToOpenSshPrivateKey(false));
            if (!success) Generate(bits);
            return sshKeyPair;
        }

    }
}