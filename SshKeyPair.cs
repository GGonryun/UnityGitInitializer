using Chilkat;
using Miguel.Environment;
using Miguel.Web;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Windows.Forms;

namespace Miguel.SSH
{

    public static class SshKeyPairUtils
    {
        public static void ClipSSHKeys(string location)
        {
            Clipboard.SetText(FileManipulator.ReadFile(location));
            WebLoader.Load(EnvironmentVariables.Instance["sshKeyWebsite"]);
        }

        public static bool FindSSHKeys(out IEnumerable<PSObject> results)
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                return FileLocator.FindFile(out results, powershell, Path.GetFileName(EnvironmentVariables.Instance["publicKeyDefaultFilePath"]), EnvironmentVariables.Instance["userProfile"]);
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
            DirectoryManipulator.CreateNew(EnvironmentVariables.Instance["defaultSSHDirectory"]);
            FileManipulator.SaveFile(EnvironmentVariables.Instance["publicKeyDefaultFilePath"], keyPair.PublicKey);
            FileManipulator.SaveFile(EnvironmentVariables.Instance["privateKeyDefaultFilePath"], keyPair.PrivateKey);
            return EnvironmentVariables.Instance["publicKeyDefaultFilePath"];
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