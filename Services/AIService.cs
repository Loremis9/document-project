using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.Graph;
using OpenAI;
namespace WEBAPI_m1IL_1.Services
{
    public class AIService : IChatGptMarkdownFormatterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "YOUR_OPENAI_API_KEY";
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
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
    }
}
