using System;
using System.Security.Cryptography;
using System.Text;

namespace AiWebSiteWatchDog.Infrastructure.Auth
{
    internal static class EncryptionHelper
    {
        public static string Encrypt(string plaintext, byte[] key)
        {
            using var aesGcm = new AesGcm(key, 16);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];
            aesGcm.Encrypt(nonce, plainBytes, cipher, tag);
            // Layout: nonce|tag|cipher
            var combined = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, combined, nonce.Length + tag.Length, cipher.Length);
            return Convert.ToBase64String(combined);
        }

        public static string Decrypt(string encoded, byte[] key)
        {
            var combined = Convert.FromBase64String(encoded);
            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[combined.Length - nonce.Length - tag.Length];
            Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(combined, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(combined, nonce.Length + tag.Length, cipher, 0, cipher.Length);
            using var aesGcm = new AesGcm(key, 16);
            var plain = new byte[cipher.Length];
            aesGcm.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
