using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using switter.Areas.Identity.Data;
using switter.Data;

namespace switter.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class LeaderboardModel : PageModel
{
    private readonly SwitterContext _context;

    private readonly ILogger<LeaderboardModel> _logger;
    private readonly UserManager<SwitterUser> _userManager;
    public IList<LeaderboardEntry> CompletedEntries;

    public LeaderboardModel(ILogger<LeaderboardModel> logger, SwitterContext context,
        UserManager<SwitterUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public async void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        CompletedEntries = TwitterApi.CompletedEntries;
    }
}

public class LeaderboardEntry
{
    public string Name;
    public string Rank;
    public string Score;
    public string UserId;

    public LeaderboardEntry(int points, string name, int index, string id)
    {
        Score = points.ToString();
        Name = name;
        Rank = index.ToString();
        UserId = id;
    }
}