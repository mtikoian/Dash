using Dash.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dash
{
    /// <summary>
    /// Crypto is a secure two way encryption library.
    /// </summary>
    public class Crypt
    {
        public Crypt(IAppConfiguration appConfiguration)
        {
            AppConfiguration = appConfiguration;
        }

        private IAppConfiguration AppConfiguration { get; set; }

        /// <summary>
        /// Decrypt a string.
        /// </summary>
        /// <param name="cipher">String to decrypt.</param>
        /// <returns>Decrypted string.</returns>
        public string Decrypt(string cipher)
        {
            var fullCipher = Convert.FromBase64String(cipher);

            var iv = new byte[16];
            var byteCipher = new byte[16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, byteCipher, 0, iv.Length);
            var key = Encoding.UTF8.GetBytes(AppConfiguration.CryptKey);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(byteCipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Encrypt a string.
        /// </summary>
        /// <param name="text">String to encrypt.</param>
        /// <returns>Encrypted string.</returns>
        public string Encrypt(string text)
        {
            var key = Encoding.UTF8.GetBytes(AppConfiguration.CryptKey);

            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        var iv = aesAlg.IV;

                        var decryptedContent = msEncrypt.ToArray();

                        var result = new byte[iv.Length + decryptedContent.Length];

                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
        }
    }
}