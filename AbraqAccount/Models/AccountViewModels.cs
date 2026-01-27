using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? Company { get; set; }
    public string? Year { get; set; }
    public bool RememberMe { get; set; }
}

