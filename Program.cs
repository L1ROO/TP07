using Tp07.Models;

var builder = WebApplication.CreateBuilder(args);

// Session: guarda el usuario logueado del lado del servidor
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

// La cadena de conexión se configura en appsettings.json
BD.ConnectionString = builder.Configuration.GetConnectionString("DBRedSocial")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DBRedSocial' en appsettings.json");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
