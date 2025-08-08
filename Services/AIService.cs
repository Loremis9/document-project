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
using System.Text.Json.Serialization;
using UglyToad.PdfPig.Tokens;
using WEBAPI_m1IL_1.Config;
namespace WEBAPI_m1IL_1.Services
{
    public class OllamaAnswer
    {
        [JsonPropertyName("response")]
        public string Answer { get; set; }
        public List<int> DocumentationIds { get; set; }
    }

    public class AIService
    {
        public static string Config(String props)
        {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
            switch (props)
            {
                case "Port":
                    return config["Ollama:Containers:0:Port"];
                case "Model":
                    return config["MainAIModel:Model"];
                case "AllModel":
                    var sb = new StringBuilder();
                    var models = config.GetSection("Ollama:Containers").Get<List<OllamaContainer>>();
                    foreach (var model in models)
                    {
                        sb.Append($"{model.Model},");
                    }
                    if (sb.Length > 0)
                        sb.Length--;
                    return sb.ToString();
                default:
                    throw new ArgumentException("Invalid configuration property requested.");
            }
        }


        private readonly HttpClient _httpClient;
        private string EndpointOllama = @$"http://localhost:{Config("Port")}/api/generate";
        private readonly string MainAiModel = Config("Model");

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

        }
        public async Task<string> AskQuestionToAi(int userId, string prompt, string ask, string? contextId, string? model, string? image)
        {
            switch (ask)
            {
                case "reformule":
                    return await SendToAi(GetModelPayload($"{AiPrompts.Reformule} : ${prompt}  [/INST]", userId, contextId, model, image));
                case "chat":
                    return await SendToAi(GetModelPayload($"${prompt}", userId, contextId, model, image));
                default:
                    return null;
            }
        }
        public async Task<string> AskDescriptionImageToAi(int userId, string image, string? contextId)
        {
            return await SendToAi(GetModelPayload($"{AiPrompts.DescriptionImage}", userId, contextId, image));
        }

        public async Task<string> AskAi(int userId, string? content, string ask, string? contextId)
        {
            if (string.IsNullOrWhiteSpace(contextId))
            {
                contextId = SampleUtils.GenerateUUID();
            }

            switch (ask)
            {
                case "tag":
                    return await SendToAi(GetModelPayload($"{AiPrompts.Tags} :\n {content} [/INST]", userId, contextId, MainAiModel));
                case "search":
                    return await SendToAi(GetModelPayload($"{AiPrompts.SearchPrompt} :\n {content}", userId, contextId, MainAiModel));
                case "convert":
                    return await SendToAi(GetModelPayload($"{AiPrompts.ConvertToMarkdownPrompt}  :\n{content}", userId, contextId, MainAiModel));
                case "Description":
                    return await SendToAi(GetModelPayload($"{AiPrompts.DescriptionDocumentFile}", userId, contextId, MainAiModel));
                default:
                    return null;
            }
        }

        public async Task<string> SendToAi(object payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(EndpointOllama, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaAnswer>(responseString);
            return CleanResponse(ollamaResponse?.Answer) ?? string.Empty;
        }

        public object GetModelPayload(string prompt, int userId, string contextId, string? model, string? image = null)
        {
            if (string.IsNullOrEmpty(model))
            {
                model = MainAiModel;
            }
            var context = userId.ToString() + contextId;
            int[] contextArray = context.Select(c => (int)c).ToArray();
            if (string.IsNullOrEmpty(image))
            {
                return new
                {
                    model = model,
                    prompt = prompt,
                    context = contextArray,
                    stream = false
                };
            }
            else
            {
                return new
                {
                    model = model,
                    prompt = prompt,
                    context = contextArray,
                    images = new[] { image },
                    stream = false
                };
            }
        }

        public string CleanResponse(string aiResponse)
        {
            // 1. Nettoyage des guillemets en trop
            aiResponse = aiResponse.Trim(' ', '"');

            // 2. Remplacement des séquences échappées
            aiResponse = aiResponse.Replace("\\\"", "\"");

            // 3. Suppression des retours à la ligne (échappés ou réels)
            aiResponse = aiResponse
                .Replace("\\n", " ")
                .Replace("\\r", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");

            // 4. Réduction des espaces multiples
            aiResponse = System.Text.RegularExpressions.Regex.Replace(aiResponse, @"\s{2,}", " ");

            return aiResponse.Trim();
        }
        public string GetAllModel()
        {
            return Config("AllModel");
        }
    }
}
