using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chess
{
    public class Tile
    {
        public string loc { get; set; }
        public Piece occupyingPiece { get; set; }

        public Tile(Position pos)
        {
            loc = ConvertPositionToAlgebraicNotation(pos);
        }

        private string ConvertPositionToAlgebraicNotation(Position pos)
        {
            string loc = string.Empty;

            if (pos.x < 0 || pos.x > 7 || pos.y < 0 || pos.y > 7)
            {
                throw new ArgumentException("Invalid position - both x and y must be between 0 and 7");
            }

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
            }
            
            return "Unknown";
        }
    }
}
