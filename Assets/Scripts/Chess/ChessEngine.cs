using System.Collections.Generic;
using System.Linq;

namespace Chess
{
    public class ChessEngine
    {
        private Piece[,] board;
        private PieceColor currentTurn;
        public PieceColor CurrentTurn => currentTurn;
        private List<Move> moveHistory;

        // Castling rights
        private bool whiteCanCastleKingSide = true;
        private bool whiteCanCastleQueenSide = true;
        private bool blackCanCastleKingSide = true;
        private bool blackCanCastleQueenSide = true;

        // En passant target (-1 if none)
        private int enPassantRow = -1;
        private int enPassantCol = -1;

        // King positions for fast lookup
        private int whiteKingRow = 7, whiteKingCol = 4;
        private int blackKingRow = 0, blackKingCol = 4;

        public Piece[,] Board => board;
        public bool WhiteCanCastleKingSide => whiteCanCastleKingSide;
        public bool WhiteCanCastleQueenSide => whiteCanCastleQueenSide;
        public bool BlackCanCastleKingSide => blackCanCastleKingSide;
        public bool BlackCanCastleQueenSide => blackCanCastleQueenSide;
        public int EnPassantRow => enPassantRow;
        public int EnPassantCol => enPassantCol;
        public List<Move> MoveHistory => moveHistory;
        public int WhiteKingRow => whiteKingRow;
        public int WhiteKingCol => whiteKingCol;
        public int BlackKingRow => blackKingRow;
        public int BlackKingCol => blackKingCol;

        public ChessEngine()
        {
            board = new Piece[8, 8];
            moveHistory = new List<Move>();
            InitializeBoard();
            currentTurn = PieceColor.White;
        }

        public ChessEngine(ChessEngine other)
        {
            board = (Piece[,])other.board.Clone();
            currentTurn = other.currentTurn;
            moveHistory = new List<Move>(other.moveHistory);
            whiteCanCastleKingSide = other.whiteCanCastleKingSide;
            whiteCanCastleQueenSide = other.whiteCanCastleQueenSide;
            blackCanCastleKingSide = other.blackCanCastleKingSide;
            blackCanCastleQueenSide = other.blackCanCastleQueenSide;
            enPassantRow = other.enPassantRow;
            enPassantCol = other.enPassantCol;
            whiteKingRow = other.whiteKingRow;
            whiteKingCol = other.whiteKingCol;
            blackKingRow = other.blackKingRow;
            blackKingCol = other.blackKingCol;
        }

        private void InitializeBoard()
        {
            // Clear board
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    board[r, c] = default;

            // Black pieces (row 0)
            board[0, 0] = new Piece(PieceType.Rook, PieceColor.Black);
            board[0, 1] = new Piece(PieceType.Knight, PieceColor.Black);
            board[0, 2] = new Piece(PieceType.Bishop, PieceColor.Black);
            board[0, 3] = new Piece(PieceType.Queen, PieceColor.Black);
            board[0, 4] = new Piece(PieceType.King, PieceColor.Black);
            board[0, 5] = new Piece(PieceType.Bishop, PieceColor.Black);
            board[0, 6] = new Piece(PieceType.Knight, PieceColor.Black);
            board[0, 7] = new Piece(PieceType.Rook, PieceColor.Black);

            // Black pawns (row 1)
            for (int c = 0; c < 8; c++)
                board[1, c] = new Piece(PieceType.Pawn, PieceColor.Black);

            // White pieces (row 7)
            board[7, 0] = new Piece(PieceType.Rook, PieceColor.White);
            board[7, 1] = new Piece(PieceType.Knight, PieceColor.White);
            board[7, 2] = new Piece(PieceType.Bishop, PieceColor.White);
            board[7, 3] = new Piece(PieceType.Queen, PieceColor.White);
            board[7, 4] = new Piece(PieceType.King, PieceColor.White);
            board[7, 5] = new Piece(PieceType.Bishop, PieceColor.White);
            board[7, 6] = new Piece(PieceType.Knight, PieceColor.White);
            board[7, 7] = new Piece(PieceType.Rook, PieceColor.White);

            // White pawns (row 6)
            for (int c = 0; c < 8; c++)
                board[6, c] = new Piece(PieceType.Pawn, PieceColor.White);
        }

        public Piece GetPiece(int row, int col)
        {
            if (row < 0 || row > 7 || col < 0 || col > 7)
                return default;
            return board[row, col];
        }

