namespace switter
{
    public class Post
    {
        public string Contents { get; set; }
        public DateTime Time { get; set; }
        public string PosterID { get; set; }
        public string PostID { get; set; }
        public Post(string contents, DateTime time, string posterID, string postID)
        {
            Contents = contents;
            Time = time;
            PosterID = posterID;
            PostID = postID;
        }
    }
}
