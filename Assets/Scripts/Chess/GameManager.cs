using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Chess
{
    public class GameManager : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private Color lightSquareColor = new Color(0.94f, 0.86f, 0.73f);
        [SerializeField] private Color darkSquareColor = new Color(0.58f, 0.40f, 0.27f);
        [SerializeField] private Color selectedSquareColor = new Color(0.85f, 0.95f, 0.50f);
        [SerializeField] private Color validMoveColor = new Color(0.50f, 0.85f, 0.30f, 0.50f);
        [SerializeField] private Color lastMoveColor = new Color(0.80f, 0.90f, 0.40f, 0.50f);
        [SerializeField] private Color checkColor = new Color(1.0f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color whitePieceColor = Color.white;
        [SerializeField] private Color blackPieceColor = Color.black;
        [SerializeField] private int boardSize = 480; // pixels

        [Header("Font Settings")]
        [SerializeField] private int pieceFontSize = 32;

        private ChessEngine engine;
        private GameObject boardPanel;
        private Image[,] squareImages;
        private Text[,] pieceTexts;
        private GameObject[,] highlightOverlays;
        private int? selectedRow;
        private int? selectedCol;
        private List<Move> currentLegalMoves;
        private Text statusText;
        private Text capturedWhiteText;
        private Text capturedBlackText;
        private GameObject gameOverPanel;
        private Text gameOverText;
        private Button restartButton;
        private int lastMoveFromRow = -1, lastMoveFromCol = -1;
        private int lastMoveToRow = -1, lastMoveToCol = -1;

        void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            engine = new ChessEngine();
            currentLegalMoves = new List<Move>();
            CreateGameUI();
            RefreshBoard();
            UpdateStatus();
        }

        void CreateGameUI()
        {
            // Create EventSystem (required for UI input with Input System Package)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Create Canvas
            GameObject canvasObj = new GameObject("ChessCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create board panel (centered)
            boardPanel = new GameObject("BoardPanel");
            boardPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform boardRect = boardPanel.AddComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.sizeDelta = new Vector2(boardSize, boardSize);
            boardRect.anchoredPosition = new Vector2(0, 30);

            // Create squares
            float squareSize = boardSize / 8f;
            squareImages = new Image[8, 8];
            pieceTexts = new Text[8, 8];
            highlightOverlays = new GameObject[8, 8];

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Square background
                    GameObject square = new GameObject($"Square_{row}_{col}");
                    square.transform.SetParent(boardPanel.transform, false);
                    RectTransform sqRect = square.AddComponent<RectTransform>();
                    sqRect.anchorMin = Vector2.zero;
                    sqRect.anchorMax = Vector2.zero;
                    sqRect.sizeDelta = new Vector2(squareSize, squareSize);
                    sqRect.anchoredPosition = new Vector2(col * squareSize, (7 - row) * squareSize);

                    Image img = square.AddComponent<Image>();
                    img.color = (row + col) % 2 == 0 ? lightSquareColor : darkSquareColor;
                    squareImages[row, col] = img;

                    // Add click handler
                    int capturedRow = row;
                    int capturedCol = col;
                    Button btn = square.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.AddListener(() => OnSquareClicked(capturedRow, capturedCol));

                    // Highlight overlay (for selected, valid moves, last move)
                    GameObject overlay = new GameObject($"Overlay_{row}_{col}");
                    overlay.transform.SetParent(square.transform, false);
                    RectTransform olRect = overlay.AddComponent<RectTransform>();
                    olRect.anchorMin = Vector2.zero;
                    olRect.anchorMax = Vector2.one;
                    olRect.sizeDelta = Vector2.zero;
                    Image olImg = overlay.AddComponent<Image>();
                    olImg.color = new Color(0, 0, 0, 0);
                    highlightOverlays[row, col] = overlay;

                    // Piece text
                    GameObject pieceObj = new GameObject($"Piece_{row}_{col}");
                    pieceObj.transform.SetParent(square.transform, false);
                    RectTransform ptRect = pieceObj.AddComponent<RectTransform>();
                    ptRect.anchorMin = Vector2.zero;
                    ptRect.anchorMax = Vector2.one;
                    ptRect.sizeDelta = Vector2.zero;

                    Text text = pieceObj.AddComponent<Text>();
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.fontSize = pieceFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.raycastTarget = false;
                    pieceTexts[row, col] = text;
                }
            }

            // Create status panel
            GameObject statusPanel = new GameObject("StatusPanel");
            statusPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform spRect = statusPanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0, 1);
            spRect.anchorMax = new Vector2(1, 1);
            spRect.sizeDelta = new Vector2(0, 60);
            spRect.anchoredPosition = new Vector2(0, 0);

            statusText = new GameObject("StatusText").AddComponent<Text>();
            statusText.transform.SetParent(statusPanel.transform, false);
            RectTransform stRect = statusText.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 24;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.white;

            // Create captured pieces panels
            // White's captured pieces (top-left)
            capturedWhiteText = CreateCapturedPiecesLabel(canvasObj, "CapturedWhite", new Vector2(-boardSize / 2f - 100, 60));
            // Black's captured pieces (top-right)
            capturedBlackText = CreateCapturedPiecesLabel(canvasObj, "CapturedBlack", new Vector2(boardSize / 2f + 100, 60));

            // Game over panel (hidden by default)
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform goRect = gameOverPanel.AddComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0.5f, 0.5f);
            goRect.anchorMax = new Vector2(0.5f, 0.5f);
            goRect.sizeDelta = new Vector2(300, 200);
            goRect.anchoredPosition = Vector2.zero;

            Image goBg = gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0, 0, 0, 0.85f);

            gameOverText = new GameObject("GameOverText").AddComponent<Text>();
            gameOverText.transform.SetParent(gameOverPanel.transform, false);
            RectTransform gtRect = gameOverText.GetComponent<RectTransform>();
            gtRect.anchorMin = Vector2.zero;
            gtRect.anchorMax = Vector2.one;
            gtRect.sizeDelta = Vector2.zero;
            gtRect.offsetMin = new Vector2(20, 60);
            gtRect.offsetMax = new Vector2(-20, -20);
            gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gameOverText.fontSize = 28;
            gameOverText.alignment = TextAnchor.MiddleCenter;
            gameOverText.color = Color.white;

            GameObject restartBtnObj = new GameObject("RestartButton");
            restartBtnObj.transform.SetParent(gameOverPanel.transform, false);
            RectTransform rbRect = restartBtnObj.AddComponent<RectTransform>();
            restartButton = restartBtnObj.AddComponent<Button>();
            rbRect.anchorMin = new Vector2(0.5f, 0);
            rbRect.anchorMax = new Vector2(0.5f, 0);
            rbRect.sizeDelta = new Vector2(160, 40);
            rbRect.anchoredPosition = new Vector2(0, 20);

            Image rbImage = restartButton.gameObject.AddComponent<Image>();
            rbImage.color = new Color(0.3f, 0.5f, 0.8f);

            Text rbText = new GameObject("RestartText").AddComponent<Text>();
            rbText.transform.SetParent(restartButton.transform, false);
            RectTransform rbtRect = rbText.GetComponent<RectTransform>();
            rbtRect.anchorMin = Vector2.zero;
            rbtRect.anchorMax = Vector2.one;
            rbtRect.sizeDelta = Vector2.zero;
            rbText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rbText.fontSize = 20;
            rbText.alignment = TextAnchor.MiddleCenter;
            rbText.text = "New Game";
            rbText.color = Color.white;

            restartButton.onClick.AddListener(RestartGame);
            gameOverPanel.SetActive(false);
        }

        private Text CreateCapturedPiecesLabel(GameObject parent, string name, Vector2 position)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160, 400);
            rect.anchoredPosition = position;

            Text text = new GameObject("LabelText").AddComponent<Text>();
            text.transform.SetParent(panel.transform, false);
            RectTransform tRect = text.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.8f, 0.8f, 0.8f);
            return text;
        }

        void OnSquareClicked(int row, int col)
        {
            if (gameOverPanel.activeSelf) return;

            Piece clickedPiece = engine.GetPiece(row, col);

            // If a piece is already selected
            if (selectedRow.HasValue && selectedCol.HasValue)
            {
                // If clicking the same square, deselect
                if (row == selectedRow.Value && col == selectedCol.Value)
                {
                    ClearSelection();
                    RefreshBoard();
                    return;
                }

                // If clicking own piece, change selection
                if (clickedPiece.Type != PieceType.None && clickedPiece.Color == engine.CurrentTurn)
                {
                    SelectPiece(row, col);
                    return;
                }

                // Try to make a move
                Move? targetMove = null;
                foreach (Move move in currentLegalMoves)
                {
                    if (move.ToRow == row && move.ToCol == col)
                    {
                        targetMove = move;
                        break;
                    }
                }

                if (targetMove.HasValue)
                {
                    // Handle pawn promotion with auto-queen for simplicity
                    Move moveToMake = targetMove.Value;
                    if (moveToMake.IsPromotion)
                    {
                        // For now, auto-promote to Queen (most common choice)
                        moveToMake.PromotionType = PieceType.Queen;
                    }

                    lastMoveFromRow = moveToMake.FromRow;
                    lastMoveFromCol = moveToMake.FromCol;
                    lastMoveToRow = moveToMake.ToRow;
                    lastMoveToCol = moveToMake.ToCol;

                    engine.MakeMove(moveToMake);
                    ClearSelection();
                    RefreshBoard();
                    UpdateStatus();

                    // Check for game over
                    CheckGameOver();
                }
                else
                {
                    ClearSelection();
                    RefreshBoard();
                }
            }
            else
            {
                // No piece selected - try to select one
                if (clickedPiece.Type != PieceType.None && clickedPiece.Color == engine.CurrentTurn)
                {
                    SelectPiece(row, col);
                }
            }
        }

        void SelectPiece(int row, int col)
        {
            selectedRow = row;
            selectedCol = col;
            currentLegalMoves = engine.GetLegalMoves(row, col);
            RefreshBoard();

            // Highlight selected square
            SetHighlight(row, col, selectedSquareColor);

            // Highlight valid moves
            foreach (Move move in currentLegalMoves)
            {
                if (move.CapturedPiece.Type != PieceType.None)
                {
                    SetHighlight(move.ToRow, move.ToCol, new Color(1, 0, 0, 0.4f));
                }
                else
                {
                    SetHighlight(move.ToRow, move.ToCol, validMoveColor);
                }
            }
        }

        void ClearSelection()
        {
            selectedRow = null;
            selectedCol = null;
            currentLegalMoves.Clear();
        }

        void SetHighlight(int row, int col, Color color)
        {
            if (highlightOverlays[row, col] != null)
            {
                highlightOverlays[row, col].GetComponent<Image>().color = color;
            }
        }

        void ClearHighlights()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    SetHighlight(r, c, new Color(0, 0, 0, 0));
        }

        void RefreshBoard()
        {
            ClearHighlights();

            // Show last move
            if (lastMoveFromRow >= 0)
            {
                SetHighlight(lastMoveFromRow, lastMoveFromCol, lastMoveColor);
                SetHighlight(lastMoveToRow, lastMoveToCol, lastMoveColor);
            }

            // Highlight king in check
            if (engine.IsInCheck(PieceColor.White))
            {
                SetHighlight(engine.WhiteKingRow, engine.WhiteKingCol, checkColor);
            }
            if (engine.IsInCheck(PieceColor.Black))
            {
                SetHighlight(engine.BlackKingRow, engine.BlackKingCol, checkColor);
            }

            // Update piece visuals
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = engine.GetPiece(row, col);
                    if (piece.Type != PieceType.None)
                    {
                        pieceTexts[row, col].text = piece.Symbol.ToString();
                        pieceTexts[row, col].color = piece.IsWhite ? whitePieceColor : blackPieceColor;
                    }
                    else
                    {
                        pieceTexts[row, col].text = "";
                    }
                }
            }

            // Update captured pieces display
            UpdateCapturedPieces();
        }

        void UpdateCapturedPieces()
        {
            var whiteCaptured = new List<char>();
            var blackCaptured = new List<char>();

            // Compare initial pieces with current board state
            var initialWhite = new Dictionary<PieceType, int>
            {
                { PieceType.Queen, 1 }, { PieceType.Rook, 2 },
                { PieceType.Bishop, 2 }, { PieceType.Knight, 2 }, { PieceType.Pawn, 8 }
            };
            var initialBlack = new Dictionary<PieceType, int>(initialWhite);

            var currentWhite = new Dictionary<PieceType, int>();
            var currentBlack = new Dictionary<PieceType, int>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece p = engine.GetPiece(r, c);
                    if (p.Type != PieceType.None && p.Type != PieceType.King)
                    {
                        if (p.IsWhite)
                            currentWhite[p.Type] = currentWhite.ContainsKey(p.Type) ? currentWhite[p.Type] + 1 : 1;
                        else
                            currentBlack[p.Type] = currentBlack.ContainsKey(p.Type) ? currentBlack[p.Type] + 1 : 1;
                    }
                }
            }

            // White captured pieces (black pieces that were taken by white)
            foreach (var kvp in initialBlack)
            {
                int remaining = currentBlack.ContainsKey(kvp.Key) ? currentBlack[kvp.Key] : 0;
                int captured = kvp.Value - remaining;
                for (int i = 0; i < captured; i++)
                {
                    char symbol = new Piece(kvp.Key, PieceColor.Black).Symbol;
                    blackCaptured.Add(symbol);
                }
            }

            // Black captured pieces (white pieces that were taken by black)
            foreach (var kvp in initialWhite)
            {
                int remaining = currentWhite.ContainsKey(kvp.Key) ? currentWhite[kvp.Key] : 0;
                int captured = kvp.Value - remaining;
                for (int i = 0; i < captured; i++)
                {
                    char symbol = new Piece(kvp.Key, PieceColor.White).Symbol;
                    whiteCaptured.Add(symbol);
                }
            }

            capturedWhiteText.text = "Captured by White:\n" + string.Join(" ", blackCaptured);
            capturedBlackText.text = "Captured by Black:\n" + string.Join(" ", whiteCaptured);
        }

        void UpdateStatus()
        {
            string turnLabel = engine.CurrentTurn == PieceColor.White ? "White's Turn" : "Black's Turn";
            string checkLabel = "";

            if (engine.IsInCheck(engine.CurrentTurn))
            {
                checkLabel = " — Check!";
            }

            statusText.text = $"{turnLabel}{checkLabel}";
        }

        void CheckGameOver()
        {
            PieceColor current = engine.CurrentTurn;

            if (engine.IsCheckmate(current))
            {
                string winner = current == PieceColor.White ? "Black" : "White";
                ShowGameOver($"Checkmate!\n{winner} wins!");
            }
            else if (engine.IsStalemate(current))
            {
                ShowGameOver("Stalemate!\nIt's a draw!");
            }
            else if (engine.IsDraw())
            {
                ShowGameOver("Draw!\nInsufficient material.");
            }

            if (!gameOverPanel.activeSelf)
            {
                // Play a move sound effect or animation would go here
            }
        }

        void ShowGameOver(string message)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = message;
        }

        void RestartGame()
        {
            engine = new ChessEngine();
            ClearSelection();
            lastMoveFromRow = -1;
            lastMoveFromCol = -1;
            lastMoveToRow = -1;
            lastMoveToCol = -1;
            gameOverPanel.SetActive(false);
            currentLegalMoves = new List<Move>();
            RefreshBoard();
            UpdateStatus();
        }

        void Update()
        {
            // Handle keyboard shortcuts using Input System
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            if (keyboard.uKey.wasPressedThisFrame && engine.MoveHistory.Count > 0)
            {
                // Undo last move - rebuild engine from history minus last move
                // For simplicity, just restart and replay all moves except last
                UndoLastMove();
            }
        }

        void UndoLastMove()
        {
            if (engine.MoveHistory.Count == 0) return;

            // Rebuild board from initial state, replaying all moves except last
            List<Move> history = new List<Move>(engine.MoveHistory);
            history.RemoveAt(history.Count - 1);

            engine = new ChessEngine();
            foreach (Move move in history)
            {
                engine.MakeMove(move);
            }

            ClearSelection();
            lastMoveFromRow = -1;
            lastMoveFromCol = -1;
            lastMoveToRow = -1;
            lastMoveToCol = -1;
            currentLegalMoves = new List<Move>();
            RefreshBoard();
            UpdateStatus();
            gameOverPanel.SetActive(false);
        }
    }
}
