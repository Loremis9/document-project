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

		[HttpGet("create-user")]
		[Authorize]
		public async Task<OutputUserDto> CreateUser(string username, string password, string email)
		{
			var user = await userService.CreateUserAsync(username, email, password);
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

		[HttpGet("update-user")]
		[Authorize]
		public async Task<OutputUserDto> UpdateUser(string? username, string? password, string? email)
		{
			var user = userService.GetCurrentUserAsync();
			return await userService.UpdateUser(username, password, email, user.Id);

		}
		[HttpGet("delete-user")]
		[Authorize]
		public async Task<bool> DeleteUser(int userToDelete)
		{
			var user = userService.GetCurrentUserAsync();
			return await userService.DeleteUser(user.Id, userToDelete);
		}
	}

}