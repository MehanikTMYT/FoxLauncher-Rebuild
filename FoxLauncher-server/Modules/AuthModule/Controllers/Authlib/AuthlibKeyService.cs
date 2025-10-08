using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace FoxLauncher.Modules.AuthModule.Services 
{
    public interface IAuthlibKeyService 
    {
        string GetPublicKeyPem();
        byte[] GetPrivateKey();
        string SignData(byte[] data);
    }

    public class AuthlibKeyService : IAuthlibKeyService 
    {
        private readonly ILogger<AuthlibKeyService> _logger;
        private readonly string _publicKeyPath;
        private readonly string _privateKeyPath;
        private RSA? _rsa;

        public AuthlibKeyService(ILogger<AuthlibKeyService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            
            var dataDir = Path.Combine(environment.ContentRootPath, "data", "authlib");
            Directory.CreateDirectory(dataDir);
            _publicKeyPath = Path.Combine(dataDir, "public.pem");
            _privateKeyPath = Path.Combine(dataDir, "private.pem");

            LoadOrCreateKeys();
        }

        private void LoadOrCreateKeys()
        {
            if (File.Exists(_publicKeyPath) && File.Exists(_privateKeyPath))
            {
                _logger.LogInformation("Loading existing Authlib keys.");
                var privateKeyBytes = File.ReadAllBytes(_privateKeyPath);
                _rsa = RSA.Create();
                _rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            }
            else
            {
                _logger.LogInformation("Generating new Authlib keys.");
                _rsa = RSA.Create(2048); // Рекомендуемый размер

                var privateKeyBytes = _rsa.ExportRSAPrivateKey();
                var publicKeyBytes = _rsa.ExportRSAPublicKey();

                File.WriteAllBytes(_privateKeyPath, privateKeyBytes);
                File.WriteAllBytes(_publicKeyPath, publicKeyBytes);

                _logger.LogInformation("New Authlib keys generated and saved.");
            }
        }

        public string GetPublicKeyPem()
        {
            if (_rsa == null) throw new InvalidOperationException("RSA keys not loaded.");
            var publicKeyBytes = _rsa.ExportRSAPublicKey();
            var base64Key = Convert.ToBase64String(publicKeyBytes);
            return $"-----BEGIN PUBLIC KEY-----\n{FormatPemString(base64Key)}\n-----END PUBLIC KEY-----";
        }

        public byte[] GetPrivateKey()
        {
            if (_rsa == null) throw new InvalidOperationException("RSA keys not loaded.");
            return _rsa.ExportRSAPrivateKey();
        }

        public string SignData(byte[] data)
        {
            if (_rsa == null) throw new InvalidOperationException("RSA keys not loaded.");
            var signature = _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        private string FormatPemString(string base64String)
        {
            const int lineLength = 64;
            var sb = new StringBuilder();
            for (int i = 0; i < base64String.Length; i += lineLength)
            {
                sb.AppendLine(base64String.Substring(i, Math.Min(lineLength, base64String.Length - i)));
            }
            return sb.ToString().TrimEnd('\n', '\r');
        }
    }
}