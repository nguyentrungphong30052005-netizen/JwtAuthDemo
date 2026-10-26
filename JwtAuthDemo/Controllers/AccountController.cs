using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        string role = "";
        if (username == "user1" && password == "123") role = "user";
        else if (username == "admin1" && password == "123") role = "admin";

        if (string.IsNullOrEmpty(role))
        {
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        // Tạo JWT token chứa role
        var token = GenerateJwtToken(username, role);

        // Lưu JWT vào HttpOnly Cookie
        Response.Cookies.Append("JWTCookie", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return RedirectToAction("Dashboard");
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        ViewBag.Username = User.FindFirst(ClaimTypes.Name)?.Value;
        ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;
        return View();
    }

    [Authorize(Roles = "admin")]
    public IActionResult AdminManage()
    {
        ViewBag.Username = User.FindFirst(ClaimTypes.Name)?.Value;
        return View();
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("JWTCookie");
        return RedirectToAction("Login");
    }

    private string GenerateJwtToken(string username, string role)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "super_secret_key_at_least_32_bytes_long");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}