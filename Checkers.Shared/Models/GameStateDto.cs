using System.Collections.Generic;

namespace Checkers.Shared.Models
{
    public class GameStateDto
    {
        public string GameId { get; set; }
        public PlayerDto PlayerWhite { get; set; }
        public PlayerDto PlayerBlack { get; set; }
        public List<string> Board { get; set; }   // 64 клетки: "", "w","b","W","B"
        public bool WhiteTurn { get; set; } = true;
        public bool IsFinished { get; set; }
        public string Winner { get; set; }
        public List<MoveDto> Moves { get; set; }

        

        public GameStateDto() { }

        public GameStateDto(
            string gameId,
            PlayerDto white,
            PlayerDto black,
            List<string> board,
            bool whiteTurn,
            bool isFinished,
            string winner,
            List<MoveDto> moves)
        {
            GameId = gameId;
            PlayerWhite = white;
            PlayerBlack = black;
            Board = board;
            WhiteTurn = whiteTurn;
            IsFinished = isFinished;
            Winner = winner;
            Moves = moves;
        }
    }
}
