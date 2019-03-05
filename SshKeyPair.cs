using Chilkat;
namespace DLTest
{
    public class SshKeyPair
    {
        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }

        public SshKeyPair(string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public void AppendEmail(string email)
        {
            PublicKey += $" {email}";
        }
    }
}