using System.Diagnostics;

namespace BedBrigade.Tests
{
    public static class TestHelper
    {
        public static bool RunningInPipeline
        {
            get
            {
                string? account = Environment.GetEnvironmentVariable("APPVEYOR_ACCOUNT_NAME");

                return !string.IsNullOrEmpty(account);
            }
        }
        
        public static string GetSolutionPath()
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            int count = 0;
            const int maxDepth = 1000;

            while (count < maxDepth)
            {
                string[] files = Directory.GetFiles(currentPath, "*.sln");

                if (files.Any())
                {
                    return currentPath;
                }

                string? parentPath = Path.GetDirectoryName(currentPath);

                //We are at the root we did not find anything
                if (parentPath == null || parentPath == currentPath)
                    throw new DirectoryNotFoundException("Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);

                currentPath = parentPath;
                count++;
            }

            throw new DirectoryNotFoundException("Reached Max Depth. Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// Execute an external program.
        /// </summary>
        /// <param name="executablePath">Path and filename of the executable.</param>
        /// <param name="arguments">Arguments to pass to the executable.</param>
        /// <param name="windowStyle">Window style for the process (hidden, minimized, maximized, etc).</param>
        /// <param name="waitUntilFinished">Wait for the process to finish.</param>
        /// <returns>Exit Code</returns>
        public static int Shell(string executablePath, string arguments, ProcessWindowStyle windowStyle, bool waitUntilFinished)
        {
            string fileName = "";

            try
            {
                Process process = new Process();
                string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(executablePath) ?? string.Empty);

                //Look for the file in the executing assembly directory
                if (File.Exists(assemblyPath))
                {
                    fileName = assemblyPath;
                    process.StartInfo.FileName = assemblyPath;
                }
                else // if there is no path to the file, an error will be thrown
                {
                    fileName = executablePath;
                    process.StartInfo.FileName = executablePath;
                }

                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.WindowStyle = windowStyle;

                //Start the Process
                process.Start();

                if (waitUntilFinished)
                {
                    process.WaitForExit();
                }

                if (waitUntilFinished)
                    return process.ExitCode;

                return 0;
            }
            catch
            {
                string message = string.Format("Shell Fail: {0} {1}", fileName, arguments);
                throw new ApplicationException(message);
            }
        }

        public static void DeleteOldHtmlFiles()
        {
            string tempPath = Path.GetTempPath();
            string[] files = Directory.GetFiles(tempPath, "*.html");

            foreach (string file in files)
            {
                DateTime lastWriteTime = File.GetLastWriteTime(file);

                if (lastWriteTime < DateTime.Now.AddDays(-1))
                {
                    File.Delete(file);
                }
            }
        }

        public static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        public static void TryOpenFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));

            // Don't fail tests for UX niceties.
            try
            {
                // Avoid trying to open a browser in CI/headless environments.
                if (RunningInPipeline)
                    return;

                // Ensure we hand the OS a fully-qualified path.
                string fullPath = Path.GetFullPath(filePath);

                if (IsWindows())
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                    return;
                }

                // For macOS/Linux, a file:// URI is more reliable than a raw path.
                string fileUri = new Uri(fullPath).AbsoluteUri;

