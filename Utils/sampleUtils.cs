using WEBAPI_m1IL_1.Models;
using System.Text;
using System.IO.Compression;

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
        public static string GetDirectoryTreeFromZipStream(Stream zipStream, bool includeFiles = true)
        {
            var result = new StringBuilder();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Créer une structure arborescente
            var root = new Dictionary<string, object>();

            foreach (var entry in archive.Entries)
            {
                var parts = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var current = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    bool isFile = (i == parts.Length - 1) && !string.IsNullOrEmpty(entry.Name);

                    if (isFile && !includeFiles)
                        break;

                    if (!current.ContainsKey(part))
                    {
                        current[part] = isFile ? null : new Dictionary<string, object>();
                    }

                    if (!isFile)
                        current = (Dictionary<string, object>)current[part];
                }
            }

            PrintTree(root, result, "", true);
            return result.ToString();
        }

        private static void PrintTree(Dictionary<string, object> node, StringBuilder sb, string indent, bool isLast)
        {
            var items = node.Keys.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                bool lastItem = i == items.Count - 1;
                sb.AppendLine($"{indent}{(lastItem ? "└── " : "├── ")}{items[i]}");

                if (node[items[i]] is Dictionary<string, object> subDir)
                {
                    PrintTree(subDir, sb, indent + (lastItem ? "    " : "│   "), lastItem);
                }
            }
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