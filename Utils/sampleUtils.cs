using WEBAPI_m1IL_1.Models;
using System.Text;

namespace WEBAPI_m1IL_1.Utils
{

    public static class SampleUtils
    {
        public static string GenerateUUID()
        {
            Guid myuuid = Guid.NewGuid();
            return myuuid.ToString();
        }
        public static IEnumerable<string> ChunkString(string str, int maxChunkSize = 20)
        {
            if (string.IsNullOrEmpty(str) || maxChunkSize <= 0)
                yield break;

            for (int i = 0; i < str.Length; i += maxChunkSize)
            {
                int length = Math.Min(maxChunkSize, str.Length - i);
                yield return str.Substring(i, length);
            }
        }

        public static List<string> PrepareChunksForOllama(
        List<(int DocId, string Snippet)> snippets,
        int maxChunkSize = 12000)
        {
            var chunks = new List<string>();
            chunks.Add($"--------------- Début ----------------- \n ");

            // 1. Group by DocId
            var grouped = snippets
                .GroupBy(s => s.DocId)
                .OrderBy(g => g.Key); // optionnel, pour trier par DocId

            foreach (var group in grouped)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"=== Documentation {group.Key} ===");

                foreach (var item in group)
                {
                    builder.AppendLine(item.Snippet);
                }

                string docText = builder.ToString();

                // 2. Chunk the text for this DocId
                var docChunks = SampleUtils.ChunkString(docText, maxChunkSize);

                // 3. Préfixer chaque chunk avec le DocId pour contexte
                foreach (var chunk in docChunks)
                {
                    chunks.Add($"[DocId {group.Key}] {chunk}");
                }
            }
            chunks.Add($"--------------- FIN ----------------- \n");
            return chunks;
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

        public static string ConvertImageToBase64(string path)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(path);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            return base64ImageRepresentation;
        }

    }
}