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
            //separate tweets into 100
            List<List<string>> requests = new List<List<string>>();
            var postlist = _context.post.ToList();
            List<string> request = new List<string>();
            for (int x = 0; x < postlist.Count;x++)
            {
                Debug.WriteLine(postlist[x].ID);
                request.Add(postlist[x].ID);
                if(x+1%99==0||x==postlist.Count-1)
                {
                    requests.Add(request.GetRange(0,request.Count));
                    request.Clear();
                }
            }
            List<Tweet2> likes = new List<Tweet2>();
            foreach(List<string> str in requests)
            {
                var tweets = TwitterAPI.GetTweets(str);
                if (tweets==null)
                {
                    continue;
                }
                likes.AddRange(tweets);
            }
            if (likes.Count == 0)
                return;
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            foreach(var user in _userManager.Users)
            {
                entries.Add(new LeaderboardEntry(0, user.UserName, 0, user.Id));
            }
            //go through every post, finding how many likes it got and the leaderboardentry of the poster
            foreach(var post in _context.post.ToList())
            {
                int tweetIndex = findTweetByID(likes, post.ID);
                int entryIndex = findEntryByID(entries, post.PosterID);
                entries[entryIndex].Score = (int.Parse(entries[entryIndex].Score) + likes[tweetIndex].likes).ToString();
            }
            //sort entries
            entries = entries.OrderByDescending(o => o.Score).ToList();
            //add indexes
            for(int x = 0; x < 10;x++)
            {
                entries[x].Rank = (x + 1).ToString();
                if (x == entries.Count - 1)
                    break;
            }
            completedEntries = entries.GetRange(0, entries.Count>10?10:entries.Count);
        }
        public int findEntryByID(List<LeaderboardEntry> entries,string id)
        {
            for(int x = 0; x < entries.Count;x++)
            {
                if (entries[x].userID == id)
                    return x;
            }
            return -1;
        }
        public int findTweetByID(List<Tweet2> entries, string id)
        {
            for (int x = 0; x < entries.Count; x++)
            {
                if (entries[x].ID == id)
                    return x;
            }
            return -1;
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