                if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo("open", fileUri) { UseShellExecute = false });
                    return;
                }

                if (OperatingSystem.IsLinux())
                {
                    // Many Linux desktops rely on xdg-open.
                    // If DISPLAY/WAYLAND_DISPLAY aren't set, it's likely headless; just skip.
                    string? display = Environment.GetEnvironmentVariable("DISPLAY");
                    string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
                    if (string.IsNullOrWhiteSpace(display) && string.IsNullOrWhiteSpace(waylandDisplay))
                        return;

                    // Some browsers (e.g., Brave with certain sandbox/permissions) may not be able to read /tmp.
                    // Prefer opening with Google Chrome if it's available.
                    string? chromePath = FindLinuxExecutableInPath("google-chrome")
                                         ?? FindLinuxExecutableInPath("google-chrome-stable")
                                         ?? FindLinuxExecutableInPath("chrome")
                                         ?? FindLinuxExecutableInPath("chromium")
                                         ?? FindLinuxExecutableInPath("chromium-browser");

                    if (!string.IsNullOrWhiteSpace(chromePath))
                    {
                        Process.Start(new ProcessStartInfo(chromePath, fileUri) { UseShellExecute = false });
                        return;
                    }

                    Process.Start(new ProcessStartInfo("xdg-open", fileUri) { UseShellExecute = false });
                    return;
                }

                // Fallback: best-effort shell execute.
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to open file '{filePath}'. {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string? FindLinuxExecutableInPath(string executableName)
        {
            if (!OperatingSystem.IsLinux())
                return null;

            string? path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string candidate = Path.Combine(dir, executableName);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        public static bool ThisComputerHasExcelInstalled()
        {
            try
            {
                Type? excelType = Type.GetTypeFromProgID("Excel.Application");
                return excelType != null;
            }
            catch
            {
                return false;
            }
        }



        public static bool IsRiderInstalled()
        {
            try
            {
                if (IsWindows())
                    return IsRiderInstalledOnWindows();

                if (OperatingSystem.IsLinux())
                    return IsRiderInstalledOnLinux();

                if (OperatingSystem.IsMacOS())
                    return Directory.Exists("/Applications/Rider.app");

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRiderInstalledOnLinux()
        {
            // Snap install (common): /snap/rider/current/bin/rider or /var/lib/snapd/snap/rider
            if (File.Exists("/snap/rider/current/bin/rider"))
                return true;

            if (Directory.Exists("/var/lib/snapd/snap/rider"))
                return true;

            // Toolbox install (common): ~/.local/share/JetBrains/Toolbox/apps/Rider
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                string toolboxApps = Path.Combine(home, ".local", "share", "JetBrains", "Toolbox", "apps", "Rider");
                if (Directory.Exists(toolboxApps))
                    return true;

                // Older/hypothetical location
                string jetBrainsLocal = Path.Combine(home, ".local", "share", "JetBrains", "Rider");
                if (Directory.Exists(jetBrainsLocal))
                    return true;
            }

            // Apt/manual installs often land under /opt
            if (Directory.Exists("/opt/JetBrains"))
            {
                if (Directory.GetDirectories("/opt/JetBrains", "Rider*", SearchOption.TopDirectoryOnly).Any())
                    return true;
            }

            if (Directory.Exists("/opt"))
            {
                if (Directory.GetDirectories("/opt", "rider*", SearchOption.TopDirectoryOnly).Any())
                    return true;
            }

            // PATH check: some installs provide a 'rider' launcher
            return FindLinuxExecutableInPath("rider") != null;
        }

        private static bool IsRiderInstalledOnWindows()
        {
            // Check common install folders first (fast).
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (Directory.Exists(Path.Combine(programFiles, "JetBrains")) ||
                Directory.Exists(Path.Combine(programFilesX86, "JetBrains")))
            {
                return true;
            }

            // Toolbox (default): %LocalAppData%\JetBrains\Toolbox\apps\Rider
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                string toolboxRider = Path.Combine(localAppData, "JetBrains", "Toolbox", "apps", "Rider");
                if (Directory.Exists(toolboxRider))
                    return true;
            }

            // Registry uninstall keys: look for "JetBrains Rider" display name.
            // Best-effort only; if registry access fails, we still return false.
            try
            {
                using Microsoft.Win32.RegistryKey? localMachine = Microsoft.Win32.Registry.LocalMachine;
                if (localMachine != null)
                {
                    string[] uninstallPaths =
                    [
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                    ];

                    foreach (string uninstallPath in uninstallPaths)
                    {
                        using Microsoft.Win32.RegistryKey? uninstallKey = localMachine.OpenSubKey(uninstallPath);
                        if (uninstallKey == null)
                            continue;

                        foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                        {
                            using Microsoft.Win32.RegistryKey? subKey = uninstallKey.OpenSubKey(subKeyName);
                            string? displayName = subKey?.GetValue("DisplayName") as string;

                            if (!string.IsNullOrWhiteSpace(displayName) &&
                                displayName.Contains("Rider", StringComparison.OrdinalIgnoreCase) &&
                                displayName.Contains("JetBrains", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

    }
}
