using System.Diagnostics;

namespace BedBrigade.SpeakIt.Tests
{
    public static class TestHelper
    {

        public static List<string> ExcludeDirectories = new List<string> { "Administration", "Layout" };
        public static List<string> ExcludeFiles = new List<string> { "Error.razor", "CustomErrorBoundary.razor", "SignUpGrid.razor.cs", "SignUpHelper.cs" };
        public static List<string> WildcardPatterns = new List<string> { "*.razor", "*.cs" };

        public static List<string> GetSourceDirectories()
        {
            string solutionPath = TestHelper.GetSolutionPath();
            string componentsPath = Path.Combine(solutionPath, "BedBrigade.Client", "Components");
            string modelPath = Path.Combine(solutionPath, "BedBrigade.Common", "Models");
            return new List<string>() { componentsPath, modelPath };
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


        public static bool IsRunningUnderGitHubActions()
        {
            return Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
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
    }
}
