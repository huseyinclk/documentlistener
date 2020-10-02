using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    static class ExtensionMethods
    {
        public static string TrimAll(this string str)
        {
            return string.IsNullOrEmpty(str) ? "" : str.TrimStart().Trim().TrimEnd();
        }
    }
}
