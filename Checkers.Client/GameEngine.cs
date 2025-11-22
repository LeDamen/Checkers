using System;
using System.Collections.Generic;

namespace Checkers.Client
{
    public enum PieceType
    {
        None,
        White,
        Black,
        WhiteKing,
        BlackKing
    }

    public class MoveResult
    {
        public bool IsValid { get; set; }
        public bool IsCapture { get; set; }
        public (int r, int c)? CapturedPiece { get; set; }
        public bool BecameKing { get; set; }
    }

    public class GameEngine
    {
        public PieceType[,] Board = new PieceType[8, 8];
        public PieceType CurrentTurn = PieceType.White;

        public GameEngine()
        {
            ResetBoard();
        }

        public void ResetBoard()
        {
            Board = new PieceType[8, 8];
            CurrentTurn = PieceType.White;

            // чёрные шашки (вверху)
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 8; c++)
                    if ((r + c) % 2 == 1)
                        Board[r, c] = PieceType.Black;

            // белые шашки (внизу)
            for (int r = 5; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if ((r + c) % 2 == 1)
                        Board[r, c] = PieceType.White;
        }

        private bool IsKing(PieceType p) =>
            p == PieceType.WhiteKing || p == PieceType.BlackKing;

        private bool IsSameColor(PieceType a, PieceType b)
        {
            if (a == PieceType.None || b == PieceType.None) return false;
            return (a == PieceType.White || a == PieceType.WhiteKing) ==
                   (b == PieceType.White || b == PieceType.WhiteKing);
        }

        // === Проверка хода ===================================================================
        public MoveResult ValidateMove(int sr, int sc, int tr, int tc)
        {
            var result = new MoveResult();
            var piece = Board[sr, sc];

            if (piece == PieceType.None)
                return result;

            if (!IsSameColor(piece, CurrentTurn))
                return result;

            int dr = tr - sr;
            int dc = tc - sc;

            if (Math.Abs(dr) != Math.Abs(dc))
                return result;

            bool isKing = IsKing(piece);
            int direction = (piece == PieceType.White || piece == PieceType.WhiteKing) ? -1 : 1;

            // --- Обычный ход (не рубка)
            if (Math.Abs(dr) == 1)
            {
                if (!isKing && dr != direction)
                    return result;

                if (Board[tr, tc] != PieceType.None)
                    return result;

                result.IsValid = true;
                return result;
            }

            // --- Рубка (перепрыгивание)
            if (Math.Abs(dr) == 2)
            {
                int mr = (sr + tr) / 2;
                int mc = (sc + tc) / 2;

                var middle = Board[mr, mc];
                if (middle == PieceType.None)
                    return result;

                if (IsSameColor(middle, piece))
                    return result;

                if (!isKing && dr != 2 * direction)
                    return result;

                if (Board[tr, tc] != PieceType.None)
                    return result;

                // рубка
                result.IsValid = true;
                result.IsCapture = true;
                result.CapturedPiece = (mr, mc);
                return result;
            }

            return result;
        }

        // === Выполнить ход ===================================================================
        public MoveResult MakeMove(int sr, int sc, int tr, int tc)
        {
            var vr = ValidateMove(sr, sc, tr, tc);
            if (!vr.IsValid)
                return vr;

            var piece = Board[sr, sc];

            Board[sr, sc] = PieceType.None;
            Board[tr, tc] = piece;

            // рубка
            if (vr.IsCapture && vr.CapturedPiece != null)
            {
                var (cr, cc) = vr.CapturedPiece.Value;
                Board[cr, cc] = PieceType.None;
            }

            // превращение в дамку
            if (piece == PieceType.White && tr == 0)
            {
                Board[tr, tc] = PieceType.WhiteKing;
                vr.BecameKing = true;
            }
            if (piece == PieceType.Black && tr == 7)
            {
                Board[tr, tc] = PieceType.BlackKing;
                vr.BecameKing = true;
            }

            // смена очереди (если это не серия рубок)
            if (!vr.IsCapture)
            {
                CurrentTurn = CurrentTurn == PieceType.White ?
                              PieceType.Black : PieceType.White;
            }

            return vr;
        }
    }
}
