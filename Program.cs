using Lecturio.Configuration;
using Lecturio.Data;
using Lecturio.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<SupabaseOptions>(
    builder.Configuration.GetSection(SupabaseOptions.SectionName));

builder.Services.AddDbContext<LecturioDbContext>((sp, options) =>
{
    var supabaseOptions = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
    options.UseNpgsql(supabaseOptions.GetNpgsqlConnectionString());
});

builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>((sp, client) =>
{
    var supabaseOptions = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
    client.BaseAddress = new Uri($"{supabaseOptions.Url.TrimEnd('/')}/auth/v1/");
    client.DefaultRequestHeaders.Add("apikey", supabaseOptions.AnonKey);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
