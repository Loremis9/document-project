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

        // Crée un dossier (et ses parents si besoin)
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        // Crée un fichier .md avec un nom donné et un contenu optionnel
        public static void CreateMarkdownFile(string directory, string fileName, string content = "")
        {
            if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                fileName += ".md";
            string filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
        }
        public static string ReadFileContent(string filePath)
        {
            return File.ReadAllText(filePath);
        }
        public static string GetDirectoryTree(string rootPath, bool includeFiles = true, string indent = "", bool isLast = true)
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
        // Ajoute ou remplace les tags d'un objet (Documentation ou DocumentationFile)
        public static void SetTags(object docOrFile, IEnumerable<string> tags)
        {
            string tagString = string.Join(",", tags);
            if (docOrFile is Documentation doc)
                doc.Tags = tagString;
            else if (docOrFile is DocumentationFile file)
                file.Tags = tagString;
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
    }
}
