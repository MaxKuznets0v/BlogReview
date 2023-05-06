using BlogReview.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BlogReview.Models;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using BlogReview.Controllers;
using BlogReview.Services;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc.Razor;

var builder = WebApplication.CreateBuilder(args);
var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
var connectionString = configuration.GetConnectionString("DefaultConnection");

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization();
builder.Services.AddDbContext<ArticleContext>(options =>
    options.UseLazyLoadingProxies()
    //.UseMySQL(connectionString));
    .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31))));
    //.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<User>(options =>  
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ArticleContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});
builder.Services.AddSignalR();

builder.Services.AddAuthentication()
.AddLinkedIn(options =>
{
    IConfigurationSection linkedinAuthNSection =
        builder.Configuration.GetSection("Authentication:Linkedin");
    options.ClientId = linkedinAuthNSection["ClientId"];
    options.ClientSecret = linkedinAuthNSection["ClientSecret"];
    options.SignInScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(options =>
{
    IConfigurationSection googleAuthNSection =
    builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleAuthNSection["ClientId"];
    options.ClientSecret = googleAuthNSection["ClientSecret"];
    options.SignInScheme = IdentityConstants.ExternalScheme;
});
builder.Services.AddScoped(provider =>
{
    IConfigurationSection config =
    builder.Configuration.GetSection("ImageCloud:Cloudinary");
    return new ImageStorageService(new Account(config["CloudName"], config["Key"], config["Secret"]));
});
var app = builder.Build();
app.UseCookiePolicy(new CookiePolicyOptions()
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});
using var scope = app.Services.CreateScope();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

if (!await roleManager.RoleExistsAsync("MasterAdmin"))
{
    var role = new IdentityRole<Guid> { Name = "MasterAdmin" };
    var result = await roleManager.CreateAsync(role);
    if (!result.Succeeded)
    {
        throw new Exception("Failed to create the MasterAdmin role.");
    }
}
if (!await roleManager.RoleExistsAsync("Admin"))
{
    var adminRole = new IdentityRole<Guid> { Name = "Admin" };
    var result = await roleManager.CreateAsync(adminRole);
    if (!result.Succeeded)
    {
        throw new Exception("Failed to create the Admin role.");
    } 
    else
    {
        var admins = builder.Configuration.GetSection("DefaultAdmins");
        foreach (var admin in admins.GetChildren())
        {
            User userAdmin = new() { UserName = admin["UserName"], Email = admin["Email"] };
            var adminRes = await userManager.CreateAsync(userAdmin);

            if (adminRes.Succeeded)
            {
                await userManager.AddToRolesAsync(userAdmin, new List<string>() { "Admin", "MasterAdmin" });
            }
            else
            {
                throw new Exception("Failed to add user as an Admin.");
            }
        }
    }
}

if (!await roleManager.RoleExistsAsync("User"))
{
    var role = new IdentityRole<Guid> { Name = "User" };
    var result = await roleManager.CreateAsync(role);
    if (!result.Succeeded)
    {
        throw new Exception("Failed to create the User role.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("ru")
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new[] { new CookieRequestCultureProvider() }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CommentsHub>("/comment");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Feed}/{action=Index}/{id?}");

app.Run();
