// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using switter.Areas.Identity.Data;

namespace switter.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly IUserEmailStore<SwitterUser> _emailStore;
    private readonly IEmailSender _sender;
    private readonly UserManager<SwitterUser> _userManager;
    private readonly IUserStore<SwitterUser> _userStore;

    public RegisterConfirmationModel(UserManager<SwitterUser> userManager, IEmailSender sender,
        IUserStore<SwitterUser> userStore)
    {
        _userManager = userManager;
        _sender = sender;
        _userStore = userStore;
        _emailStore = GetEmailStore();
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool DisplayConfirmAccountLink { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
    {
        if (email == null) return RedirectToPage("/Index");
        returnUrl = returnUrl ?? Url.Content("~/");

        //var user = await _userManager.FindByEmailAsync(email);
        var source = new CancellationTokenSource();
        var token = source.Token;
        var user = await _emailStore.FindByEmailAsync(email, token);
        if (user == null) return NotFound($"Unable to load user with email '{email}'.");

        Email = email;
        // Once you add a real email sender, you should remove this code that lets you confirm the account
        DisplayConfirmAccountLink = false;
        if (DisplayConfirmAccountLink)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            EmailConfirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                null,
                new { area = "Identity", userId, code, returnUrl },
                Request.Scheme);
        }

        return Page();
    }

    private IUserEmailStore<SwitterUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
            throw new NotSupportedException("The default UI requires a user store with email support.");
        return (IUserEmailStore<SwitterUser>)_userStore;
    }
}