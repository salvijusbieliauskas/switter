using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using switter;
using switter.Areas.Identity.Data;
using switter.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration["swimter"];
builder.Services.AddDbContext<SwitterContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<SwitterUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<SwitterContext>();
builder.Services.AddRazorPages();
builder.Services.AddHostedService<HostedService>();
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);
TwitterApi.Init(builder.Configuration["ConsumerKey"], builder.Configuration["ConsumerSecret"],
    builder.Configuration["AccessToken"], builder.Configuration["AccessSecret"], builder.Configuration["BearerToken"]);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();