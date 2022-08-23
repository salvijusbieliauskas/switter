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
        public IList<switter.Data.Post> post { get; set; }
        public LeaderboardModel(ILogger<LeaderboardModel> logger, Data.switterContext context)
        {
            _logger = logger;
            _context = context;
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
                request.Add(postlist[x].ID);
                if(x%99==0||x==postlist.Count-1)
                {
                    requests.Add(request);
                    request.Clear();
                }
            }
            List<int> likes = new List<int>();
            foreach(List<string> str in requests)
            {
                TwitterAPI.GetTweets(str);
            }

            post = _context.post.ToList();
        }
    }
    public class LeaderboardEntry
    {
        public string ID;
        public string Name;
        public string index;
        public LeaderboardEntry(string iD, string name, int index)
        {
            ID = iD;
            Name = name;
            this.index = index.ToString();
        }
    }
}