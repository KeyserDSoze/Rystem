﻿using System.Text;
using System.Text.Json;

namespace System.Security.Cryptography
{
    public static class CryptoExtensions
    {
        public static string ToHash(this string message)
        {
            using var mySHA512 = (SHA512)CryptoConfig.CreateFromName("SHA512")!;
            var bytes = mySHA512.ComputeHash(Encoding.UTF8.GetBytes(message));
            StringBuilder stringBuilder = new();
            foreach (var @byte in bytes)
                stringBuilder.Append(@byte.ToString("x2"));
            return stringBuilder.ToString();
        }
        public static string ToHash<T>(this T message)
            => message.ToJson().ToHash();
    }
}
