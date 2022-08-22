using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace switter.Pages
{
    public class IndexModel1 : PageModel
    {
        private readonly ILogger<IndexModel1> _logger;

        public IndexModel1(ILogger<IndexModel1> logger)
        {
            _logger = logger;
        }
        public string ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(280, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [DataType(DataType.Text)]
            [Display(Name = "Post text")]
            public string PostText { get; set; }
        }

        //[TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (TwitterAPI.VerifyTweet(Input.PostText) == true)
            {
                var status = TwitterAPI.SendTweet(Input.PostText);
                if (status.success)
                    StatusMessage = "Tweet sent!";
                else
                    StatusMessage = status.message;
            }
            else
            {
                StatusMessage = "Your tweet was invalid.";
            }
            return Page();
        }
        public bool accepted = false;
        public IActionResult OnPostAccept(string data)
        {
            Response.Cookies.Append("terms", "true");
            return LocalRedirect("/");
        }
    }
}