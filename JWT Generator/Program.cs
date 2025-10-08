using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        // Генерируем 32 байта (256 бит)
        byte[] key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        // Кодируем в Base64 для удобного хранения
        string base64Key = Convert.ToBase64String(key);
        Console.WriteLine("JwtSettings.SecretKey (Base64):");
        Console.WriteLine(base64Key);
        Console.WriteLine();
        Console.WriteLine("Длина строки: " + base64Key.Length + " символов");

        // Опционально: кодируем в Hex
        string hexKey = Convert.ToHexString(key);
        Console.WriteLine("JwtSettings.SecretKey (Hex):");
        Console.WriteLine(hexKey);
        Console.WriteLine();
        Console.WriteLine("Длина строки (Hex): " + hexKey.Length + " символов");
    }
}