using System;
using System.ComponentModel.DataAnnotations;


namespace Checkers.Server.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }
        public int? Player1Id { get; set; }
        public int? Player2Id { get; set; }
        public int? WinnerId { get; set; }
        public string MovesJson { get; set; } = "[]"; // JSON array of moves
        public DateTime DatePlayed { get; set; }
    }
}