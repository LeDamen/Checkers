namespace Checkers.Shared.Models
{
    public class PlayerDto
    {
        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public bool IsWhite { get; set; }

        public PlayerDto() { }

        public PlayerDto(string connectionId, string name, bool isWhite)
        {
            ConnectionId = connectionId;
            Name = name;
            IsWhite = isWhite;
        }
    }
}
