using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AbraqAccount.Controllers;

[Route("account")]
public class LoginController : Controller
{
    private readonly IAccountService _accountService;

    public LoginController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
    {
        var user = await _accountService.AuthenticateUserAsync(username, password);

        if (user == null)
        {
            return Redirect("/login?error=Invalid credentials");
        }

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);

        return Redirect("/dashboard");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Redirect("/login");
    }
}
