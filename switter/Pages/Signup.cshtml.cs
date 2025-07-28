using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace switter.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class SignupModel : PageModel
{
    private readonly ILogger<SignupModel> _logger;

    public SignupModel(ILogger<SignupModel> logger)
    {
        _logger = logger;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; }

    [Required] [Display(Name = "Email")] public string Email { get; set; }

    [Required]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Required]
    [Display(Name = "VerifyPassword")]
    public string VerifyPassword { get; set; }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}