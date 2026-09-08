using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EAPM
{
    static class StringExtensions
    {
        public static string DirectoryToScsUsername(this string directorystring)
        {
            DirectoryInfo di = new(directorystring);
            string hex = di.Name;
            try
            {
                if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0 || !hex.IsHex())
                {
                    return hex;
                }
                byte[] bytes = Convert.FromHexString(hex);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return hex;
            }
        }

        public static string ScsUsernameToDirectory(this string username)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(username);
            return Convert.ToHexString(bytes);
        }

        public static bool IsHex(this IEnumerable<char> chars)
        {
            foreach (char c in chars)
            {
                bool isHex = (c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f') ||
                             (c >= 'A' && c <= 'F');

                if (!isHex)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
