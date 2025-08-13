using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.Helpers;
using NPOI.XSSF.UserModel.Helpers;
namespace WEBAPI_m1IL_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private IConfiguration _configuration;
        private UserService userService;
        public LoginController(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            this.userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLogin userLogin)
        {
            Console.WriteLine($"Tentative de connexion pour: {userLogin.UserEmail}");

            var isOk = await Authenticate(userLogin);

            if (isOk != -1)
            {
                //générer jeton JWT
                string token = Generate(isOk);
                Console.WriteLine($"Token généré pour l'utilisateur ID: {isOk}");
                return Ok(token);
            }
            else
            {
                Console.WriteLine($"Échec de l'authentification pour: {userLogin.UserEmail}");
                return Unauthorized("Bad credentials");
            }
        }

        [HttpPost("test")]
        public IActionResult TestAuth()
        {
            return Ok(new { message = "Test endpoint accessible", timestamp = DateTime.Now });
        }

        private string Generate(int userId)
        {
            var secret = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var security = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(security, SecurityAlgorithms.HmacSha256);

            //Choisir les informations à mettre dans le token (claims)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            };

            //Générer le token 
            var token = new JwtSecurityToken(
                      issuer: issuer,
                      audience: audience,
                      claims: claims,
                      expires: DateTime.Now.AddMinutes(30),
                      signingCredentials: credentials
                );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenString;
        }

        private async Task<int> Authenticate(UserLogin userLogin)
        {
            if (userLogin == null || string.IsNullOrWhiteSpace(userLogin.UserEmail) || string.IsNullOrWhiteSpace(userLogin.Password))
                return -1; // Données invalides

            var user = await userService.GetUserByMailAsync(userLogin.UserEmail);
            if (user == null)
                return -1; // Utilisateur introuvable

            // 🔐 Comparaison sécurisée si tu stockes un hash
            if (!PasswordHelper.VerifyPassword(userLogin.Password, user.Password))
                return -1; // Mot de passe incorrect

            return user.Id; // Authentification réussie
        }
    }
}