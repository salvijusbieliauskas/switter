using switter.Data;
using switter.Pages;

namespace switter;

public class HostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer _t;

    public HostedService(IServiceScopeFactory scopeFactory)
    {
        this._scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _t = new Timer(
            UpdateEntries,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _t?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private void UpdateEntries(object state)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SwitterContext>();
            var requests = new List<List<string>>();
            var postlist = context.Post.ToList();
            var request = new List<string>();
            for (var x = 0; x < postlist.Count; x++)
            {
                request.Add(postlist[x].Id);
                if (x + 1 % 99 == 0 || x == postlist.Count - 1)
                {
                    requests.Add(request.GetRange(0, request.Count));
                    request.Clear();
                }
            }

            var likes = new List<Tweet2>();
            foreach (var str in requests)
            {
                var tweets = TwitterApi.GetTweets(str);
                if (tweets == null) continue;
                likes.AddRange(tweets);
            }

            if (likes.Count == 0)
                return;
            var entries = new List<LeaderboardEntry>();
            foreach (var user in context.Users) entries.Add(new LeaderboardEntry(0, user.UserName, 0, user.Id));
            //go through every post, finding how many likes it got and the leaderboardentry of the poster
            foreach (var post in context.Post.ToList())
            {
                var tweetIndex = FindTweetById(likes, post.Id);
                var entryIndex = FindEntryById(entries, post.PosterId);
                try
                {
                    entries[entryIndex].Score =
                        (int.Parse(entries[entryIndex].Score) + likes[tweetIndex].Likes).ToString();
                }
                catch
                {
                }
            }

            //sort entries
            entries = entries.OrderByDescending(o => int.Parse(o.Score)).ToList();
            //add indexes
            for (var x = 0; x < 10; x++)
            {
                entries[x].Rank = (x + 1).ToString();
                if (x == entries.Count - 1)
                    break;
            }

            TwitterApi.CompletedEntries = entries.GetRange(0, entries.Count > 10 ? 10 : entries.Count);
        }
    }

    private int FindEntryById(List<LeaderboardEntry> entries, string id)
    {
        for (var x = 0; x < entries.Count; x++)
            if (entries[x].UserId == id)
                return x;
        return -1;
    }

    private int FindTweetById(List<Tweet2> entries, string id)
    {
        for (var x = 0; x < entries.Count; x++)
            if (entries[x].Id == id)
                return x;
        return -1;
    }
}