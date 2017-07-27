using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Chess
{
    public enum PieceType
    {
        King = 0,
        Queen,
        Bishop,
        Knight,
        Rook,
        Pawn
    }

    public enum TargetTileType
    {
        EmptyOrEnemy = 0,
        Empty,
        Enemy
    }

    public class Piece
    {
        public Position position { get; set; }
        public bool isWhite { get; set; }
        public PieceType type { get; set; }
        public GameObject img { get; set; }
        public bool checkTarget { get; set; }
        public bool hasMoved { get; set; }
        public Tile[][] board;

        // List of positions this piece could move to which contain a piece. If the position contains an enemy piece, this piece can move
        // to that position and take it. If the piece occupying the position is allied, 
        protected List<Position> threatens;
        protected List<Position> possibleMoves;

        public Piece(bool white, Position pos, Tile[][] gameBoard)
        {
            position = pos;
            isWhite = white;
            checkTarget = false;
            hasMoved = false;
            board = gameBoard;

            threatens = new List<Position>();

            // List of the positions this piece can move to
            possibleMoves = new List<Position>();
        }

        public override bool Equals(object obj)
        {
            return ((obj is Piece) && (this == (Piece)obj));
        }

        public static bool operator ==(Piece a, Piece b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return ReferenceEquals(a, null) && ReferenceEquals(b, null);
            }

            return (a.GetType() == b.GetType() &&
                    a.type == b.type &&
                    a.position == b.position &&
                    a.isWhite == b.isWhite);
        }

        public static bool operator !=(Piece a, Piece b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Call this on selecting a piece
        /// </summary>
        public void Select()
        {
            CalculateMoves();
        }

        /// <summary>
        /// Moves the piece to specified position without any intelligence (doesn't update board, recalculate any piece's moves, etc)
        /// </summary>
        /// <param name="pos"></param>
        public void SetPosition(Position pos)
        {
            position = pos;
            img.transform.position = new Vector3(pos.x, pos.y, GlobalVals.PieceZPos);
        }

        /// <summary>
        /// Checks whether a position is in this piece's available moves
        /// </summary>
        /// <param name="pos">Position to move to</param>
        /// <returns>Whether pos is in the piece's possible moves</returns>
        public bool CanMoveTo(Position pos)
        {
            return possibleMoves.Contains(pos);
        }

        public bool Threatens(Position pos)
        {
            return threatens.Contains(pos);
        }

        /// <summary>
        /// Returns the list of possible moves for this piece
        /// </summary>
        /// <returns>Returns the list of possible moves for this piece</returns>
        public List<Position> Moves()
        {
            return possibleMoves;
        }

        /// <summary>
        /// Check whether or not this piece threatens check on enemy team
        /// </summary>
        /// <returns>Whether this piece currently threatens the enemy king</returns>
        public bool ThreatensCheck()
        {
            return threatens.Any(x =>
                                    board[x.x][x.y].occupyingPiece != null &&
                                    board[x.x][x.y].occupyingPiece.checkTarget &&
                                    board[x.x][x.y].occupyingPiece.isWhite != isWhite);
        }

        /// <summary>
        /// Enable or disable the visual/GameObject for this piece
        /// </summary>
        /// <param name="enabled">True for enable; false for disable</param>
        public void EnableVisual(bool enabled)
        {
            img.SetActive(enabled);
        }

        /// <summary>
        /// Move a piece to a position on the board with logic
        /// </summary>
        /// <param name="pos">Position to move to</param>
        /// <returns>UndoMove to undo this move</returns>
        public virtual UndoMove MoveTo(Position pos)
        {
            var toUndo = new UndoMove();
            var vacating = position;
            board[vacating.x][vacating.y].occupyingPiece = null;
            position = pos;

            if (board[position.x][position.y].occupyingPiece != null)
            {
                toUndo.recreatePiece = board[position.x][position.y].occupyingPiece;
                toUndo.recreatePiece.EnableVisual(false);
            }

            board[position.x][position.y].occupyingPiece = this;

            toUndo.pieceMoves.Add(new SimpleTuple(this, vacating));

            img.transform.position = new Vector3(pos.x, pos.y, GlobalVals.PieceZPos);

            if (!hasMoved)
            {
                toUndo.resetHasMoved.Add(this);
                hasMoved = true;
            }

            for (int x = 0; x <= GlobalVals.boardWidth; ++x)
            {
                for (int y = 0; y <= GlobalVals.boardHeight; ++y)
                {
                    if (board[x][y].occupyingPiece != null && 
                        (board[x][y].occupyingPiece.threatens.Contains(vacating) || board[x][y].occupyingPiece.threatens.Contains(position)))
                    {
                        // this piece got unblocked - recalculate its moves
                        board[x][y].occupyingPiece.CalculateMoves();
                    }
                }
            }

            CalculateMoves();

            return toUndo;
        }

        /// <summary>
        /// Calculate the current possible moves for this piece. This function should be overridden by subclass
        /// </summary>
        public virtual void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();
        }

        /// <summary>
        /// Calculates a recursive move and adds all potential moves generated to this piece's possibleMoves and threatens
        /// </summary>
        /// <param name="xMove">X offset for the move</param>
        /// <param name="yMove">Y offset for the move</param>
        /// <param name="validTarget">Valid tile types for this move</param>
        protected void CalculateRecursiveMove(int xMove, int yMove, TargetTileType validTarget)
        {
            var pos = position.GetOffsetPosition(xMove, yMove);

            while (Utils.CheckMoveValidity(
                                board,
                                pos,
                                isWhite,
                                validTarget))
            {
                if (board[pos.x][pos.y].occupyingPiece != null)
                {
                    break;
                }

                possibleMoves.Add(pos);
                threatens.Add(pos);

                pos = pos.GetOffsetPosition(xMove, yMove);
            }

            if (Utils.ValidatePositionOnBoard(pos) && board[pos.x][pos.y].occupyingPiece != null)
            {
                threatens.Add(pos);

                if (validTarget != TargetTileType.Empty && board[pos.x][pos.y].occupyingPiece.isWhite != isWhite)
                {
                    possibleMoves.Add(pos);
                }
            }
        }

        /// <summary>
        /// Adds a position to this piece's possibleMoves/threatens based on the target's state and the passed TargetTileType
        /// </summary>
        /// <param name="target">Position to add</param>
        /// <param name="valid">State(s) for which to add the target position</param>
        protected void AddTargetIfValid(Position target, TargetTileType valid)
        {
            if (!Utils.ValidatePositionOnBoard(target))
            {
                return; 
            }

            switch (valid)
            {
                case TargetTileType.Empty:
                    if (board[target.x][target.y].occupyingPiece == null)
                    {
                        possibleMoves.Add(target);
                    }
                    break;
                case TargetTileType.EmptyOrEnemy:
                    if (board[target.x][target.y].occupyingPiece == null)
                    {
                        possibleMoves.Add(target);
                    }
                    else
                    {
                        threatens.Add(target);

                        if (board[target.x][target.y].occupyingPiece.isWhite != isWhite)
                        {
                            possibleMoves.Add(target);
                        }
                    }
                    break;
                case TargetTileType.Enemy:
                    if (board[target.x][target.y].occupyingPiece != null && board[target.x][target.y].occupyingPiece.isWhite != isWhite)
                    {
                        possibleMoves.Add(target);
                        threatens.Add(target);
                    }
                    else
                    {
                        threatens.Add(target);
                    }
                    break;
            }
        }

        /// <summary>
        /// Check whether or not this piece has a move that will result in its king not being in check
        /// </summary>
        /// <param name="enemyPieces">List of enemy pieces</param>
        /// <returns>True iff this piece has a move that can result in no check on its king</returns>
        public bool CanBlockCheck(List<Piece> enemyPieces)
        {
            var origPos = position;
            bool canBlock = false;
            CalculateMoves();
            board[position.x][position.y].occupyingPiece = null;

            foreach (var move in possibleMoves)
            {
                var holdPiece = board[move.x][move.y].occupyingPiece;
                board[move.x][move.y].occupyingPiece = this;
                bool threatFound = false;

                foreach (var piece in enemyPieces.Where(x => x.position !=  move))
                {
                    piece.CalculateMoves();

                    if (piece.ThreatensCheck())
                    {
                        threatFound = true;
                        break;
                    }
                }

                board[move.x][move.y].occupyingPiece = holdPiece;

                if (!threatFound)
                {
                    canBlock = true;
                    break;
                }
            }

            board[position.x][position.y].occupyingPiece = this;
            return canBlock;
        }

        public void DestroyVisual()
        {
            GameObject.Destroy(img);
        }
    }

    public class Pawn : Piece
    {
        public Pawn(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.Pawn;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "Pawn" + (white ? "_White" : "_Black");

            SetPosition(pos);
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            AddTargetIfValid(position.GetOffsetPosition(0, Utils.ForwardDirection(isWhite)), TargetTileType.Empty);

            if (possibleMoves.Any() && !hasMoved)
            {
                AddTargetIfValid(position.GetOffsetPosition(0, 2 * Utils.ForwardDirection(isWhite)), TargetTileType.Empty);
            }

            AddTargetIfValid(position.GetOffsetPosition(1, Utils.ForwardDirection(isWhite)), TargetTileType.Enemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, Utils.ForwardDirection(isWhite)), TargetTileType.Enemy);
        }

        public override UndoMove MoveTo(Position pos)
        {
            var um = base.MoveTo(pos);

            if ((isWhite && position.y == GlobalVals.boardHeight) ||
                (!isWhite && position.y == 0))
            {
                board[position.x][position.y].occupyingPiece = new Queen(isWhite, position, board);
                board[position.x][position.y].occupyingPiece.CalculateMoves();

                GameObject.FindGameObjectWithTag(GlobalVals.ControllerTag).GetComponent<ChessControllerScript>()
                                                                            .SwapPieces(this, board[position.x][position.y].occupyingPiece);
                um.swapPiece = new SimpleTuple(this, board[position.x][position.y].occupyingPiece);
                EnableVisual(false);
            }

            return um;
        }
    }

    public class Rook : Piece
    {
        public Rook(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.Rook;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "Rook" + (white ? "_White" : "_Black");

            SetPosition(pos);
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            CalculateRecursiveMove(1, 0, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, 0, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(0, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(0, -1, TargetTileType.EmptyOrEnemy);
        }
    }

    public class Knight : Piece
    {
        public Knight(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.Knight;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "Knight" + (white ? "_White" : "_Black");

            SetPosition(pos);
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            AddTargetIfValid(position.GetOffsetPosition(1, 2), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(1, -2), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, 2), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, -2), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(2, 1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(2, -1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-2, 1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-2, -1), TargetTileType.EmptyOrEnemy);
        }
    }

    public class Bishop : Piece
    {
        public Bishop(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.Bishop;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "Bishop" + (white ? "_White" : "_Black");

            SetPosition(pos);
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            CalculateRecursiveMove(1, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(1, -1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, -1, TargetTileType.EmptyOrEnemy);
        }
    }

    public class King : Piece
    {
        public King(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.King;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "King" + (white ? "_White" : "_Black");

            SetPosition(pos);

            checkTarget = true;
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            AddTargetIfValid(position.GetOffsetPosition(1, 1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(1, 0), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(1, -1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(0, 1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(0, -1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, 1), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, 0), TargetTileType.EmptyOrEnemy);
            AddTargetIfValid(position.GetOffsetPosition(-1, -1), TargetTileType.EmptyOrEnemy);

            if (!hasMoved)
            {
                List<Piece> enemies = new List<Piece>();

                for (int x = 0; x <= GlobalVals.boardWidth; ++x)
                {
                    for (int y = 0; y <= GlobalVals.boardHeight; ++y)
                    {
                        if (board[x][y].occupyingPiece != null && board[x][y].occupyingPiece.isWhite != isWhite)
                        {
                            enemies.Add(board[x][y].occupyingPiece);
                        }
                    }
                }

                var rookPos = new Position(GlobalVals.boardWidth, position.y);
                int loopVar;

                for (loopVar = position.x + 1; loopVar < rookPos.x; ++loopVar)
                {
                    if (board[loopVar][position.y].occupyingPiece != null)
                    {
                        break;
                    }
                }

                if (loopVar == rookPos.x 
                    && board[rookPos.x][rookPos.y].occupyingPiece != null 
                    && !board[rookPos.x][rookPos.y].occupyingPiece.hasMoved
                    && !enemies.Any(piece => piece.Threatens(new Position(position.x + 1, position.y))))
                {
                    possibleMoves.Add(position.GetOffsetPosition(2, 0));
                }

                rookPos = new Position(0, position.y);
                for (loopVar = position.x - 1; loopVar > rookPos.x; --loopVar)
                {
                    if (board[loopVar][position.y].occupyingPiece != null)
                    {
                        break;
                    }
                }

                if (loopVar == rookPos.x 
                    && board[rookPos.x][rookPos.y].occupyingPiece != null 
                    && !board[rookPos.x][rookPos.y].occupyingPiece.hasMoved
                    && !enemies.Any(piece => piece.Threatens(new Position(position.x - 1, position.y))))
                {
                    possibleMoves.Add(position.GetOffsetPosition(-2, 0));
                }

                foreach (var enemy in enemies)
                {
                    possibleMoves = possibleMoves.Where(move => !(enemy.Threatens(move))).ToList();
                }
            }
        }

        //// Overridden to handle castling
        public override UndoMove MoveTo(Position pos)
        {
            UndoMove um = new UndoMove();

            if (pos.x == position.x + 2)
            {
                // castling right
                um.pieceMoves.Add(new SimpleTuple(board[GlobalVals.boardWidth][position.y].occupyingPiece, 
                                                new Position(GlobalVals.boardWidth, position.y)));
                board[GlobalVals.boardWidth][position.y].occupyingPiece.MoveTo(pos.GetOffsetPosition(-1, 0));
            }
            else if (pos.x == position.x - 2)
            {
                // castling left
                um.pieceMoves.Add(new SimpleTuple(board[0][position.y].occupyingPiece, new Position(0, position.y)));
                board[0][position.y].occupyingPiece.MoveTo(pos.GetOffsetPosition(1, 0));
            }

            return base.MoveTo(pos);
        }
    }

    public class Queen : Piece
    {
        public Queen(bool white, Position pos, Tile[][] gameBoard)
            : base(white, pos, gameBoard)
        {
            type = PieceType.Queen;

            img = Utils.CreateVisualForPiece(type, isWhite);
            img.name = "Queen" + (white ? "_White" : "_Black");

            SetPosition(pos);
        }

        public override void CalculateMoves()
        {
            possibleMoves = new List<Position>();
            threatens = new List<Position>();

            CalculateRecursiveMove(1, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(1, 0, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(1, -1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(0, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(0, -1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, 1, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, 0, TargetTileType.EmptyOrEnemy);
            CalculateRecursiveMove(-1, -1, TargetTileType.EmptyOrEnemy);
        }
    }
}
