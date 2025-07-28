using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using switter.Areas.Identity.Data;
using switter.Data;

namespace switter.Pages;

public class IndexModel1 : PageModel
{
    private static readonly TimeSpan CooldownTime = new(0, 10, 0);
    private readonly SwitterContext _context;
    private readonly ILogger<IndexModel1> _logger;
    private readonly UserManager<SwitterUser> _userManager;
    private readonly IUserStore<SwitterUser> _userStore;
    public bool Accepted = false;
    public List<string> SupportedTypes = new() { "image/webp", "image/jpg", "image/jpeg", "image/png" };

    public IndexModel1(SwitterContext context, ILogger<IndexModel1> logger, IUserStore<SwitterUser> userStore,
        UserManager<SwitterUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public string ReturnUrl { get; set; }

    [BindProperty] public InputModel Input { get; set; }

    //[TempData]
    public string StatusMessage { get; set; }
    public string HeaderText { get; set; }

    public string GetIp()
    {
        return Request.HttpContext.Connection.RemoteIpAddress.ToString();
    } //

    public async void OnGet()
    {
        HeaderText =
            "instead of refreshing, click on \"switter\" or \"Garbage\" to avoid posting a duplicate. atsitiktinis katinelio faktas: " +
            TwitterApi.GetCatFact();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        //check cooldowns
        Debug.WriteLine(TwitterApi.Cooldowns.Count);
        for (var x = 0; x < TwitterApi.Cooldowns.Count; x++)
        {
            if (TwitterApi.Cooldowns[x].Ip.Equals(GetIp()))
            {
                var difference = DateTime.Now.Subtract(TwitterApi.Cooldowns[x].PostTime);
                if (difference >= CooldownTime)
                {
                    TwitterApi.Cooldowns.RemoveAt(x);
                    break;
                }

                var somethiung = CooldownTime - difference;
                StatusMessage = "You have to wait " + somethiung.Minutes + " minutes and " + somethiung.Seconds +
                                " seconds until you can post again";
                Input.Media = null;
                Input.PostText = null;
                return Page();
            }

            if (DateTime.Now.Subtract(TwitterApi.Cooldowns[x].PostTime) >= CooldownTime)
                TwitterApi.Cooldowns.RemoveAt(x);
        }


        var verificationStatus = TwitterApi.VerifyTweet(Input.PostText);
        if (verificationStatus.Success)
        {
            var mediaId = "";
            if (Input.Media != null && Input.Media.Length > 0)
            {
                if (!SupportedTypes.Contains(Input.Media.ContentType))
                {
                    StatusMessage = "Unsupported file type";
                    Debug.WriteLine(Input.Media.ContentType);
                    Input.Media = null;
                    Input.PostText = null;
                    return Page();
                }

                //means that there is some media
                using (var memoryStream = new MemoryStream())
                {
                    await Input.Media.CopyToAsync(memoryStream);
                    if (memoryStream.Length > 5000000 && !Input.Media.ContentType.Equals("image/gif"))
                    {
                        StatusMessage = "Maximum image size is 5MB";
                        Input.Media = null;
                        Input.PostText = null;
                        return Page();
                    }

                    if (memoryStream.Length > 15000000 && Input.Media.ContentType.Equals("images/gif"))
                    {
                        StatusMessage = "Maximum gif size is 15MB";
                        Input.Media = null;
                        Input.PostText = null;
                        return Page();
                    }

                    var id = TwitterApi.UploadImage(memoryStream.ToArray());
                    if (id == "failed")
                    {
                        StatusMessage = "Image upload failed.";
                        Input.Media = null;
                        Input.PostText = null;
                        return Page();
                    }

                    mediaId = id;
                }
            }

            var status = TwitterApi.SendTweet(Input.PostText, mediaId);
            if (status.Success)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    _context.Post.Add(new Post(status.Id, user.Id));
                    _context.SaveChanges();
                }

                TwitterApi.Cooldowns.Add(new Cooldown(DateTime.Now, GetIp()));
                StatusMessage = "Tweet sent!";
            }
            else
            {
                StatusMessage = status.Message;
            }
        }
        else
        {
            StatusMessage = verificationStatus.Message;
        }

        Input.Media = null;
        Input.PostText = null;
        return Page();
    }

    public IActionResult OnPostAccept(string data)
    {
        Response.Cookies.Append("terms", "true");
        return LocalRedirect("/");
    }

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
}

public class Cooldown
{
    public string Ip;
    public DateTime PostTime;

    public Cooldown(DateTime post, string ip)
    {
        this.Ip = ip;
        PostTime = post;
    }
}