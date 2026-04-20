var builder = WebApplication.CreateBuilder(args);

// ✅ ambil connection string dari Railway ENV
var conn = builder.Configuration.GetConnectionString("Default");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddControllers();

// 👉 kalau kamu pakai DatabaseService, bisa inject di sini
// builder.Services.AddSingleton(new DatabaseService(conn));

var app = builder.Build();

// 🔥 default index.html
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

// 🔥 serve wwwroot
app.UseStaticFiles();

// CORS
app.UseCors("AllowAll");

// Routing API
app.MapControllers();

// 🔥 FIX WAJIB BUAT RAILWAY (JANGAN HARDCODE 5000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();