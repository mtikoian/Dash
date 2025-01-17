﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dash.Configuration;

namespace Dash
{
    /// <summary>
    /// Crypto is a secure two way encryption library.
    /// </summary>
    public class Crypt
    {
        IAppConfiguration _AppConfiguration { get; set; }

        public Crypt(IAppConfiguration appConfiguration) => _AppConfiguration = appConfiguration;

        public string Decrypt(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - iv.Length];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);
            var key = Encoding.UTF8.GetBytes(_AppConfiguration.CryptKey);

            using (var aesAlg = Aes.Create())
            using (var decryptor = aesAlg.CreateDecryptor(key, iv))
            {
                string result;
                using (var msDecrypt = new MemoryStream(cipher))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                    result = srDecrypt.ReadToEndAsync().Result;
                return result;
            }
        }

        public string Encrypt(string text)
        {
            var key = Encoding.UTF8.GetBytes(_AppConfiguration.CryptKey);
            using (var aesAlg = Aes.Create())
            using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                    swEncrypt.WriteAsync(text);
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
