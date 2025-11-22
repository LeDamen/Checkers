using System.ComponentModel.DataAnnotations;


namespace Checkers.Server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = null!;
    }
}