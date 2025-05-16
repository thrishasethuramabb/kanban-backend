using kanbanBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace kanbanBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LabbelMainContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(LabbelMainContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Retrieve the username from the JWT claims.
            var username = User?.Claims.FirstOrDefault(c =>
     c.Type == ClaimTypes.Name ||
     c.Type == "sub" ||
     c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("No user found in token.");
            }


            var employee = await _context.TblEmployee.FirstOrDefaultAsync(e => e.StrUsername == username);
            if (employee == null)
                return NotFound("User not found.");

            return Ok(new
            {
                id = employee.IntEmployeeId,
                name = $"{employee.StrEmployeeFirstName} {employee.StrEmployeeLastName}",
                role = employee.StrRole
            });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Invalid credentials");

            var hashedPassword = HashSHA1(request.Password);

            var employee = await _context.TblEmployee
                .FirstOrDefaultAsync(e =>
                    e.StrUsername == request.Username &&
                    e.StrPassword == hashedPassword &&
                    e.BitIsActive);

            if (employee == null)
                return Unauthorized("Username or password incorrect");
            Console.WriteLine($"Extracted Username: {request.Username}");
            if (employee == null)
            {
                Console.WriteLine("Employee record not found for this username.");
            }


            var token = GenerateJwtToken(employee);

            return Ok(new
            {
                user = new
                {
                    employee.IntEmployeeId,
                    employee.StrEmployeeFirstName,
                    employee.StrEmployeeLastName,
                    employee.StrUsername,
                    employee.StrRole
                },
                token
            });
        }

        private string HashSHA1(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }
        }

        private string GenerateJwtToken(Employee employee)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, employee.StrUsername),
                new Claim("role", employee.StrRole),
                new Claim("employeeId", employee.IntEmployeeId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
