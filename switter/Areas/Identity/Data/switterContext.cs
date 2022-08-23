using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using switter.Areas.Identity.Data;

namespace switter.Data;

public class switterContext : IdentityDbContext<switterUser>
{
    public switterContext(DbContextOptions<switterContext> options)
        : base(options)
    {
    }
    public DbSet<Post> post { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>().ToTable("Post");
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
public class Post
{
    public string ID { get; set; }
    public string PosterID { get; set; }
    public Post(string iD, string posterID)
    {
        ID = iD;
        PosterID = posterID;
    }
}