using System;
using System.Security.Cryptography;
using System.Text;

public class WormStoreHash
{
    public static string ComputeHash(string data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert the input data to a byte array and compute the hash
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));

            // Convert the byte array to a hex string
            StringBuilder hashString = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                hashString.Append(bytes[i].ToString("x2"));
            }

            return hashString.ToString();
        }
    }

    public static void Main()
    {
        string data = "Sample data to be hashed";
        string hash = ComputeHash(data);
        Console.WriteLine("Data Hash: " + hash);
    }
}