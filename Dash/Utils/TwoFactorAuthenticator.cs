﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace Dash.Utils
{
    /// <summary>
    /// GoogleAuthenticator implementation from
    /// https://github.com/stephenlawuk/GoogleAuthenticator
    /// https://github.com/brandonpotter/GoogleAuthenticator
    /// </summary>
    public class TwoFactorAuthenticator
    {
        static readonly DateTime _Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static long GetCurrentCounter() => GetCurrentCounter(DateTime.UtcNow, _Epoch, 30);

        static long GetCurrentCounter(DateTime now, DateTime epoch, int timeStep) => (long)(now - epoch).TotalSeconds / timeStep;

        static string RemoveWhitespace(string str) => new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());

        static string UrlEncode(string value)
        {
            var result = new StringBuilder();
            var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

            foreach (var symbol in value)
            {
                if (validChars.IndexOf(symbol) != -1)
                    result.Append(symbol);
                else
                    result.Append('%' + string.Format("{0:X2}", (int)symbol));
            }

            return result.ToString().Replace(" ", "%20");
        }

        internal static string GenerateHashedCode(string secret, long iterationNumber, int digits = 6) => GenerateHashedCode(Base32Encoding.ToBytes(secret), iterationNumber, digits);

        internal static string GenerateHashedCode(byte[] key, long iterationNumber, int digits = 6)
        {
            var counter = BitConverter.GetBytes(iterationNumber);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);

            var hash = new HMACSHA1(key).ComputeHash(counter);
            var offset = hash[hash.Length - 1] & 0xf;

            // Convert the 4 bytes into an integer, ignoring the sign.
            var binary =
                ((hash[offset] & 0x7f) << 24)
                | (hash[offset + 1] << 16)
                | (hash[offset + 2] << 8)
                | (hash[offset + 3]);

            var password = binary % (int)Math.Pow(10, digits);
            return password.ToString(new string('0', digits));
        }

        public TwoFactorAuthenticator() => DefaultClockDriftTolerance = TimeSpan.FromMinutes(1);

        public TimeSpan DefaultClockDriftTolerance { get; set; }

        public static string GeneratePINAtInterval(string accountSecretKey, long counter, int digits = 6) => GenerateHashedCode(accountSecretKey, counter, digits);

        /// <summary>
        /// Generate a setup code for a Google Authenticator user to scan
        /// </summary>
        /// <param name="issuer">Issuer ID (the name of the system, i.e. 'MyApp'), can be omitted but not recommended https://github.com/google/google-authenticator/wiki/Key-Uri-Format </param>
        /// <param name="accountTitleNoSpaces">Account Title (no spaces)</param>
        /// <param name="accountSecretKey">Account Secret Key</param>
        /// <param name="secretIsBase32">Flag saying if accountSecretKey is in Base32 format or original secret</param>
        /// <param name="QRPixelsPerModule">Number of pixels per QR Module (2 pixels give ~ 100x100px QRCode)</param>
        /// <returns>SetupCode object</returns>
        public static SetupCode GenerateSetupCode(string issuer, string accountTitleNoSpaces, string accountSecretKey, bool secretIsBase32, int QRPixelsPerModule) => secretIsBase32 ?
                GenerateSetupCode(issuer, accountTitleNoSpaces, Base32Encoding.ToBytes(accountSecretKey), QRPixelsPerModule) :
                GenerateSetupCode(issuer, accountTitleNoSpaces, Encoding.UTF8.GetBytes(accountSecretKey), QRPixelsPerModule);

        /// <summary>
        /// Generate a setup code for a Google Authenticator user to scan
        /// </summary>
        /// <param name="issuer">Issuer ID (the name of the system, i.e. 'MyApp'), can be omitted but not recommended https://github.com/google/google-authenticator/wiki/Key-Uri-Format </param>
        /// <param name="accountTitleNoSpaces">Account Title (no spaces)</param>
        /// <param name="accountSecretKey">Account Secret Key as byte[]</param>
        /// <param name="QRPixelsPerModule">Number of pixels per QR Module (2 = ~120x120px QRCode)</param>
        /// <returns>SetupCode object</returns>
        public static SetupCode GenerateSetupCode(string issuer, string accountTitleNoSpaces, byte[] accountSecretKey, int QRPixelsPerModule)
        {
            if (accountTitleNoSpaces == null)
                throw new NullReferenceException("Account Title is null");

            accountTitleNoSpaces = RemoveWhitespace(accountTitleNoSpaces);
            var encodedSecretKey = Base32Encoding.ToString(accountSecretKey);
            var provisionUrl = "";
            if (issuer.IsEmpty())
            {
                provisionUrl = string.Format("otpauth://totp/{0}?secret={1}", accountTitleNoSpaces, encodedSecretKey);
            }
            else
            {
                //  https://github.com/google/google-authenticator/wiki/Conflicting-Accounts
                // Added additional prefix to account otpauth://totp/Company:joe_example@gmail.com for backwards compatibility
                provisionUrl = string.Format("otpauth://totp/{2}:{0}?secret={1}&issuer={2}", accountTitleNoSpaces, encodedSecretKey, UrlEncode(issuer));
            }
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(provisionUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new DashQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(QRPixelsPerModule);
            var result = "";
            using (var ms = new MemoryStream())
            {
                qrCodeImage.Save(ms, ImageFormat.Bmp);
                result = Convert.ToBase64String(ms.ToArray());
            }
            return new SetupCode(accountTitleNoSpaces, encodedSecretKey, $"data:image/png;base64,{result}");
        }

        public static string[] GetCurrentPINs(string accountSecretKey, TimeSpan timeTolerance)
        {
            var codes = new List<string>();
            var iterationCounter = GetCurrentCounter();
            var iterationOffset = 0;
            if (timeTolerance.TotalSeconds > 30)
                iterationOffset = Convert.ToInt32(timeTolerance.TotalSeconds / 30.00);

            var iterationStart = iterationCounter - iterationOffset;
            var iterationEnd = iterationCounter + iterationOffset;
            for (var counter = iterationStart; counter <= iterationEnd; counter++)
                codes.Add(GeneratePINAtInterval(accountSecretKey, counter));

            return codes.ToArray();
        }

        public static bool ValidateTwoFactorPIN(string accountSecretKey, string twoFactorCodeFromClient, TimeSpan timeTolerance) => GetCurrentPINs(accountSecretKey, timeTolerance).Any(c => c == twoFactorCodeFromClient);

        public bool ValidateTwoFactorPIN(string accountSecretKey, string twoFactorCodeFromClient) => ValidateTwoFactorPIN(accountSecretKey, twoFactorCodeFromClient, DefaultClockDriftTolerance);
    }
}
