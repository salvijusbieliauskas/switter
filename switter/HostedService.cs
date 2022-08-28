using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using switter;
using Microsoft.AspNetCore.Identity.UI.Services;
using switter.Data;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace switter
{
    public class HostedService : IHostedService
    {
        Timer t;
        private readonly IServiceScopeFactory scopeFactory;
        public HostedService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            t = new Timer(
                UpdateEntries,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(5)
            );

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            t?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void UpdateEntries(object state)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<Data.switterContext>();
                List<List<string>> requests = new List<List<string>>();
                var postlist = _context.post.ToList();
                List<string> request = new List<string>();
                for (int x = 0; x < postlist.Count; x++)
                {
                    request.Add(postlist[x].ID);
                    if (x + 1 % 99 == 0 || x == postlist.Count - 1)
                    {
                        requests.Add(request.GetRange(0, request.Count));
                        request.Clear();
                    }
                }
                List<Tweet2> likes = new List<Tweet2>();
                foreach (List<string> str in requests)
                {
                    var tweets = TwitterAPI.GetTweets(str);
                    if (tweets == null)
                    {
                        continue;
                    }
                    likes.AddRange(tweets);
                }
                if (likes.Count == 0)
                    return;
                List<Pages.LeaderboardEntry> entries = new List<Pages.LeaderboardEntry>();
                foreach (var user in _context.Users)
                {
                    entries.Add(new Pages.LeaderboardEntry(0, user.UserName, 0, user.Id));
                }
                //go through every post, finding how many likes it got and the leaderboardentry of the poster
                foreach (var post in _context.post.ToList())
                {
                    int tweetIndex = findTweetByID(likes, post.ID);
                    int entryIndex = findEntryByID(entries, post.PosterID);
                    try
                    {
                        entries[entryIndex].Score = (int.Parse(entries[entryIndex].Score) + likes[tweetIndex].likes).ToString();
                    }
                    catch { }
                }
                //sort entries
                entries = entries.OrderByDescending(o => int.Parse(o.Score)).ToList();
                //add indexes
                for (int x = 0; x < 10; x++)
                {
                    entries[x].Rank = (x + 1).ToString();
                    if (x == entries.Count - 1)
                        break;
                }
                TwitterAPI.completedEntries = entries.GetRange(0, entries.Count > 10 ? 10 : entries.Count);
            }
        }
        private int findEntryByID(List<Pages.LeaderboardEntry> entries, string id)
        {
            for (int x = 0; x < entries.Count; x++)
            {
                if (entries[x].userID == id)
                    return x;
            }
            return -1;
        }
        private int findTweetByID(List<Tweet2> entries, string id)
        {
            for (int x = 0; x < entries.Count; x++)
            {
                if (entries[x].ID == id)
                    return x;
            }
            return -1;
        }
    }
}
