using System.Security.Cryptography;
using System.Text;
using dotnet_services_viewer.Application.Interfaces;

namespace dotnet_services_viewer.Infrastructure.Security;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService()
    {
        // En una app real, estas claves vendrían de un Key Vault o Environment Variables.
        // Aquí usamos una clave fija para que el sistema funcione consistentemente.
        var keyString = "A6Z8X9K2P5Q1W3E4R7T0Y1U2I3O4P5A6"; // 32 chars = 256 bits
        var ivString = "Z8X9K2P5Q1W3E4R7"; // 16 chars = 128 bits
        
        _key = Encoding.UTF8.GetBytes(keyString);
        _iv = Encoding.UTF8.GetBytes(ivString);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
