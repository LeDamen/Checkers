using Microsoft.EntityFrameworkCore;
using Checkers.Server.Data;
using Checkers.Server.Hubs;


var builder = WebApplication.CreateBuilder(args);


// Add services
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=checkers.db"));


var app = builder.Build();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();
app.MapHub<GameHub>("/gamehub");


// Ensure SQLite database / tables exist (lightweight, for development)
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<Checkers.Server.Data.AppDbContext>();
	db.Database.EnsureCreated();
}

app.Run();