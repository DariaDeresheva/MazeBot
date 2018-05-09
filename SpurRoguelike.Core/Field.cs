using System.Collections.Generic;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.Core
{
    public class Field
    {
        public int VisibilityWidth { get; private set; }
        public int VisibilityHeight { get; private set; }

        public Field(int width, int height, int visibilityWidth, int visibilityHeight)
        {
            VisibilityWidth = visibilityWidth;
            VisibilityHeight = visibilityHeight;
            cells = new CellType[width, height];
        }

        public FieldView CreateView(Location? center)
        {
            return new FieldView(this, center);
        }

        public CellType this[Location index]
        {
            get { return cells[index.X, index.Y]; }
            set
            {
                cells[index.X, index.Y] = value;
                if (value == CellType.PlayerStart)
                    PlayerStart = index;
            }
        }

        public bool Contains(Location location)
        {
            return location.X >= 0 && location.X < Width && location.Y >= 0 && location.Y < Height;
        }

        public IEnumerable<Location> GetCellsOfType(CellType type)
        {
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    if (cells[i, j] == type)
                        yield return new Location(i, j);
        }

        public int Width => cells.GetLength(0);

        public int Height => cells.GetLength(1);

        public Location PlayerStart { get; private set; }
        
        private readonly CellType[,] cells;
    }
}