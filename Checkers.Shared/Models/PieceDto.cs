namespace Checkers.Shared.Models
{
    public class PieceDto
    {
        public bool IsWhite { get; set; }
        public bool IsKing { get; set; }

        public PieceDto() { }

        public PieceDto(bool isWhite, bool isKing = false)
        {
            IsWhite = isWhite;
            IsKing = isKing;
        }
    }
}
