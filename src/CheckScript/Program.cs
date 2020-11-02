using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CheckScript
{
    /// <summary>
    /// This is a replacement for the Check.ps1 which became too unruly as powershell was not suited for proper
    /// handling of stdout and stderr manipulation.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Bundles all the logging settings in one place.
        /// </summary>
        /// <remarks>Additional fields are expected in the future thus a shallow class.</remarks>
        private class Setting
        {
            public readonly string Session;
            public readonly string Timestamp;

            public Setting(string session, string timestamp)
            {
                Session = session;
                Timestamp = timestamp;
            }
        }

        /// <summary>
        /// Log which expression to execute and actually execute it.
        /// </summary>
        /// <param name="expression">Powershell expression</param>
        /// <param name="label">Label to be included in the prefix</param>
        /// <param name="setting">Logging settings</param>
        /// <returns>exit code</returns>
        private static int LogAndExecute(string expression, string label, Setting setting)
        {
            string prefix = $"{setting.Session}:{setting.Timestamp}: {label}:";
            string prefixShort = $"{setting.Session}:{setting.Timestamp}:";

            var title = $"Running: {expression}";
            var border = new String('-', title.Length);

            Console.WriteLine(prefixShort);
            Console.WriteLine($"{prefixShort} +-{border}-+");
            Console.WriteLine($"{prefixShort}   Running: {expression}");
            Console.WriteLine($"{prefixShort} +-{border}-+");
            Console.WriteLine(prefixShort);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = "powershell.exe",
                    Arguments = expression,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                Console.WriteLine($"{prefix} {e.Data}");
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"{prefix}E: {e.Data}");
                }
            };

            process.Start();
            try
            {
                // Asynchronously read the standard output and error of the spawned process.
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"{prefixShort} The exit code was 0 for: {expression}");
                    return process.ExitCode;
                }

                Console.Error.WriteLine(
                    $"{prefixShort} Failed to execute {expression}, the exit code was: {process.ExitCode}");
                return process.ExitCode;
            }
            finally
            {
                process.Close();
            }
        }

        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.Error.WriteLine($"Unexpected arguments: {string.Join(" ", args)}");
            }

            var session = ThreeLetterWords.TheList[new Random().Next(ThreeLetterWords.TheList.Count)];

            var setting = new Setting(
                session,
                DateTime.Now.ToString("HH:mm:ss"));

            var scriptsLabels = new List<(string, string)>
            {
                ("CheckLicenses.ps1", "Licenses"),
                ("CheckFormat.ps1", "Format"),
                ("CheckBiteSized.ps1", "BiteSized"),
                ("CheckDeadCode.ps1", "DeadCode"),
                ("CheckTodos.ps1", "Todos"),
                ("Doctest.ps1 -check", "Doctest"),
                ("BuildForDebug.ps1", "Build"),
                ("Test.ps1", "Test"),
                ("InspectCode.ps1", "Inspect"),
                ("CheckPushCommitMessages.ps1", "CommitMessages")
            };

            foreach (var (script, label) in scriptsLabels)
            {
                var exitCode = LogAndExecute(Path.Join(".", script), label, setting);
                if (exitCode == 0) continue;

                Environment.ExitCode = exitCode;
                return;
            }

            Console.WriteLine("All checks passed successfully. You can now push the commits.");
        }
    }
}