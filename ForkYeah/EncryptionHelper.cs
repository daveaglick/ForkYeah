using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ForkYeah
{
    public static class EncryptionHelper
    {
        // The following two methods allow arbitrary encryption algorithms and are from
        // http://www.superstarcoders.com/blogs/posts/symmetric-encryption-in-c-sharp.aspx

        public static string Encrypt<T>(string value, string password, string salt)
            where T : SymmetricAlgorithm, new()
        {
            using (DeriveBytes rgb = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt)))
            {
                using (SymmetricAlgorithm algorithm = new T())
                {
                    byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
                    byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

                    using (ICryptoTransform transform = algorithm.CreateEncryptor(rgbKey, rgbIV))
                    {
                        using (MemoryStream buffer = new MemoryStream())
                        {
                            using (CryptoStream stream = new CryptoStream(buffer, transform, CryptoStreamMode.Write))
                            {
                                using (StreamWriter writer = new StreamWriter(stream, Encoding.Unicode))
                                {
                                    writer.Write(value);
                                }
                            }

                            return Convert.ToBase64String(buffer.ToArray());
                        }
                    }
                }
            }
        }

        public static string Decrypt<T>(string text, string password, string salt)
           where T : SymmetricAlgorithm, new()
        {
            using (DeriveBytes rgb = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt)))
            {
                using (SymmetricAlgorithm algorithm = new T())
                {
                    byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
                    byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

                    using (ICryptoTransform transform = algorithm.CreateDecryptor(rgbKey, rgbIV))
                    {
                        using (MemoryStream buffer = new MemoryStream(Convert.FromBase64String(text)))
                        {
                            using (CryptoStream stream = new CryptoStream(buffer, transform, CryptoStreamMode.Read))
                            {
                                using (StreamReader reader = new StreamReader(stream, Encoding.Unicode))
                                {
                                    return reader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

        // The following methods use the above generic encryption/decryption methods, but default to AesCryptoServiceProvider

        public static string Encrypt(string value, string password, string salt)
        {
            return Encrypt<AesCryptoServiceProvider>(value, password, salt);
        }

        public static string Decrypt(string value, string password, string salt)
        {
            return Decrypt<AesCryptoServiceProvider>(value, password, salt);
        }

        // This method uses a similar generic concept as the prior ones to compute a hash with any keyed hash algorithm
        public static string Hash<T>(string value, string password)
            where T : KeyedHashAlgorithm, new()
        {
            ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes(password);
            byte[] messageBytes = encoding.GetBytes(value);

            using (KeyedHashAlgorithm algorithm = new T())
            {
                algorithm.Key = keyBytes;
                return Convert.ToBase64String(algorithm.ComputeHash(messageBytes));
            }
        }

        // The following method provides HMAC hashing using SHA256        
        public static string Hash(string value, string password)
        {
            return Hash<HMACSHA256>(value, password);
        }
    }
}