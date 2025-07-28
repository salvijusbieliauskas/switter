using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using switter.Areas.Identity.Data;

namespace switter.Data;

public class SwitterContext : IdentityDbContext<SwitterUser>
{
    public SwitterContext(DbContextOptions<SwitterContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Post { get; set; }

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
    public Post(string iD, string posterId)
    {
        Id = iD;
        PosterId = posterId;
    }

    public string Id { get; set; }
    public string PosterId { get; set; }
}