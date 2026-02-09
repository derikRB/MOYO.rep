using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sego_and__Bux.Helpers
{
    public static class PayfastHelper
    {
        /// <summary>
        /// Generates a Payfast-compliant signature from a list of fields (order matters!).
        /// </summary>
        public static string GenerateSignature(List<KeyValuePair<string, string>> fields, string? passphrase = null)
        {
            var sb = new StringBuilder();

            // 1) Append each <key>=<value>& except signature or blank
            foreach (var kv in fields)
            {
                if (kv.Key == "signature" || string.IsNullOrWhiteSpace(kv.Value))
                    continue;
                sb.Append($"{kv.Key}={kv.Value}&");
            }

            // 2) Trim trailing '&'
            if (sb.Length > 0)
                sb.Length--;

            // 3) (optional) append passphrase
            if (!string.IsNullOrWhiteSpace(passphrase))
                sb.Append($"&passphrase={passphrase}");

            var signatureStr = sb.ToString();

            // 4) debug-log the signature string
            try
            {
                var path = @"C:\temp\payfast-debug.log";
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var sw = new StreamWriter(path, true);
                sw.WriteLine($"{DateTime.Now:u} Signature String: {signatureStr}");
            }
            catch { }

            // 5) MD5 → hex
            string finalSig;
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signatureStr));
                finalSig = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 6) debug-log the final signature
            try
            {
                var path = @"C:\temp\payfast-debug.log";
                using var sw = new StreamWriter(path, true);
                sw.WriteLine($"{DateTime.Now:u} Generated Signature: {finalSig}");
            }
            catch { }

            return finalSig;
        }
    }
}
