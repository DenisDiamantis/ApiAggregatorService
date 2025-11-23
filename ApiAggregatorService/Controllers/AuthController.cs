using ApiAggregatorService.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiAggregatorService.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{

		private readonly IConfiguration _config;

		public AuthController (IConfiguration config)
		{
			_config = config;
		}

		[HttpPost("login")]
		public IActionResult Login([FromBody] LoginRequest request)
		{
			if (request.Username != "testuser" || request.Password != "password")
				return Unauthorized("Invalid credentials");

			var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
			var issuer = _config["Jwt:Issuer"];
			var audience = _config["Jwt:Audience"];
			var signingKey = new SymmetricSecurityKey(keyBytes);

			var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);


			var token = new JwtSecurityToken(
				claims: new[] { new Claim(ClaimTypes.Name, request.Username) },
				expires: DateTime.UtcNow.AddHours(2),
				issuer: issuer,
				audience: audience,
				signingCredentials: creds
			);

			return Ok(new LoginResponse
			{
				Token = new JwtSecurityTokenHandler().WriteToken(token)
			});
		}
	}
}
