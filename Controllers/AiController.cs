using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.DTO;
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

        [HttpPost("ChatWithAi")]
        [Authorize]
        public async Task<string> ChatToAI(InputChat inputChat)
        {
            var user = await userService.GetCurrentUserAsync();
            return await _aiService.AskQuestionToAi(user.Id, inputChat.Prompt, "chat", inputChat.Context, inputChat.Model, inputChat.Image);
        }

        [HttpGet("GetAllModel")]
        public async Task<string> GetAllModel()
        {
            return  _aiService.GetAllModel();
        }
    }

    }