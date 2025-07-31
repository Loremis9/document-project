using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.Graph;
using OpenAI;
using System.Collections.Concurrent;
using WEBAPI_m1IL_1.Utils;
namespace WEBAPI_m1IL_1.Services
{
    public class OllamaAnswer
{
    public string Answer { get; set; }
    public List<int> DocumentationIds { get; set; }
}
    public class AIService : IChatGptMarkdownFormatterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "YOUR_OPENAI_API_KEY";
        private const string Endpoint = "";
        private const string EndpointOllama = "http://localhost:11434";
        private readonly ConcurrentDictionary<string, byte[]> _userContexts;

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _userContexts = new ConcurrentDictionary<string, byte[]>();

        }
        //Poser une question à l'utilsateur concernant ça demande ( avoir plus de précision)
        public async Task<string> AskAi(int userId,string? content,string ask, string? question,string? contextId){
            if(!string.IsNullOrWhiteSpace(contextId)){
                contextId = SampleUtils.GenerateUUID();
            }
            switch(ask){
                case "tag":
                return await SendToAi($"{AiPrompts.tags} :\n{content}",userId,contextId);
                case "search" :  
                return await SendToAi($"{AiPrompts.SearchPrompt} :\n{content}",userId,contextId);
                case "convert" :  
                return await SendToAi($"{AiPrompts.ConvertToMarkdownPrompt}  :\n{content}",userId,contextId);
                case "reformule":   
                return await SendToAi($"{AiPrompts.reformule} : ${question}",userId,contextId);
                default:
                return null;
            }
        }

        public async Task<string> FormatAsMarkdownAsync(string plainText)
        {
            var requestBody = new
            {
                model = "gpt-4", // ou "gpt-3.5-turbo"
                messages = new[]
                {
                new { role = "system", content = "Tu es un assistant qui reformate le texte en Markdown propre et lisible. si c'est déja lisible et propre ne fait absolument rien" },
                new { role = "user", content = $"Formate ce texte en Markdown si cela est nécessaire :\n\n{plainText}" }
            },
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erreur API OpenAI: {response.StatusCode} - {responseString}");

            using var document = JsonDocument.Parse(responseString);
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        public async Task<string> SendToAi(string prompt,int userId,string contextId)
        {
            _userContexts.TryGetValue(userId.ToString(), out var context);

            var payload = new
            {
                model = "mistral", // modèle téléchargé
                prompt = prompt,
                context = context + contextId, // Injecte le contexte précédent
                stream = false // pour récupérer toute la réponse d'un coup
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(Endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
             var ollamaResponse = JsonSerializer.Deserialize<OllamaAnswer>(responseString);
            return ollamaResponse?.Answer ?? string.Empty;
    }


}
}
