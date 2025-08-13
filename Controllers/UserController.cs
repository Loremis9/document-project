using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WEBAPI_m1IL_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.Utils;
using WEBAPI_m1IL_1.DTO;
namespace WEBAPI_m1IL_1.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private UserService userService;

		public UserController(UserService userService)
		{
			this.userService = userService;
		}

		[HttpPost("create-user")]
		[Authorize]
		public async Task<OutputUserDto> CreateUser(InputUserDto inputUserDto)
		{
			var user = await userService.CreateUserAsync(inputUserDto.Username, inputUserDto.EmailAddress, inputUserDto.Password);
			return new OutputUserDto { Id = user.Id, EmailAddress = user.EmailAddress, Username = user.Username };
		}

		[HttpGet("get-user")]
		[Authorize]
		public async Task<OutputUserDto> GetUser()
		{
			var userAuth = userService.GetCurrentUserAsync();
			var user = await userService.GetUserByIdAsync(userAuth.Id);
			return new OutputUserDto { Id = user.Id, EmailAddress = user.EmailAddress, Username = user.Username };
		}

		[HttpPatch("update-user")]
		[Authorize]
		public async Task<OutputUserDto> UpdateUser(InputUpdateUserDto inputUpdateUserDto)
		{
			var user = userService.GetCurrentUserAsync();
			return await userService.UpdateUser(inputUpdateUserDto.Username, inputUpdateUserDto.Password, inputUpdateUserDto.EmailAddress, user.Id);

		}
		[HttpDelete("delete-user")]
		[Authorize]
		public async Task<bool> DeleteUser(int userToDelete)
		{
			var user = userService.GetCurrentUserAsync();
			return await userService.DeleteUser(user.Id, userToDelete);
		}
	}
}