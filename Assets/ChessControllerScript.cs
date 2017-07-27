using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Chess
{
    public class ChessControllerScript : MonoBehaviour
    {
        public GameObject whiteTile;
        public GameObject blackTile;
        public GameObject checkMateText;
        public Button newGameBtn;
        
        private List<Piece> whitePieces;
        private List<Piece> blackPieces;
        private Tile[][] board;
        private Position? selected;
        private bool whiteTurn;
        private bool acceptInput;

        /// <summary>
        /// Called on creation - set up board
        /// </summary>
        void Start()
        {
            CreateBoard();

            SpawnPieces();

            newGameBtn.onClick.AddListener(ResetGame);

            whiteTurn = true;
            acceptInput = true;
        }

        void Update()
        {
        }

        /// <summary>
        /// Resets the game state for a new game
        /// </summary>
        public void ResetGame()
        {
            foreach (var piece in whitePieces)
            {
                GameObject.Destroy(piece.img);
            }

            whitePieces.Clear();

            foreach (var piece in blackPieces)
            {
                GameObject.Destroy(piece.img);
            }

            blackPieces.Clear();

            for (int x = 0; x <= GlobalVals.boardWidth; ++x)
            {
                for (int y = 0; y <= GlobalVals.boardHeight; ++y)
                {
                    board[x][y].occupyingPiece = null;
                }
            }

            SpawnPieces();

            Utils.DeleteWithTag(GlobalVals.ChecklightTag);
            Utils.DeleteWithTag(GlobalVals.TilelightTag);
            checkMateText.SetActive(false);

            whiteTurn = true;
            acceptInput = true;
        }

        /// <summary>
        /// Callback for the tile buttons. Primary logic here - selects piece, moves piece, etc
        /// </summary>
        /// <param name="clicked">The position of the tile the user clicked</param>
        public void ButtonCallback(Position clicked)
        {
            if (!acceptInput)
            {
                return;
            }

            if (selected.HasValue)
            {
                if (board[selected.Value.x][selected.Value.y].occupyingPiece.CanMoveTo(clicked))
                {
                    var um = board[selected.Value.x][selected.Value.y].occupyingPiece.MoveTo(clicked);

                    try
                    {
                        AdvanceTurn();

                        if (um.recreatePiece != null)
                        {
                            if (um.recreatePiece.isWhite)
                            {
                                whitePieces.Remove(um.recreatePiece);
                            }
                            else
                            {
                                blackPieces.Remove(um.recreatePiece);
                            }

                            GameObject.Destroy(um.recreatePiece.img);
                        }

                        if (um.swapPiece.First != null)
                        {
                            GameObject.Destroy(((Piece)um.swapPiece.First).img);
                        }
                    }
                    catch (InvalidMoveException)
                    {
                        um.Execute(board);
                        CalculateAllMoves(); // Undo Move uses SetPosition to reposition pieces, so can result in some pieces' calculated
                                             // moves being wrong
                    }
                    finally
                    {
                        selected = null;
                        Utils.DeleteWithTag(GlobalVals.TilelightTag);
                    }
                }
                else if (board[clicked.x][clicked.y].occupyingPiece != null)
                {
                    selected = clicked;
                    board[clicked.x][clicked.y].occupyingPiece.Select();

                    Utils.DeleteWithTag(GlobalVals.TilelightTag);
                    foreach (var loc in board[clicked.x][clicked.y].occupyingPiece.Moves())
                    {
                        Utils.CreateTilelight(Tilelight.Highlight, loc);
                    }

                }
                else
                {
                    selected = null;
                    Utils.DeleteWithTag(GlobalVals.TilelightTag);
                }
            }
            else if (board[clicked.x][clicked.y].occupyingPiece != null && board[clicked.x][clicked.y].occupyingPiece.isWhite == whiteTurn)
            {
                selected = clicked;
                Utils.CreateTilelight(Tilelight.Select, clicked);
                board[clicked.x][clicked.y].occupyingPiece.Select();

                foreach (var loc in board[clicked.x][clicked.y].occupyingPiece.Moves())
                {
                    Utils.CreateTilelight(Tilelight.Highlight, loc);
                }
            }
        }

        public void SwapPieces(Piece oldPiece, Piece newPiece)
        {
            if (oldPiece.isWhite)
            {
                whitePieces.Remove(oldPiece);
                whitePieces.Add(newPiece);
            }
            else
            {
                blackPieces.Remove(oldPiece);
                blackPieces.Add(newPiece);
            }
        }

        /// <summary>
        /// Creates all the tile buttons. Called on Start only.
        /// </summary>
        private void CreateBoard()
        {
            board = new Tile[8][];
            for (int x = 0; x < board.Length; ++x)
            {
                board[x] = new Tile[8];

                for (int y = 0; y <= GlobalVals.boardWidth; ++y)
                {
                    board[x][y] = new Tile(new Position(x, y));

                    // Spawn the GameObject for this tile - visual component and acts as a button of sort that calls back in to
                    // controller when clicked on

                    if (x % 2 == 0)
                    {
                        if (y % 2 == 0)
                        {
                            var newTile = Instantiate(blackTile);
                            newTile.transform.position = new Vector3(x, y, 0);
                            newTile.name = board[x][y].loc;
                        }
                        else
                        {
                            var newTile = Instantiate(whiteTile);
                            newTile.transform.position = new Vector3(x, y, 0);
                            newTile.name = board[x][y].loc;
                        }
                    }
                    else
                    {
                        if (y % 2 == 0)
                        {
                            var newTile = Instantiate(whiteTile);
                            newTile.transform.position = new Vector3(x, y, 0);
                            newTile.name = board[x][y].loc;
                        }
                        else
                        {
                            var newTile = Instantiate(blackTile);
                            newTile.transform.position = new Vector3(x, y, 0);
                            newTile.name = board[x][y].loc;
                        }
                    }
                }
            }
            //
        }

        /// <summary>
        /// Creates all the pieces for a new game, adds them to the board, and calculates their initial moves
        /// </summary>
        private void SpawnPieces()
        {
            whitePieces = new List<Piece>();
            blackPieces = new List<Piece>();

            // spawn pawns
            for (int x = 0; x <= GlobalVals.boardWidth; ++x)
            {
                board[x][1].occupyingPiece = new Pawn(true, new Position(x, 1), board);
                whitePieces.Add(board[x][1].occupyingPiece);

                board[x][6].occupyingPiece = new Pawn(false, new Position(x, 6), board);
                blackPieces.Add(board[x][6].occupyingPiece);
            }

            board[0][0].occupyingPiece = new Rook(true, new Position(0, 0), board);
            whitePieces.Add(board[0][0].occupyingPiece);
            board[1][0].occupyingPiece = new Knight(true, new Position(1, 0), board);
            whitePieces.Add(board[1][0].occupyingPiece);
            board[2][0].occupyingPiece = new Bishop(true, new Position(2, 0), board);
            whitePieces.Add(board[2][0].occupyingPiece);
            board[3][0].occupyingPiece = new Queen(true, new Position(3, 0), board);
            whitePieces.Add(board[3][0].occupyingPiece);
            board[4][0].occupyingPiece = new King(true, new Position(4, 0), board);
            whitePieces.Add(board[4][0].occupyingPiece);
            board[5][0].occupyingPiece = new Bishop(true, new Position(5, 0), board);
            whitePieces.Add(board[5][0].occupyingPiece);
            board[6][0].occupyingPiece = new Knight(true, new Position(6, 0), board);
            whitePieces.Add(board[6][0].occupyingPiece);
            board[7][0].occupyingPiece = new Rook(true, new Position(7, 0), board);
            whitePieces.Add(board[7][0].occupyingPiece);

            board[0][7].occupyingPiece = new Rook(false, new Position(0, 7), board);
            blackPieces.Add(board[0][7].occupyingPiece);
            board[1][7].occupyingPiece = new Knight(false, new Position(1, 7), board);
            blackPieces.Add(board[1][7].occupyingPiece);
            board[2][7].occupyingPiece = new Bishop(false, new Position(2, 7), board);
            blackPieces.Add(board[2][7].occupyingPiece);
            board[3][7].occupyingPiece = new Queen(false, new Position(3, 7), board);
            blackPieces.Add(board[3][7].occupyingPiece);
            board[4][7].occupyingPiece = new King(false, new Position(4, 7), board);
            blackPieces.Add(board[4][7].occupyingPiece);
            board[5][7].occupyingPiece = new Bishop(false, new Position(5, 7), board);
            blackPieces.Add(board[5][7].occupyingPiece);
            board[6][7].occupyingPiece = new Knight(false, new Position(6, 7), board);
            blackPieces.Add(board[6][7].occupyingPiece);
            board[7][7].occupyingPiece = new Rook(false, new Position(7, 7), board);
            blackPieces.Add(board[7][7].occupyingPiece);

            CalculateAllMoves();
        }

        /// <summary>
        /// Advances turn (white -> black -> white) if it is a valid game state to switch game states. Throws InvalidMoveException if
        /// called while the current player is in check. 
        /// </summary>
        private void AdvanceTurn()
        {
            if (whiteTurn && CheckForCheck(true))
            {
                throw new InvalidMoveException();
            }
            else if (!whiteTurn && CheckForCheck(false))
            {
                throw new InvalidMoveException();
            }

            Utils.DeleteWithTag(GlobalVals.ChecklightTag);
            whiteTurn = !whiteTurn;

            if (whiteTurn && CheckForCheck(true))
            {
                Utils.CreateTilelight(Tilelight.Threaten, whitePieces.Where(x => x.checkTarget).First().position);

                if (CheckForCheckmate(true))
                {
                    checkMateText.SetActive(true);
                    acceptInput = false;
                }

                CalculateAllMoves();
            }
            else if (!whiteTurn && CheckForCheck(false))
            {
                Utils.CreateTilelight(Tilelight.Threaten, blackPieces.Where(x => x.checkTarget).First().position);

                if (CheckForCheckmate(false))
                {
                    checkMateText.SetActive(true);
                    acceptInput = false;
                }

                CalculateAllMoves();
            }
        }

        /// <summary>
        /// Cause all pieces to recalculate their potential moves
        /// </summary>
        private void CalculateAllMoves()
        {
            foreach (var piece in whitePieces)
            {
                piece.CalculateMoves();
            }

            foreach (var piece in blackPieces)
            {
                piece.CalculateMoves();
            }
        }

        /// <summary>
        /// Checks whether the specified side is currently target of check
        /// </summary>
        /// <param name="againstWhite">True if checking for check against white; false for black</param>
        /// <returns>Whether specified side is under check</returns>
        private bool CheckForCheck(bool againstWhite)
        {
            if (againstWhite)
            {
                foreach (var piece in blackPieces)
                {
                    if (piece.ThreatensCheck())
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var piece in whitePieces)
                {
                    if (piece.ThreatensCheck())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the specified side is in a losing/checkmate state
        /// </summary>
        /// <param name="againstWhite">Which side to check for checkmate (true checks for checkmate against white)</param>
        /// <returns>Whether the specified side has has lost</returns>
        private bool CheckForCheckmate(bool againstWhite)
        {
            if (againstWhite)
            {
                foreach (var piece in whitePieces)
                {
                    if (piece.CanBlockCheck(blackPieces))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var piece in blackPieces)
                {
                    if (piece.CanBlockCheck(whitePieces))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }  
}