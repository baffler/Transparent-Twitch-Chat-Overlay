using System;
using System.Security.Cryptography;
using System.Text;

namespace TransparentTwitchChatWPF.Utils;

public static class Hasher
{
    public static string CreateSha256Hash(string input)
    {
        // Use SHA256 to create the hash
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert the input string to a byte array and compute the hash
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // Convert the byte array to a hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public static string Create64BitHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // Take the first 8 bytes of the SHA256 hash and convert it to a 64-bit unsigned integer
            ulong hashValue = BitConverter.ToUInt64(hashBytes, 0);

            // Convert the number to a hexadecimal string. This will be up to 16 characters long.
            return hashValue.ToString("x");
        }
    }
}
