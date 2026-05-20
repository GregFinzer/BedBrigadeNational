namespace BedBrigade.Common.Logic
{
    /// <summary>
    /// Provides helper methods for resolving and creating media paths under the application's <c>wwwroot</c> folder.
    /// </summary>
    public static class MediaPathUtil
    {
        private const string LowerMediaDirectoryName = "media";
        private const string UpperMediaDirectoryName = "Media";
        private const string WebRootDirectoryName = "wwwroot";

        /// <summary>
        /// Gets an existing media directory or file path when one can be resolved, otherwise creates the target directory under the preferred media root.
        /// </summary>
        /// <param name="baseDirectory">The application base directory that contains the <c>wwwroot</c> folder.</param>
        /// <param name="segments">Additional path segments to append beneath the media root.</param>
        /// <returns>The resolved or newly created media path.</returns>
        public static string GetMediaDirectory(string baseDirectory, params string[] segments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            // Reuse an existing path first so the current on-disk casing is preserved.
            string? existingPath = ResolveExistingMediaPath(baseDirectory, segments);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                return existingPath;
            }

            // If nothing exists yet, create the directory under the preferred media root.
            string targetPath = BuildMediaPath(GetPreferredMediaRoot(baseDirectory), segments);
            Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        /// <summary>
        /// Resolves an existing media directory or file path using both exact-case and case-insensitive matching.
        /// </summary>
        /// <param name="baseDirectory">The application base directory that contains the <c>wwwroot</c> folder.</param>
        /// <param name="segments">Additional path segments to append beneath the media root.</param>
        /// <returns>The resolved path when found; otherwise, <see langword="null"/>.</returns>
        public static string? ResolveExistingMediaPath(string baseDirectory, params string[] segments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            // Try direct matches first for the known media root casing options.
            foreach (string mediaRoot in GetMediaRootCandidates(baseDirectory))
            {
                string exactPath = BuildMediaPath(mediaRoot, segments);
                if (Directory.Exists(exactPath) || File.Exists(exactPath))
                {
                    return exactPath;
                }
            }

            // Fall back to a case-insensitive search so Linux deployments can still find legacy paths.
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

        /// <summary>
        /// Gets the preferred media root path, favoring an existing lowercase <c>media</c> directory and then an existing uppercase <c>Media</c> directory.
        /// </summary>
        /// <param name="baseDirectory">The application base directory that contains the <c>wwwroot</c> folder.</param>
        /// <returns>The preferred media root path.</returns>
        public static string GetPreferredMediaRoot(string baseDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            string lowercaseMediaRoot = Path.Combine(baseDirectory, WebRootDirectoryName, LowerMediaDirectoryName);
            if (Directory.Exists(lowercaseMediaRoot))
            {
                return lowercaseMediaRoot;
            }

            string uppercaseMediaRoot = Path.Combine(baseDirectory, WebRootDirectoryName, UpperMediaDirectoryName);
            if (Directory.Exists(uppercaseMediaRoot))
            {
                return uppercaseMediaRoot;
            }

            return lowercaseMediaRoot;
        }

        /// <summary>
        /// Gets the supported media root path candidates for the supplied base directory.
        /// </summary>
        /// <param name="baseDirectory">The application base directory that contains the <c>wwwroot</c> folder.</param>
        /// <returns>A read-only list of media root candidates to probe.</returns>
        public static IReadOnlyList<string> GetMediaRootCandidates(string baseDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

            string lowercaseMediaRoot = Path.Combine(baseDirectory, WebRootDirectoryName, LowerMediaDirectoryName);
            string uppercaseMediaRoot = Path.Combine(baseDirectory, WebRootDirectoryName, UpperMediaDirectoryName);

            if (string.Equals(lowercaseMediaRoot, uppercaseMediaRoot, StringComparison.Ordinal))
            {
                return [lowercaseMediaRoot];
            }

            return [lowercaseMediaRoot, uppercaseMediaRoot];
        }

        /// <summary>
        /// Builds a media path by combining the media root with normalized path segments.
        /// </summary>
        /// <param name="mediaRoot">The media root path.</param>
        /// <param name="segments">The path segments to normalize and append.</param>
        /// <returns>The combined path.</returns>
        private static string BuildMediaPath(string mediaRoot, params string[] segments)
        {
            IEnumerable<string> cleanSegments = segments.SelectMany(SplitSegments);
            return cleanSegments.Aggregate(mediaRoot, Path.Combine);
        }

        /// <summary>
        /// Splits a path fragment into clean directory segments, removing empty values and trimming whitespace.
        /// </summary>
        /// <param name="segment">The raw path fragment.</param>
        /// <returns>The normalized path segments.</returns>
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

