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
        private static readonly TimeSpan cooldownTime = new TimeSpan(0, 3, 0);
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

            [StringLength(280, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [DataType(DataType.Text)]
            [Display(Name = "Post text")]
            public string PostText { get; set; }
        }
        public string GetIp()
        {
            return Request.HttpContext.Connection.RemoteIpAddress.ToString();
        }//

        public async void OnGet()
        {
            HeaderText = "instead of refreshing, click on \"whatobama\" or \"Garbage\" to avoid posting a duplicate. atsitiktinis katinelio faktas: " + TwitterAPI.getCatFact();
        }
        //[TempData]
        public string StatusMessage { get; set; }
        public List<string> supportedTypes = new List<string>() { "image/webp", "image/jpg", "image/jpeg", "image/png" };
        public string HeaderText { get; set; }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            //check cooldowns
            System.Diagnostics.Debug.WriteLine(TwitterAPI.cooldowns.Count);
            for(int x = 0; x < TwitterAPI.cooldowns.Count;x++)
            {
                if (TwitterAPI.cooldowns[x].ip.Equals(GetIp()))
                {
                    TimeSpan difference = DateTime.Now.Subtract(TwitterAPI.cooldowns[x].postTime);
                    if (difference >= cooldownTime)
                    {
                        TwitterAPI.cooldowns.RemoveAt(x);
                        break;
                    }
                    else
                    {
                        var somethiung = cooldownTime - difference;
                        StatusMessage = "You have to wait "+somethiung.Minutes+" minutes and "+somethiung.Seconds+" seconds until you can post again";
                        Input.Media = null;
                        Input.PostText = null;
                        return Page();
                    }
                }
                else if (DateTime.Now.Subtract(TwitterAPI.cooldowns[x].postTime) >= cooldownTime)
                {
                    TwitterAPI.cooldowns.RemoveAt(x);
                }
            }
            //

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
                        Input.Media = null;
                        Input.PostText = null;
                        return Page();
                    }
                    //means that there is some media
                    using (var memoryStream = new MemoryStream())
                    {
                        await Input.Media.CopyToAsync(memoryStream);
                        if(memoryStream.Length>5000000)
                        {
                            StatusMessage = "Maximum image size is 5MB";
                            Input.Media = null;
                            Input.PostText = null;
                            return Page();
                        }
                        else
                        {
                            string id = TwitterAPI.UploadImage(memoryStream.ToArray());
                            if (id == "failed")
                            {
                                StatusMessage = "Image upload failed.";
                                Input.Media = null;
                                Input.PostText = null;
                                return Page();
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
                    TwitterAPI.cooldowns.Add(new Cooldown(DateTime.Now, GetIp()));
                    StatusMessage = "Tweet sent!";
                }
                else
                    StatusMessage = status.message;
            } 
            else
            {
                StatusMessage = verificationStatus.message;
            }
            Input.Media = null;
            Input.PostText = null;
            return Page();
        }
        public bool accepted = false;
        public IActionResult OnPostAccept(string data)
        {
            Response.Cookies.Append("terms", "true");
            return LocalRedirect("/");
        }

    }
    public class Cooldown
    {
        public string ip;
        public DateTime postTime;
        public Cooldown(DateTime post, string ip)
        {
            this.ip = ip;
            this.postTime = post;
        }
    }
}