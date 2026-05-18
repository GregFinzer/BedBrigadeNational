namespace BedBrigade.Common.Logic
{
    public static class MediaPathUtil
    {
        private const string LowerMediaDirectoryName = "media";
        private const string UpperMediaDirectoryName = "Media";

        public static string GetMediaDirectory(string baseDirectory, params string[] segments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            string? existingPath = ResolveExistingMediaPath(baseDirectory, segments);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                return existingPath;
            }

            string targetPath = BuildMediaPath(GetPreferredMediaRoot(baseDirectory), segments);
            Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        public static string? ResolveExistingMediaPath(string baseDirectory, params string[] segments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            foreach (string mediaRoot in GetMediaRootCandidates(baseDirectory))
            {
                string exactPath = BuildMediaPath(mediaRoot, segments);
                if (Directory.Exists(exactPath) || File.Exists(exactPath))
                {
                    return exactPath;
                }
            }

            foreach (string mediaRoot in GetMediaRootCandidates(baseDirectory))
            {
                string candidatePath = BuildMediaPath(mediaRoot, segments);
                string? resolvedPath = FileUtil.ResolveCaseInsensitivePath(candidatePath);
                if (!string.IsNullOrWhiteSpace(resolvedPath)
                    && (Directory.Exists(resolvedPath) || File.Exists(resolvedPath)))
                {
                    return resolvedPath;
                }
            }

            return null;
        }

        public static string GetPreferredMediaRoot(string baseDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            string lowercaseMediaRoot = Path.Combine(baseDirectory, "wwwroot", LowerMediaDirectoryName);
            if (Directory.Exists(lowercaseMediaRoot))
            {
                return lowercaseMediaRoot;
            }

            string uppercaseMediaRoot = Path.Combine(baseDirectory, "wwwroot", UpperMediaDirectoryName);
            if (Directory.Exists(uppercaseMediaRoot))
            {
                return uppercaseMediaRoot;
            }

            return lowercaseMediaRoot;
        }

        public static IReadOnlyList<string> GetMediaRootCandidates(string baseDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            string lowercaseMediaRoot = Path.Combine(baseDirectory, "wwwroot", LowerMediaDirectoryName);
            string uppercaseMediaRoot = Path.Combine(baseDirectory, "wwwroot", UpperMediaDirectoryName);

            if (string.Equals(lowercaseMediaRoot, uppercaseMediaRoot, StringComparison.Ordinal))
            {
                return [lowercaseMediaRoot];
            }

            return [lowercaseMediaRoot, uppercaseMediaRoot];
        }

        private static string BuildMediaPath(string mediaRoot, params string[] segments)
        {
            IEnumerable<string> cleanSegments = segments.SelectMany(SplitSegments);
            return cleanSegments.Aggregate(mediaRoot, Path.Combine);
        }

        private static IEnumerable<string> SplitSegments(string? segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return [];
            }

            return segment.Split(['/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}

