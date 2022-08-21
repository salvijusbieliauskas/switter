using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace switter.Pages
{
    public class IndexModel1 : PageModel
    {
        private readonly ILogger<IndexModel1> _logger;

        public IndexModel1(ILogger<IndexModel1> logger)
        {
            _logger = logger;
        }
        public void OnGet()
        {
            string accepted = string.Empty;
            var terms = Request.Cookies["terms"];
            if (terms != null)
            {
                accepted = terms.ToString();
                if (accepted == "false")
                {
                    visibility = "initial";
                    othervisibility = "none";
                }
                else
                {
                    visibility = "none";
                    othervisibility = "initial";
                }
            }
            else
            {
                visibility = "initial";
                othervisibility = "none";
            }
        }
        public string visibility { get; set; }
        public string othervisibility { get; set; }
        public IActionResult OnPostAccept(string data)
        {
            visibility = "none";
            othervisibility = "initial";
            Response.Cookies.Append("terms", "true");
            return Page();
        }
        public IActionResult OnPostText(string data)
        {
            visibility = "none";
            othervisibility = "initial";
            return Page();
        }
    }
}