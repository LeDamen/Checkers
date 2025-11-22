using Microsoft.EntityFrameworkCore;
using Checkers.Server.Data;
using Checkers.Server.Hubs;


var builder = WebApplication.CreateBuilder(args);


// Add services
builder.Services.AddSignalR();
builder.Services.AddControllers();


builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=checkers.db"));


var app = builder.Build();


app.UseRouting();
app.MapControllers();
app.MapHub<GameHub>("/gamehub");


app.Run();