using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Chess
{
    public struct Position
    {
        public int x;
        public int y;

        public Position(int xPos, int yPos)
        {
            x = xPos;
            y = yPos;
        }

        public override bool Equals(object obj)
        {
            return ((obj is Position) && (this == (Position)obj));
        }

        public override int GetHashCode()
        {
            return (1000 * x) + y;
        }

        public static bool operator ==(Position x, Position y)
        {
            return ((x.x == y.x) && (x.y == y.y));
        }

        public static bool operator !=(Position x, Position y)
        {
            return !(x == y);
        }

        public Position GetOffsetPosition(int xOffset, int yOffset)
        {
            return new Position(x + xOffset, y + yOffset);
        }
    }

    public enum Tilelight
    {
        Select = 0,
        Highlight,
        Threaten
    }

    public struct SimpleTuple
    {
        public object First;
        public object Second;

        public SimpleTuple(object first, object second)
        {
            First = first;
            Second = second;
        }
    }

    public static class GlobalVals
    {
        static public int boardWidth = 7;
        static public int boardHeight = 7;

        static public int HiddenZPos = 1;
        static public int TileZPos = 0;
        static public int PieceZPos = -1;
        static public int TilelightZPos = -2;

        static public string TexturePath_SelectTile = "/Tile/SelTile.png";
        static public string TexturePath_HighlightTile = "/Tile/HighlightTile.png";
        static public string TexturePath_ThreatenTile = "/Tile/ErrTile.png";

        static public bool white = true;
        static public bool black = false;

        static public string TilelightTag = "Tilelight";
        static public string ChecklightTag = "Checklight";
        static public string ControllerTag = "GameController";
    }

    public static class Utils
    {
        public static int ForwardDirection(bool white)
        {
            return white ? 1 : -1;
        }

        public static bool ValidatePositionOnBoard(Position target)
        {
            return (target.x >= 0 && target.y >= 0 && target.x <= GlobalVals.boardWidth && target.y <= GlobalVals.boardHeight);
        }

        public static bool CheckMoveValidity(Tile[][] board, Position target, bool whitePiece, TargetTileType moveType)
        {
            if (!ValidatePositionOnBoard(target))
            {
                return false;
            }

            switch (moveType)
            {
                case TargetTileType.Empty:
                    return (board[target.x][target.y].occupyingPiece == null);
                case TargetTileType.EmptyOrEnemy:
                    return (board[target.x][target.y].occupyingPiece == null || board[target.x][target.y].occupyingPiece.isWhite != whitePiece);
                case TargetTileType.Enemy:
                    return (board[target.x][target.y].occupyingPiece != null && board[target.x][target.y].occupyingPiece.isWhite != whitePiece);
                default:
                    return false;
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            path = Application.dataPath + path;
            Texture2D tex2d;
            byte[] fileData;

            if (File.Exists(path))
            {
                fileData = File.ReadAllBytes(path);
                tex2d = new Texture2D(50, 50); // Create new 50 x 50 texture

                if (tex2d.LoadImage(fileData))
                {
                    return tex2d;
                }
            }

            // Failed to load texture - return null
            return null;
        }

        public static Sprite CreateSprite(string texturePath)
        {
            var texture = LoadTexture(texturePath);

            if (texture == null)
            {
                throw new ArgumentException(string.Format("Could not load texture {0}", texturePath));
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 50);
        }

        /// <summary>
        /// Get the algebraic notation for a position. This position MUST exist on a standard 8x8 chess board
        /// </summary>
        /// <param name="pos">Position to get name/algebraic position for</param>
        /// <returns>String containing name of tile</returns>
        public static string PositionToTileName(Position pos)
        {
            switch (pos.x)
            {
                case 0:
                    return 'a' + pos.y.ToString();
                case 1:
                    return 'b' + pos.y.ToString();
                case 2:
                    return 'c' + pos.y.ToString();
                case 3:
                    return 'd' + pos.y.ToString();
                case 4:
                    return 'e' + pos.y.ToString();
                case 5:
                    return 'f' + pos.y.ToString();
                case 6:
                    return 'g' + pos.y.ToString();
                case 7:
                    return 'h' + pos.y.ToString();
                default:
                    throw new ArgumentException(string.Format("Invalid position: {0}, {1}", pos.x, pos.y));
            }
        }

        /// <summary>
        /// Converts a string which contains an algebraic position for a tile on a standard chess board to a Position
        /// </summary>
        /// <param name="name">Algebraic notation string (eg, "a1" or "A1")</param>
        /// <returns>Position for the passed algebraic notation</returns>
        public static Position TileNameToPosition(string name)
        {
            if (name.Length != 2)
            {
                throw new ArgumentException(name + " is invalid to convert to position");
            }

            switch (name[0])
            {
                case 'a':
                case 'A':
                    return new Position(0, (int)char.GetNumericValue(name[1]));
                case 'b':
                case 'B':
                    return new Position(1, (int)char.GetNumericValue(name[1]));
                case 'c':
                case 'C':
                    return new Position(2, (int)char.GetNumericValue(name[1]));
                case 'd':
                case 'D':
                    return new Position(3, (int)char.GetNumericValue(name[1]));
                case 'e':
                case 'E':
                    return new Position(4, (int)char.GetNumericValue(name[1]));
                case 'f':
                case 'F':
                    return new Position(5, (int)char.GetNumericValue(name[1]));
                case 'g':
                case 'G':
                    return new Position(6, (int)char.GetNumericValue(name[1]));
                case 'h':
                case 'H':
                    return new Position(7, (int)char.GetNumericValue(name[1]));
                default:
                    throw new ArgumentException(name + " is invalid to convert to position");
            }
        }

        public static string GetTexturePath(PieceType type, bool white)
        {
            switch (type)
            {
                case PieceType.Bishop:
                    return white ? "/Pieces/WhiteBishop.png" : "/Pieces/BlackBishop.png";
                case PieceType.King:
                    return white ? "/Pieces/WhiteKing.png" : "/Pieces/BlackKing.png";
                case PieceType.Knight:
                    return white ? "/Pieces/WhiteKnight.png" : "/Pieces/BlackKnight.png";
                case PieceType.Pawn:
                    return white ? "/Pieces/WhitePawn.png" : "/Pieces/BlackPawn.png";
                case PieceType.Queen:
                    return white ? "/Pieces/WhiteQueen.png" : "/Pieces/BlackQueen.png";
                case PieceType.Rook:
                    return white ? "/Pieces/WhiteRook.png" : "/Pieces/BlackRook.png";
                default:
                    throw new ArgumentException("Unrecognized type");
            }
        }

        public static GameObject CreateVisualForPiece(PieceType type, bool white)
        {
            var texture = LoadTexture(GetTexturePath(type, white));

            GameObject ret = new GameObject();
            ret.AddComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(new Vector2(0, 0), new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f));

            return ret;
        }

        public static GameObject CreateTilelight(Tilelight type, Position pos)
        {
            var go = new GameObject();

            var sr = go.AddComponent<SpriteRenderer>();

            switch (type)
            {
                case Tilelight.Select:
                    sr.sprite = CreateSprite(GlobalVals.TexturePath_SelectTile);
                    go.name = "SELECT_" + pos.x.ToString() + '.' + pos.y.ToString();
                    go.tag = GlobalVals.TilelightTag;
                    break;
                case Tilelight.Threaten:
                    sr.sprite = CreateSprite(GlobalVals.TexturePath_ThreatenTile);
                    go.name = "THREATEN_" + pos.x.ToString() + '.' + pos.y.ToString();
                    go.tag = GlobalVals.ChecklightTag;
                    break;
                default:
                    sr.sprite = CreateSprite(GlobalVals.TexturePath_HighlightTile);
                    go.name = "HIGHLIGHT_" + pos.x.ToString() + '.' + pos.y.ToString();
                    go.tag = GlobalVals.TilelightTag;
                    break;
            }

            go.transform.position = new Vector3(pos.x, pos.y, GlobalVals.TilelightZPos);
            
            return go;
        }

        public static void DeleteWithTag(string tag)
        {
            var destroyable = GameObject.FindGameObjectsWithTag(tag);

            for (int x = 0; x < destroyable.Length; ++x)
            {
                GameObject.Destroy(destroyable[x]);
            }
        }

        public static bool IsPosValid(Position pos)
        {
            return (!(pos.x < 0 || pos.x > GlobalVals.boardWidth || pos.y < 0 || pos.y > GlobalVals.boardHeight));
        }
    }

    public class InvalidMoveException : Exception
    {
    }
}
