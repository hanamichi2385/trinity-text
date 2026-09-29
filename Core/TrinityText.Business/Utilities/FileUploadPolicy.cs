using System;
using System.Collections.Generic;
using System.IO;

namespace TrinityText.Business
{
    /// <summary>
    /// Files added to the file manager are copied to the CDN by the publications: server-side scripts, executables and
    /// server configuration files must never end up there (code execution / configuration overwrite on the web server).
    /// The list can be changed at startup by the host application.
    /// </summary>
    public static class FileUploadPolicy
    {
        public static ISet<string> BlockedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ASP.NET / IIS
            ".aspx", ".ashx", ".asmx", ".ascx", ".asax", ".asp", ".cshtml", ".vbhtml", ".config", ".master", ".svc", ".axd",
            // other server-side languages
            ".php", ".php3", ".php4", ".php5", ".phtml", ".jsp", ".jspx", ".cgi", ".pl", ".py", ".rb",
            // executables / scripts
            ".exe", ".dll", ".com", ".scr", ".msi", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".js.map.exe", ".sh", ".jar", ".war",
            // web server configuration
            ".htaccess", ".htpasswd",
        };

        public static bool IsAllowed(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            var name = filename.Trim();

            // ".htaccess" has no extension for Path.GetExtension on some inputs: test the name as well
            return !BlockedExtensions.Contains(Path.GetExtension(name)) && !BlockedExtensions.Contains(name);
        }
    }
}
