using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace switter.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class LeaderboardModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<LeaderboardModel> _logger;
        private readonly switter.Data.switterContext _context;
        public IList<LeaderboardEntry> completedEntries; 
        private readonly UserManager<Areas.Identity.Data.switterUser> _userManager;
        public LeaderboardModel(ILogger<LeaderboardModel> logger, Data.switterContext context, UserManager<Areas.Identity.Data.switterUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            completedEntries = TwitterAPI.completedEntries;
        }
    }
    public class LeaderboardEntry
    {
        public string Score;
        public string Name;
        public string Rank;
        public string userID;
        public LeaderboardEntry(int points, string name, int index, string id)
        {
            Score = points.ToString();
            Name = name;
            this.Rank = index.ToString();
            this.userID = id;
        }
    }
}