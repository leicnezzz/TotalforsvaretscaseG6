using Microsoft.AspNetCore.Authentication.Cookies;
using Totalforsvaret.Services;
using Microsoft.EntityFrameworkCore;
using Totalforsvaret.Models;
using Totalforsvaret.DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// identity framework ting
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddIdentityCore<Bruker>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<BrukerClaimsFactory>();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
  
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Headers.Accept.ToString().Contains("application/json"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Headers.Accept.ToString().Contains("application/json"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("KunPrivat", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("Brukertype", "Privat"));

    options.AddPolicy("KunOffentlig", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("Brukertype", "Offentlig"));
});

builder.Services.AddScoped<Totalforsvaret.Handlers.ResourceHandler>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 4, 0))));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    await TestUserSeeder.SeedAsync(scope.ServiceProvider);
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// Bevar gamle innganger, inkludert POST og valideringsfeil på ressursskjemaet.
app.MapControllerRoute(
    name: "legacy-resource",
    pattern: "Home/Skjema",
    defaults: new { area = "Privat", controller = "Resource", action = "Skjema" });

app.MapGet("/OffentligAktor/Behov", () => Results.LocalRedirect("~/Behov"));

app.MapControllerRoute(
    name: "behov",
    pattern: "Behov",
    defaults: new { area = "Offentlig", controller = "Need", action = "Create" });


app.MapControllerRoute(
    name: "legacy-need",
    pattern: "Need/{action=Create}/{id?}",
    defaults: new { area = "Offentlig", controller = "Need" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.MapControllerRoute(
    name: "offentlig-match",
    pattern: "OffentligAktor/match",
    defaults: new { controller = "OffentligAktor", action = "Index", match = true });

app.MapControllerRoute(
    name: "ressursregistrering",
    pattern: "Ressurs",
    defaults: new { controller = "Home", action = "ResourceRegistration" });


app.Run();
