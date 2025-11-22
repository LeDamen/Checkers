using Checkers.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;


namespace Checkers.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Match> Matches { get; set; } = null!;
    }
}