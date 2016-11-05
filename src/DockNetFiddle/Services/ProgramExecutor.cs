using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DockNetFiddle.Models;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;

namespace DockNetFiddle.Services
{
    public class ProgramExecutor : IProgramExecutor
    {
        private IHostingEnvironment env;
        public ProgramExecutor(IHostingEnvironment env)
        {
            this.env = env;
        }

        public string Execute(ProgramSpecification program)
        {
            var tempFolder = GetTempFolderPath();
            try
            {
                CopyToFolder(tempFolder, program);                
                return ExecuteDockerCommand(tempFolder);
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
        }

        private string GetTempFolderPath() => 
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        private void CopyToFolder(string folder, ProgramSpecification program)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Program.cs"), program.Program);
            File.WriteAllText(Path.Combine(folder, "project.json"), program.ProjectJSON);
        }

        private string ExecuteDockerCommand(string folder)
        {
            var dockerFormattedTempFolder = folder
                    .Replace("C:", "/c")
                    .Replace('\\', '/');
            var proc = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(env.ContentRootPath, "executeInDocker.bat"),
                Arguments = dockerFormattedTempFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            var result = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return String.IsNullOrWhiteSpace(error) ? result : error;
        }
    }
}
