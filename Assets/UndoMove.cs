using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class UndoMove
    {
        public List<SimpleTuple> pieceMoves { get; set; }
        public List<Piece> resetHasMoved { get; set; }
        public Piece recreatePiece { get; set; }
        public SimpleTuple swapPiece { get; set;}

        public UndoMove()
        {
            pieceMoves = new List<SimpleTuple>();
            resetHasMoved = new List<Piece>();
            recreatePiece = null;
            swapPiece = new SimpleTuple(null, null);
        }

        public UndoMove(List<SimpleTuple> moves, List<Piece> resets, Piece recreate)
        {
            pieceMoves = moves;
            resetHasMoved = resets;
            recreatePiece = recreate;
            swapPiece = new SimpleTuple(null, null);
        }

        public void CombineWith(UndoMove um)
        {
            pieceMoves.AddRange(um.pieceMoves);
            resetHasMoved.AddRange(um.resetHasMoved);
            if (recreatePiece == null)
            {
                recreatePiece = um.recreatePiece;
            }
        }

        public void Execute(Tile[][] board)
        {
            foreach (var move in pieceMoves)
            {
                ((Piece)move.First).SetPosition((Position)move.Second);
                board[((Position)move.Second).x][((Position)move.Second).y].occupyingPiece = (Piece)move.First;
            }

            foreach (var piece in resetHasMoved)
            {
                piece.hasMoved = false;
            }

            if (recreatePiece != null)
            {
                recreatePiece.EnableVisual(true);
                board[recreatePiece.position.x][recreatePiece.position.y].occupyingPiece = recreatePiece;
            }

            if (swapPiece.First != null && swapPiece.Second != null)
            {
                GameObject.FindGameObjectWithTag(GlobalVals.ControllerTag).GetComponent<ChessControllerScript>()
                                                    .SwapPieces((Piece)swapPiece.Second, (Piece)swapPiece.First);
            }
        }
    }
}