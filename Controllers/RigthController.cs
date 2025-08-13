using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.DTO;
namespace WEBAPI_m1IL_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RightController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private RigthAccessService rigthAccessService;
        private UserService userService;
        public RightController(IConfiguration configuration, RigthAccessService rigthAccessService, UserService userService)
        {
            _configuration = configuration;
            this.rigthAccessService = rigthAccessService;
            this.userService = userService;
        }

        [HttpGet("Get-Rigth")]
        [Authorize]
        public async Task<ICollection<UserPermission>> GetRigth()
        {
            var user = userService.GetCurrentUserAsync();
            var permissions = await rigthAccessService.GetAllUserPermission(user.Id);
            return permissions;
        }


        [HttpPost("Update-Rigth")]
        [Authorize]
        public async Task<UserPermission> UpdateRigth(InputRigth inputRigh)
        {
            var user = userService.GetCurrentUserAsync();
            var permissions = await rigthAccessService.ChangeUserPermisionByOtherUser(user.Id, inputRigh.DocumentationId, inputRigh.Read,
                inputRigh.Write, inputRigh.Delete, inputRigh.Admin, inputRigh.UserToChange);
            return permissions;
        }


        [HttpPost("Delete-Rigth")]
        [Authorize]
        public async Task<bool> DeleteRigth(int documentationId, int UserToChange)
        {
            var user = userService.GetCurrentUserAsync();
            var permissions = await rigthAccessService.DeletePermission(user.Id, documentationId, UserToChange);
            return permissions;
        }
    }
}
