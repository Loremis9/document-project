using WEBAPI_m1IL_1.Models;
using System.Text;

namespace WEBAPI_m1IL_1.Utils
{
    public static class SampleUtils
{
    public static string GenerateUUID()
    {
            Guid myuuid = Guid.NewGuid();
            return  myuuid.ToString();
    }
    public static IEnumerable<string> ChunkString(string str, int maxChunkSize=20)
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
}

}