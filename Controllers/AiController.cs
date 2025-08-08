using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WEBAPI_m1IL_1.Services;
namespace WEBAPI_m1IL_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AiController : ControllerBase
    {
        private AIService _aiService;
        private UserService userService;
        public AiController(AIService aiService, UserService userService)
        {
            _aiService = aiService;
            this.userService = userService;
        }

        [HttpGet("ChatWithAi")]
        [Authorize]
        public async Task<string> ChatToAI(string prompt, string? image, string context, string model)
        {
            var user = await userService.GetCurrentUserAsync();
            return await _aiService.AskQuestionToAi(user.Id, prompt, "chat", context, model, image);
        }

        [HttpGet("GetAllModel")]
        public async Task<string> GetAllModel(string prompt, string? image, string context, string model)
        {
            return  _aiService.GetAllModel();
        }
    }

    }