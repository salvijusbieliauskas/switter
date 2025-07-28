// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using switter.Areas.Identity.Data;

namespace switter.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly SignInManager<SwitterUser> _signInManager;
    private readonly UserManager<SwitterUser> _userManager;

    public IndexModel(
        UserManager<SwitterUser> userManager,
        SignInManager<SwitterUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Username { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    private async Task LoadAsync(SwitterUser user)
    {
        var userName = await _userManager.GetUserNameAsync(user);

        Username = userName;

        Input = new InputModel
        {
            Username = userName
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var userName = user.UserName;

        var emailUser = await _userManager.FindByNameAsync(Input.Username);
        if (emailUser != null)
        {
            StatusMessage = "This username is already taken";
            return RedirectToPage();
        }

        if (Input.Username != userName)
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Username);
            if (!setUserNameResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to set username.";
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    public class InputModel
    {
        [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 3)]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string Username { get; set; }
    }
}