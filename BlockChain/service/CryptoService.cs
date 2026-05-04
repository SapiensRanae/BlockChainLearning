using System.Security.Cryptography;
using System.Text;

namespace BlockChain.service
{

    public class CryptoService
    {
        public (string publicKey, string privateKey) GenerateKeyPair()
        {
            using (var rsa = RSA.Create())
            {
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                return (publicKey, privateKey);
            }
           
        }

        public static byte[] Sign(string data, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
                return rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        public static bool VerifySignature(string data, byte[] signature, string publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                var dataBytes = Encoding.UTF8.GetBytes(data);
                return rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }
}