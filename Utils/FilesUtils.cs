using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using CsvHelper;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using WEBAPI_m1IL_1.Models;
namespace WEBAPI_m1IL_1.Utils
{
    public class FilesUtils
    {

        // Parcourt tous les fichiers et dossiers à partir d'un chemin racine
        public static IEnumerable<string> GetAllFilesAndDirectories(string rootPath)
        {
            foreach (var dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
                yield return dir;
            foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))     
                    yield return file;
        }


        // Crée un fichier .md avec un nom donné et un contenu optionnel
        public static void CreateMarkdownFile(string directory, string fileName, string content = "")
        {
            if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                fileName += ".md";
            string filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
        }
        public static string ReadFile(string path){
            return System.IO.File.ReadAllText(path);
        }

        public static void OverwriteFile(string filePath, string content)
        {
        File.WriteAllText(filePath, content);
        }

        public static string GetDirectoryTree(string rootPath, bool includeFiles = true,string indent = "",bool isLast = true)
        {
            var result = new System.Text.StringBuilder();
            var dirInfo = new DirectoryInfo(rootPath);
            result.AppendLine(indent + (isLast ? "└── " : "├── ") + dirInfo.Name);
            indent += isLast ? "    " : "│   ";

            var subDirs = dirInfo.GetDirectories();
            for (int i = 0; i < subDirs.Length; i++)
            {
                bool lastDir = (i == subDirs.Length - 1) && (!includeFiles || dirInfo.GetFiles().Length == 0);
                result.Append(GetDirectoryTree(subDirs[i].FullName, includeFiles, indent, lastDir));
            }
            if (includeFiles)
            {
                var files = dirInfo.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    bool lastFile = (i == files.Length - 1);
                    result.AppendLine(indent + (lastFile ? "└── " : "├── ") + files[i].Name);
                }
            }
            return result.ToString();
        }

        // Récupère la liste des tags d'un objet (Documentation ou DocumentationFile)
        public static List<string> GetTags(object docOrFile)
        {
            string tagString = null;
            if (docOrFile is Documentation doc)
                tagString = doc.Tags;
            else if (docOrFile is DocumentationFile file)
                tagString = file.Tags;
            return string.IsNullOrWhiteSpace(tagString)
                ? new List<string>()
                : tagString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

    
        public static string ConvertCsvFileToMarkdown(string csvFilePath)
    {
        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>();

        var sb = new StringBuilder();

        bool headerWritten = false;

        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;

            if (!headerWritten)
            {
                // Header row
                sb.Append("| ");
                sb.Append(string.Join(" | ", dict.Keys));
                sb.AppendLine(" |");

                // Separator row
                sb.Append("| ");
                sb.Append(string.Join(" | ", dict.Keys.Select(k => "---")));
                sb.AppendLine(" |");

                headerWritten = true;
            }

            // Data rows
            sb.Append("| ");
            sb.Append(string.Join(" | ", dict.Values.Select(v => v?.ToString()?.Replace("|", "\\|") ?? "")));
            sb.AppendLine(" |");
        }

        return sb.ToString();
    }
        public static string ExtractTextFromPdf(string pdfFilePath)
        {
            var sb = new StringBuilder();

            using (PdfDocument document = PdfDocument.Open(pdfFilePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
        }

        public static List<String> DeleteFileUnauthorized(string path){
            var deletedFiles = new List<String>();
            var forbiddenExtensions = new[] { ".exe", ".bat", ".cmd", ".js", ".ps1", ".vbs", ".com", ".scr", ".pif", ".jar", ".msi", ".dll", ".sys" };
            
            try
            {
                // Parcourt récursivement tous les fichiers dans le répertoire
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (forbiddenExtensions.Contains(ext))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            deletedFiles.Add(file);
                        }
                        catch (Exception ex)
                        {
                            // Log l'erreur mais continue avec les autres fichiers
                            Console.WriteLine($"Erreur lors de la suppression de {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du parcours du répertoire {path}: {ex.Message}");
            }
            return deletedFiles;
        }
    }


}
