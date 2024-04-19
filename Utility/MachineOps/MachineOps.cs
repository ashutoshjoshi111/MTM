using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

namespace Utility.MachineOps
{
    public class MachineOps
    {
        public MachineOps() { }

        /// <summary>
        /// Gives details of current CPU utilization.
        /// </summary>
        /// <returns>float</returns>
        public static float getCPUUsage()
        {

            string command = @"typeperf ""\Processor(_Total)\% Processor Time"" -sc 1";
            string output = ExecuteCommand(command);
            float cpuUsage = ParseCpuUsage(output);           
            return cpuUsage;
        }

        public static string ExecuteCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        public static float ParseCpuUsage(string output)
        {
            // Split the output into lines.
            string[] lines = output.Split('\n');

            // The CPU usage value is expected on the second line (index 1) after the header.
            if (lines.Length > 1)
            {
                string dataLine = lines[2]; // Get the second line where the data resides.
                string[] parts = dataLine.Split(',');

                if (parts.Length > 1) // Ensure there's at least two elements (date and value)
                {
                    string cpuUsageString = parts[1].Trim('"'); // Trim quotes if present around the CPU usage value.
                    cpuUsageString = cpuUsageString.Replace(@"""", "");

                    if (float.TryParse(cpuUsageString, NumberStyles.Any, CultureInfo.InvariantCulture, out float cpuUsage))
                    {
                        return cpuUsage;
                    }
                }
            }

            return -1; // Return -1 if parsing fails.
        }

    }
}