        // Get all pseudo-legal moves for a piece (moves that follow piece movement rules)
        private List<Move> GetPseudoLegalMoves(int row, int col)
        {
            List<Move> moves = new List<Move>();
            Piece piece = board[row, col];
            if (piece.Type == PieceType.None) return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn: AddPawnMoves(moves, row, col, piece); break;
                case PieceType.Knight: AddKnightMoves(moves, row, col, piece); break;
                case PieceType.Bishop: AddSlidingMoves(moves, row, col, piece, MoveGenerator.BishopDirections); break;
                case PieceType.Rook: AddSlidingMoves(moves, row, col, piece, MoveGenerator.RookDirections); break;
                case PieceType.Queen:
                    AddSlidingMoves(moves, row, col, piece, MoveGenerator.RookDirections);
                    AddSlidingMoves(moves, row, col, piece, MoveGenerator.BishopDirections);
                    break;
                case PieceType.King: AddKingMoves(moves, row, col, piece); break;
            }

            return moves;
        }

        // Get legal moves (pseudo-legal moves that don't leave own king in check)
        public List<Move> GetLegalMoves(int row, int col)
        {
            List<Move> pseudoMoves = GetPseudoLegalMoves(row, col);
            List<Move> legalMoves = new List<Move>();
            PieceColor color = board[row, col].Color;

            foreach (Move move in pseudoMoves)
            {
                // Simulate the move and check if king is in check
                if (!WouldBeInCheckAfterMove(move, color))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        // Check if a color is in check
        public bool IsInCheck(PieceColor color)
        {
            (int kingRow, int kingCol) = color == PieceColor.White
                ? (whiteKingRow, whiteKingCol)
                : (blackKingRow, blackKingCol);

            return IsSquareAttacked(kingRow, kingCol, color);
        }

        // Check if a color is in checkmate
        public bool IsCheckmate(PieceColor color)
        {
            if (!IsInCheck(color)) return false;
            return !HasAnyLegalMove(color);
        }

        // Check if a color is in stalemate
        public bool IsStalemate(PieceColor color)
        {
            if (IsInCheck(color)) return false;
            return !HasAnyLegalMove(color);
        }

        // Check if game is drawn (insufficient material, stalemate checked separately)
        public bool IsDraw()
        {
            return IsInsufficientMaterial();
        }

        private bool HasAnyLegalMove(PieceColor color)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board[r, c].Type != PieceType.None && board[r, c].Color == color)
                    {
                        if (GetLegalMoves(r, c).Count > 0)
                            return true;
                    }
                }
            }
            return false;
        }

        // Check if a square is attacked by any piece of the opposite color
        public bool IsSquareAttacked(int row, int col, PieceColor defendingColor)
        {
            PieceColor attackingColor = defendingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Check pawn attacks
            int pawnDir = defendingColor == PieceColor.White ? -1 : 1;
            foreach (int dc in new[] { -1, 1 })
            {
                int pr = row + pawnDir;
                int pc = col + dc;
                if (pr >= 0 && pr < 8 && pc >= 0 && pc < 8)
                {
                    Piece p = board[pr, pc];
                    if (p.Type == PieceType.Pawn && p.Color == attackingColor)
                        return true;
                }
            }

            // Check knight attacks
            foreach (var (dr, dc) in MoveGenerator.KnightMoves)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece p = board[nr, nc];
                    if (p.Type == PieceType.Knight && p.Color == attackingColor)
                        return true;
                }
            }

            // Check king attacks
            foreach (var (dr, dc) in MoveGenerator.KingMoves)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece p = board[nr, nc];
                    if (p.Type == PieceType.King && p.Color == attackingColor)
                        return true;
                }
            }

            // Check sliding pieces (rook, bishop, queen)
            // Rook directions & queen
            foreach (var (dr, dc) in MoveGenerator.RookDirections)
            {
                int nr = row + dr;
                int nc = col + dc;
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece p = board[nr, nc];
                    if (p.Type != PieceType.None)
                    {
                        if (p.Color == attackingColor && (p.Type == PieceType.Rook || p.Type == PieceType.Queen))
                            return true;
                        break;
                    }
                    nr += dr;
                    nc += dc;
                }
            }

            // Bishop directions & queen
            foreach (var (dr, dc) in MoveGenerator.BishopDirections)
            {
                int nr = row + dr;
                int nc = col + dc;
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece p = board[nr, nc];
                    if (p.Type != PieceType.None)
                    {
                        if (p.Color == attackingColor && (p.Type == PieceType.Bishop || p.Type == PieceType.Queen))
                            return true;
                        break;
                    }
                    nr += dr;
                    nc += dc;
                }
            }

            return false;
        }

        private bool WouldBeInCheckAfterMove(Move move, PieceColor color)
        {
            // Clone board and simulate
            Piece[,] clonedBoard = (Piece[,])board.Clone();
            int clonedEnPassantRow = enPassantRow;
            int clonedEnPassantCol = enPassantCol;
            int clonedWKR = whiteKingRow, clonedWKC = whiteKingCol;
            int clonedBKR = blackKingRow, clonedBKC = blackKingCol;
            bool clonedWCKS = whiteCanCastleKingSide, clonedWCQS = whiteCanCastleQueenSide;
            bool clonedBCKS = blackCanCastleKingSide, clonedBCQS = blackCanCastleQueenSide;

            // Apply move on cloned board
            ApplyMoveInternal(move);

            bool inCheck = IsInCheck(color);

            // Restore
            board = clonedBoard;
            enPassantRow = clonedEnPassantRow;
            enPassantCol = clonedEnPassantCol;
            whiteKingRow = clonedWKR;
            whiteKingCol = clonedWKC;
            blackKingRow = clonedBKR;
            blackKingCol = clonedBKC;
            whiteCanCastleKingSide = clonedWCKS;
            whiteCanCastleQueenSide = clonedWCQS;
            blackCanCastleKingSide = clonedBCKS;
            blackCanCastleQueenSide = clonedBCQS;

            return inCheck;
        }

        private void ApplyMoveInternal(Move move)
        {
            // Handle en passant capture
            if (move.IsEnPassant)
            {
                // Remove the captured pawn
                board[move.FromRow, move.ToCol] = default;
            }

            // Handle castling - move the rook
            if (move.IsCastling)
            {
                if (move.CastlingType == CastlingType.KingSide)
                {
                    int row = move.FromRow;
                    board[row, 5] = board[row, 7]; // Rook moves to F file
                    board[row, 7] = default;
                    Piece rook = board[row, 5];
                    rook.HasMoved = true;
                    board[row, 5] = rook;
                }
                else // QueenSide
                {
                    int row = move.FromRow;
                    board[row, 3] = board[row, 0]; // Rook moves to D file
                    board[row, 0] = default;
                    Piece rook = board[row, 3];
                    rook.HasMoved = true;
                    board[row, 3] = rook;
                }
            }

            // Move piece
            Piece movedPiece = board[move.FromRow, move.FromCol];
            movedPiece.HasMoved = true;

            // Handle promotion
            if (move.IsPromotion)
            {
                movedPiece.Type = move.PromotionType;
            }

            board[move.ToRow, move.ToCol] = movedPiece;
            board[move.FromRow, move.FromCol] = default;

            // Update king position
            if (movedPiece.Type == PieceType.King)
            {
                if (movedPiece.Color == PieceColor.White)
                {
                    whiteKingRow = move.ToRow;
                    whiteKingCol = move.ToCol;
                    whiteCanCastleKingSide = false;
                    whiteCanCastleQueenSide = false;
                }
                else
                {
                    blackKingRow = move.ToRow;
                    blackKingCol = move.ToCol;
                    blackCanCastleKingSide = false;
                    blackCanCastleQueenSide = false;
                }
            }

            // Update castling rights if rook moved or captured
            if (move.FromRow == 7 && move.FromCol == 0) whiteCanCastleQueenSide = false;
            if (move.FromRow == 7 && move.FromCol == 7) whiteCanCastleKingSide = false;
            if (move.FromRow == 0 && move.FromCol == 0) blackCanCastleQueenSide = false;
            if (move.FromRow == 0 && move.FromCol == 7) blackCanCastleKingSide = false;
            if (move.ToRow == 7 && move.ToCol == 0) whiteCanCastleQueenSide = false;
            if (move.ToRow == 7 && move.ToCol == 7) whiteCanCastleKingSide = false;
            if (move.ToRow == 0 && move.ToCol == 0) blackCanCastleQueenSide = false;
            if (move.ToRow == 0 && move.ToCol == 7) blackCanCastleKingSide = false;

            // Update en passant state
            enPassantRow = -1;
            enPassantCol = -1;
            if (movedPiece.Type == PieceType.Pawn && System.Math.Abs(move.ToRow - move.FromRow) == 2)
            {
                enPassantRow = (move.FromRow + move.ToRow) / 2;
                enPassantCol = move.FromCol;
            }
        }

        // Make a move on the actual board and update game state
        public void MakeMove(Move move)
        {
            ApplyMoveInternal(move);
            moveHistory.Add(move);
            currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        // --- Move generation helpers ---

        private void AddPawnMoves(List<Move> moves, int row, int col, Piece piece)
        {
            int dir = piece.IsWhite ? -1 : 1;
            int startRow = piece.IsWhite ? 6 : 1;
            int promotionRow = piece.IsWhite ? 0 : 7;

            // Forward one square
            int nr = row + dir;
            if (nr >= 0 && nr < 8 && board[nr, col].Type == PieceType.None)
            {
                if (nr == promotionRow)
                {
                    AddPromotionMoves(moves, row, col, nr, col, piece);
                }
                else
                {
                    moves.Add(CreateMove(row, col, nr, col, piece));

                    // Forward two squares from starting position
                    if (row == startRow)
                    {
                        int nr2 = row + 2 * dir;
                        if (board[nr2, col].Type == PieceType.None)
                        {
                            moves.Add(CreateMove(row, col, nr2, col, piece));
                        }
                    }
                }
            }

            // Captures
            foreach (int dc in new[] { -1, 1 })
            {
                int nc = col + dc;
                if (nc < 0 || nc > 7) continue;
                nr = row + dir;
                if (nr < 0 || nr > 7) continue;

                // Normal capture
                if (board[nr, nc].Type != PieceType.None && board[nr, nc].Color != piece.Color)
                {
                    if (nr == promotionRow)
                    {
                        AddPromotionMoves(moves, row, col, nr, nc, piece);
                    }
                    else
                    {
                        moves.Add(CreateMove(row, col, nr, nc, piece));
                    }
                }

                // En passant
                if (nr == enPassantRow && nc == enPassantCol)
                {
                    moves.Add(new Move
                    {
                        FromRow = row, FromCol = col,
                        ToRow = nr, ToCol = nc,
                        MovedPiece = piece,
                        CapturedPiece = board[enPassantRow - dir, enPassantCol],
                        IsEnPassant = true
                    });
                }
            }
        }

        private void AddPromotionMoves(List<Move> moves, int fromRow, int fromCol, int toRow, int toCol, Piece piece)
        {
            foreach (PieceType promoType in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
            {
                moves.Add(new Move
                {
                    FromRow = fromRow, FromCol = fromCol,
                    ToRow = toRow, ToCol = toCol,
                    MovedPiece = piece,
                    CapturedPiece = board[toRow, toCol],
                    IsPromotion = true,
                    PromotionType = promoType
                });
            }
        }

        private void AddKnightMoves(List<Move> moves, int row, int col, Piece piece)
        {
            foreach (var (dr, dc) in MoveGenerator.KnightMoves)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece target = board[nr, nc];
                    if (target.Type == PieceType.None || target.Color != piece.Color)
                    {
                        moves.Add(CreateMove(row, col, nr, nc, piece));
                    }
                }
            }
        }

        private void AddSlidingMoves(List<Move> moves, int row, int col, Piece piece, (int dr, int dc)[] directions)
        {
            foreach (var (dr, dc) in directions)
            {
                int nr = row + dr;
                int nc = col + dc;
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece target = board[nr, nc];
                    if (target.Type == PieceType.None)
                    {
                        moves.Add(CreateMove(row, col, nr, nc, piece));
                    }
                    else
                    {
                        if (target.Color != piece.Color)
                        {
                            moves.Add(CreateMove(row, col, nr, nc, piece));
                        }
                        break;
                    }
                    nr += dr;
                    nc += dc;
                }
            }
        }

        private void AddKingMoves(List<Move> moves, int row, int col, Piece piece)
        {
            foreach (var (dr, dc) in MoveGenerator.KingMoves)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    Piece target = board[nr, nc];
                    if (target.Type == PieceType.None || target.Color != piece.Color)
                    {
                        moves.Add(CreateMove(row, col, nr, nc, piece));
                    }
                }
            }

            // Castling
            AddCastlingMoves(moves, row, col, piece);
        }

        private void AddCastlingMoves(List<Move> moves, int row, int col, Piece piece)
        {
            if (piece.HasMoved) return;

            bool isWhite = piece.IsWhite;
            bool canCastleKingSide = isWhite ? whiteCanCastleKingSide : blackCanCastleKingSide;
            bool canCastleQueenSide = isWhite ? whiteCanCastleQueenSide : blackCanCastleQueenSide;

            // King-side castling
            if (canCastleKingSide)
            {
                // Check squares between king and rook are empty
                if (board[row, 5].Type == PieceType.None && board[row, 6].Type == PieceType.None)
                {
                    // Check that rook exists and hasn't moved
                    Piece rook = board[row, 7];
                    if (rook.Type == PieceType.Rook && rook.Color == piece.Color && !rook.HasMoved)
                    {
                        // Check king isn't in check, doesn't pass through check, doesn't end in check
                        if (!IsSquareAttacked(row, col, piece.Color) &&
                            !IsSquareAttacked(row, 5, piece.Color) &&
                            !IsSquareAttacked(row, 6, piece.Color))
                        {
                            moves.Add(new Move
                            {
                                FromRow = row, FromCol = col,
                                ToRow = row, ToCol = 6,
                                MovedPiece = piece,
                                IsCastling = true,
                                CastlingType = CastlingType.KingSide
                            });
                        }
                    }
                }
            }

            // Queen-side castling
            if (canCastleQueenSide)
            {
                if (board[row, 3].Type == PieceType.None &&
                    board[row, 2].Type == PieceType.None &&
                    board[row, 1].Type == PieceType.None)
                {
                    Piece rook = board[row, 0];
                    if (rook.Type == PieceType.Rook && rook.Color == piece.Color && !rook.HasMoved)
                    {
                        if (!IsSquareAttacked(row, col, piece.Color) &&
                            !IsSquareAttacked(row, 3, piece.Color) &&
                            !IsSquareAttacked(row, 2, piece.Color))
                        {
                            moves.Add(new Move
                            {
                                FromRow = row, FromCol = col,
                                ToRow = row, ToCol = 2,
                                MovedPiece = piece,
                                IsCastling = true,
                                CastlingType = CastlingType.QueenSide
                            });
                        }
                    }
                }
            }
        }

        // Check if there's insufficient material for checkmate
        private bool IsInsufficientMaterial()
        {
            // Count non-king pieces only (kings are always on board)
            List<Piece> whitePieces = new List<Piece>();
            List<Piece> blackPieces = new List<Piece>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece p = board[r, c];
                    if (p.Type != PieceType.None && p.Type != PieceType.King)
                    {
                        if (p.IsWhite) whitePieces.Add(p);
                        else blackPieces.Add(p);
                    }
                }
            }

            // King vs King
            if (whitePieces.Count == 0 && blackPieces.Count == 0)
                return true;

            // King + Bishop/Knight vs King
            if (blackPieces.Count == 0 &&
                whitePieces.Count == 1 &&
                (whitePieces[0].Type == PieceType.Bishop || whitePieces[0].Type == PieceType.Knight))
                return true;

            if (whitePieces.Count == 0 &&
                blackPieces.Count == 1 &&
                (blackPieces[0].Type == PieceType.Bishop || blackPieces[0].Type == PieceType.Knight))
                return true;

            // King + Bishop vs King + Bishop (same color bishops)
            if (whitePieces.Count == 1 && blackPieces.Count == 1 &&
                whitePieces[0].Type == PieceType.Bishop && blackPieces[0].Type == PieceType.Bishop)
            {
                return true;
            }

            return false;
        }

        private Move CreateMove(int fromRow, int fromCol, int toRow, int toCol, Piece piece)
        {
            return new Move
            {
                FromRow = fromRow, FromCol = fromCol,
                ToRow = toRow, ToCol = toCol,
                MovedPiece = piece,
                CapturedPiece = board[toRow, toCol],
            };
        }

        // Get all legal moves for the current player
        public List<Move> GetAllLegalMoves()
        {
            List<Move> allMoves = new List<Move>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board[r, c].Type != PieceType.None && board[r, c].Color == currentTurn)
                    {
                        allMoves.AddRange(GetLegalMoves(r, c));
                    }
                }
            }
            return allMoves;
        }
    }
}
