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

        public static bool IsValidSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            var s = segment.Trim();
            return s != "." && s != ".." && s.IndexOfAny(InvalidChars) < 0;
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
