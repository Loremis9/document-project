using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using NPOI.SS.Formula.Functions;
using DocumentFormat.OpenXml.ExtendedProperties;
using HandlebarsDotNet;

namespace WEBAPI_m1IL_1.Config
{

    public class ConfigCompose
    {
        public List<OllamaContainer> Containers { get; set; } = new();
        public PostgresTemplateModel Postgres { get; set; } = new();
        public MinIoTemplateModel MinIo { get; set; } = new();
        public ConfigCompose() { }

        public void SetupAndRun()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string directory = Directory.GetCurrentDirectory();

            var containers = config.GetSection("Ollama:Containers").Get<List<OllamaContainer>>();
            var postgres = config.GetSection("Postgres").Get<PostgresTemplateModel>();
            var minIO = config.GetSection("MinIO").Get<MinIoTemplateModel>();
            if (containers == null || containers.Count == 0)
            {
                Console.WriteLine("❌ Aucun conteneur trouvé.");
                return;
            }
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Config", "docker-compose.template.yaml");
            var template = File.ReadAllText(templatePath);
            var templateHandlebars = Handlebars.Compile(template);

            // Générer un script entrypoint.sh spécifique

            var scriptDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "script");
            foreach (var container in containers)
            {
                var entrypointContent = $"#!/bin/sh\nollama serve &\nsleep 5\nollama run {container.Model} || true\nwait";

                var scriptPath = Path.Combine(scriptDir, $"entrypoint-{container.Name}.sh");

                File.WriteAllText(scriptPath, entrypointContent);
                File.SetAttributes(scriptPath, File.GetAttributes(scriptPath));
                var entrypointScriptName = $"entrypoint-{container.Name}.sh";
            }
            var data = new
            {
                Containers = containers,
                Postgres = postgres,
                MinIO = minIO
            };

            var result = templateHandlebars(data);


            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var outputPath = Path.Combine(projectRoot, "docker-compose.yml");
            File.WriteAllText(outputPath, result);
            Console.WriteLine("✅ docker-compose.yml généré.");
            if (!IsDockerComposeRunning())
            {
                StartDockerCompose(projectRoot);
            }

        }

        static bool IsDockerComposeRunning()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose ps --status=running",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            if (process == null) return false;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Si une ligne contient un service, Docker Compose tourne
            return output.Split('\n').Any(line => line.Trim().StartsWith("ollama-"));
        }
        static void StartDockerCompose(string projectRoot)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose up -d",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectRoot
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();
            Console.WriteLine("Docker Compose lancé.");
        }
    }
}

