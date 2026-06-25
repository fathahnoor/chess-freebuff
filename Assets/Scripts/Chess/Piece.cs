using System.Collections.Generic;

namespace Chess
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { White, Black }

    [System.Serializable]
    public struct Piece
    {
        public PieceType Type;
        public PieceColor Color;
        public bool HasMoved;

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
            HasMoved = false;
        }

        public char Symbol => Color == PieceColor.White ? Type switch
        {
            PieceType.King => '♔',
            PieceType.Queen => '♕',
            PieceType.Rook => '♖',
            PieceType.Bishop => '♗',
            PieceType.Knight => '♘',
            PieceType.Pawn => '♙',
            _ => '?'
        } : Type switch
        {
            PieceType.King => '♚',
            PieceType.Queen => '♛',
            PieceType.Rook => '♜',
            PieceType.Bishop => '♝',
            PieceType.Knight => '♞',
            PieceType.Pawn => '♟',
            _ => '?'
        };

        public bool IsWhite => Color == PieceColor.White;
        public bool IsBlack => Color == PieceColor.Black;

        public override string ToString() => $"{Color} {Type}";
    }

    public struct Move
    {
        public int FromRow;
        public int FromCol;
        public int ToRow;
        public int ToCol;
        public Piece MovedPiece;
        public Piece CapturedPiece;
        public bool IsEnPassant;
        public bool IsCastling;
        public CastlingType CastlingType;
        public bool IsPromotion;
        public PieceType PromotionType;

        public string Notation
        {
            get
            {
                string cols = "abcdefgh";
                return $"{cols[FromCol]}{8 - FromRow} → {cols[ToCol]}{8 - ToRow}";
            }
        }
    }

    public enum CastlingType { None, KingSide, QueenSide }

    public class MoveGenerator
    {
        // Direction vectors for sliding pieces
        public static readonly (int dr, int dc)[] KnightMoves =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };

        public static readonly (int dr, int dc)[] KingMoves =
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };

        public static readonly (int dr, int dc)[] RookDirections =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        public static readonly (int dr, int dc)[] BishopDirections =
        {
            (-1, -1), (-1, 1), (1, -1), (1, 1)
        };
    }
}
