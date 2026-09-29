using System;
using System.IO;
using System.Linq;

namespace TrinityText.Business
{
    /// <summary>
    /// Validates single path segments (folder / subfolder / output file base names) that are later
    /// concatenated into filesystem or FTP paths during publishing. Rejects path separators, drive
    /// separators and relative tokens to prevent directory traversal.
    /// </summary>
    public static class PathSafety
    {
        private static readonly char[] InvalidChars =
            Path.GetInvalidFileNameChars().Concat(['/', '\\', ':']).Distinct().ToArray();

        private const int MaxSegmentLength = 255;

        // device names Windows resolves to hardware ("CON", "NUL.txt", "com1", ...): a file or folder with such a name cannot be created
        private static readonly System.Collections.Generic.HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        public static bool IsValidSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            var s = segment.Trim();
            if (s == "." || s == ".." || s.Length > MaxSegmentLength)
            {
                return false;
            }

            // control characters (CR / LF reach FTP commands and mail headers), separators, drive separators
            if (s.IndexOfAny(InvalidChars) >= 0 || s.Any(char.IsControl))
            {
                return false;
            }

            // Windows drops trailing dots: "name." and "name" would be the same entry
            if (s.EndsWith('.'))
            {
                return false;
            }

            var dot = s.IndexOf('.');
            var stem = (dot >= 0 ? s[..dot] : s).TrimEnd();
            return !ReservedNames.Contains(stem);
        }

        public static bool IsValidSegmentOrEmpty(string segment)
            => string.IsNullOrWhiteSpace(segment) || IsValidSegment(segment);

        /// <summary>
        /// Canonicalizes <paramref name="path"/> and verifies it stays inside <paramref name="root"/>.
        /// Defence in depth for paths built by concatenating stored names.
        /// </summary>
        public static string EnsureWithinRoot(string root, string path)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path '{path}' is outside the allowed directory");
            }

            return fullPath;
        }

        public static void EnsureValidSegment(string segment, string fieldName)
        {
            if (!IsValidSegment(segment))
            {
                throw new ArgumentException($"Invalid value for {fieldName}: '{segment}'");
            }
        }
    }
}
