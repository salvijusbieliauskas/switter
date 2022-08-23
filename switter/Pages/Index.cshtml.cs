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
        private readonly switter.Data.switterContext _context;
        private readonly IUserStore<Areas.Identity.Data.switterUser> userStore;
        private readonly UserManager<Areas.Identity.Data.switterUser> _userManager;

        public IndexModel1(switter.Data.switterContext context, ILogger<IndexModel1> logger, IUserStore<Areas.Identity.Data.switterUser> userStore, UserManager<Areas.Identity.Data.switterUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
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
            public IFormFile Media { get; set; }
            public string AnotherInput { get; set; }

            [Required]
            [StringLength(280, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [DataType(DataType.Text)]
            [Display(Name = "Post text")]
            public string PostText { get; set; }
        }

        //[TempData]
        public string StatusMessage { get; set; }
        public List<string> supportedTypes = new List<string>() { "image/webp", "image/jpg", "image/jpeg", "image/png" };

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");


            var verificationStatus = TwitterAPI.VerifyTweet(Input.PostText);
            if (verificationStatus.success)
            {
                string mediaID = "";
                if(Input.Media != null && Input.Media.Length>0)
                {
                    if (!supportedTypes.Contains(Input.Media.ContentType))
                    {
                        StatusMessage = "Unsupported file type";
                        System.Diagnostics.Debug.WriteLine(Input.Media.ContentType);
                        return LocalRedirect("/");
                    }
                    //means that there is some media
                    using (var memoryStream = new MemoryStream())
                    {
                        await Input.Media.CopyToAsync(memoryStream);
                        if(memoryStream.Length>5000000)
                        {
                            StatusMessage = "Maximum image size is 5MB";
                            return LocalRedirect("/");
                        }
                        else
                        {
                            string id = TwitterAPI.UploadImage(memoryStream.ToArray());
                            if (id == "failed")
                            {
                                StatusMessage = "Image upload failed.";
                                return LocalRedirect("/");
                            }
                            else
                            {
                                mediaID = id;
                            }
                        }
                    }
                }
                var status = TwitterAPI.SendTweet(Input.PostText, mediaID);
                if (status.success)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        _context.post.Add(new Data.Post(status.ID,user.Id));
                        _context.SaveChanges();
                    }
                }
                else
                    StatusMessage = status.message;
            } 
            else
            {
                StatusMessage = verificationStatus.message;
            }
            return LocalRedirect("/");
        }
        public bool accepted = false;
        public IActionResult OnPostAccept(string data)
        {
            Response.Cookies.Append("terms", "true");
            return LocalRedirect("/");
        }
    }
}