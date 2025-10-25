using System.Security.Cryptography;
using System.Text;

namespace JWTGenerator
{
    /// <summary>
    /// Генератор криптографических ключей HS256.
    /// Создает два ключа: один для администраторов (приватный), другой для обычных пользователей (публичный).
    /// </summary>
    class Program
    {
        /// <summary>
        /// Генерирует два 256-битных (32 байта) ключа HS256 и кодирует их в Base64,
        /// </summary>
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8; 

            Console.WriteLine("=== Генерация ключей HS256 ===");

            // --- Шаг 1: Генерация КЛЮЧА для ПРИВАТНОГО (Admin) токена ---
            Console.WriteLine("\n--- Ключ для ПРИВАТНОГО (Admin) токена ---");
            byte[] adminKeyBytes = new byte[32]; // 256 бит = 32 байта
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(adminKeyBytes);
            }
            string adminBase64Key = Convert.ToBase64String(adminKeyBytes);
            Console.WriteLine($" Base64 (для appsettings.json - Admin Secret):");
            Console.WriteLine(adminBase64Key);
            Console.WriteLine($"Длина Base64: {adminBase64Key.Length} символов");


            // --- Шаг 2: Генерация КЛЮЧА для ПУБЛИЧНОГО (User) токена ---
            Console.WriteLine("\n---  Ключ для ПУБЛИЧНОГО (User) токена ---");
            byte[] userKeyBytes = new byte[32]; // 256 бит = 32 байта
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(userKeyBytes);
            }
            string userBase64Key = Convert.ToBase64String(userKeyBytes);
            Console.WriteLine($"Base64 (для appsettings.json - User Secret):");
            Console.WriteLine(userBase64Key);
            Console.WriteLine($"Длина Base64: {userBase64Key.Length} символов");

            // --- Шаг 3: Инструкция для сервера ---
            // 
            Console.WriteLine(" Секретные ключи (Base64) для `appsettings.json`:");
            Console.WriteLine($"   - Admin Secret Key: {adminBase64Key}");
            Console.WriteLine($"   - User Secret Key:  {userBase64Key}");
        }
    }
}