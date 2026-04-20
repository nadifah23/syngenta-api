var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddControllers();

var app = builder.Build();

// 🔥 FIX: paksa index.html jadi default
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

// 🔥 WAJIB
app.Urls.Add("http://0.0.0.0:5000");

app.Run();