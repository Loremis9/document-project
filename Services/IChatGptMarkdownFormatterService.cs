namespace WEBAPI_m1IL_1.Services
{
    public interface IChatGptMarkdownFormatterService
    {
        Task<string> FormatAsMarkdownAsync(string plainText);
    }
}
