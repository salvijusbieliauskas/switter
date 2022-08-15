using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace switter.Pages
{
    public class PostModel : PageModel
    {
        private readonly ILogger<PostModel> _logger;

        public PostModel(ILogger<PostModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}