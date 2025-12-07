using System.Diagnostics;
using System.Text;

namespace _222303026_proje3
{
    public static class PuterJsWrapper
    {
        /// <summary>
        /// Calls a JavaScript function from puter.js and returns its image output.
        /// </summary>
        /// <param name="functionName">The name of the function to call</param>
        /// <param name="args">The arguments to pass to the function</param>
        /// <returns>The function's output (image) as byte[]</returns>
        public static byte[] CallImageFunction(string functionName, params string[] args)
        {
            string scriptPath = "puter.js";
            string arguments = $"{scriptPath} {functionName} {string.Join(" ", args)}";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // The image data is expected to be written to standard output as a byte stream
            using var ms = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(ms);

            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"puter.js error: {error}");
            }

            return ms.ToArray();
        }
    }
}