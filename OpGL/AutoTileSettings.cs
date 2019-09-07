using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    class AutoTileSettings
    {
        public string Name;
        private SortedList<int, Point> tiles = new SortedList<int, Point>();
        public Point Origin { get; private set; }
        public int Size { get; private set; }
        private static bool checkBit(int index, int data)
        {
            return (data & (1 << index)) != 0;
        }
        public AutoTileSettings(Point defaultTile)
        {
            tiles.Add(0, defaultTile);
            Size = 1;
            Origin = defaultTile;
        }
        private void setTile(int index, Point tile)
        {
            if (tiles.ContainsKey(index))
                tiles[index] = tile;
            else
                tiles.Add(index, tile);
        }
        public static AutoTileSettings Default13(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 1; i < 256; i++)
            {
                if (!checkBit(0, i))
                {
                    if (!checkBit(3, i))
                    {
                        if (!checkBit(1, i))
                        {
                            if (!checkBit(3, i))
                                ret.setTile(i, new Point(originX, originY));
                            else
                                ret.setTile(i, new Point(originX + 2, originY + 2));
                        }
                        else
                            ret.setTile(i, new Point(originX, originY + 2));
                    }
                    else if (!checkBit(1, i))
                        ret.setTile(i, new Point(originX + 2, originY + 2));
                    else
                        ret.setTile(i, new Point(originX + 1, originY + 2));
                }
                else if (!checkBit(2, i))
                {
                    if (!checkBit(3, i))
                        ret.setTile(i, new Point(originX, originY + 4));
                    else if (!checkBit(1, i))
                        ret.setTile(i, new Point(originX + 2, originY + 4));
                    else
                        ret.setTile(i, new Point(originX + 1, originY + 4));
                }
                else if (!checkBit(3, i))
                {
                    ret.setTile(i, new Point(originX, originY + 3));
                }
                else if (!checkBit(1, i))
                {
                    ret.setTile(i, new Point(originX + 2, originY + 3));
                }
                else
                {
                    if (!checkBit(7, i))
                        ret.setTile(i, new Point(originX + 2, originY + 1));
                    else if (!checkBit(4, i))
                        ret.setTile(i, new Point(originX + 1, originY + 1));
                    else if (!checkBit(6, i))
                        ret.setTile(i, new Point(originX + 2, originY));
                    else if (!checkBit(5, i))
                        ret.setTile(i, new Point(originX + 1, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
            }
            ret.Size = 13;
            return ret;
        }

        public static AutoTileSettings Default47(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 0; i < 256; i++)
            {
                if (!checkBit(0, i))
                {
                    if (!checkBit(2, i))
                    {
                        if (!checkBit(1, i))
                        {
                            if (!checkBit(3, i))
                                ret.setTile(i, new Point(originX, originY));
                            else
                                ret.setTile(i, new Point(originX + 3, originY));
                        }
                        else if (!checkBit(3, i))
                            ret.setTile(i, new Point(originX + 1, originY));
                        else
                            ret.setTile(i, new Point(originX + 2, originY));
                    }
                    else if (!checkBit(3, i))
                    {
                        if (!checkBit(1, i))
                            ret.setTile(i, new Point(originX, originY + 1));
                        else
                        {
                            if (!checkBit(5, i))
                                ret.setTile(i, new Point(originX + 6, originY));
                            else
                                ret.setTile(i, new Point(originX + 1, originY + 1));
                        }
                    }
                    else if (!checkBit(1, i))
                    {
                        if (!checkBit(6, i))
                            ret.setTile(i, new Point(originX + 7, originY));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 1));
                    }
                    else if (!checkBit(5, i))
                    {
                        if (!checkBit(6, i))
                            ret.setTile(i, new Point(originX + 6, originY + 4));
                        else
                            ret.setTile(i, new Point(originX + 4, originY + 2));
                    }
                    else if (!checkBit(6, i))
                        ret.setTile(i, new Point(originX + 5, originY + 2));
                    else
                        ret.setTile(i, new Point(originX + 2, originY + 1));
                }
                else if (!checkBit(2, i))
                {
                    if (!checkBit(1, i))
                    {
                        if (!checkBit(3, i))
                            ret.setTile(i, new Point(originX, originY + 3));
                        else if (!checkBit(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 1));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 3));
                    }
                    else if (!checkBit(3, i))
                    {
                        if (!checkBit(4, i))
                            ret.setTile(i, new Point(originX + 6, originY + 1));
                        else
                            ret.setTile(i, new Point(originX + 1, originY + 3));
                    }
                    else if (!checkBit(4, i))
                    {
                        if (!checkBit(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 5));
                        else
                            ret.setTile(i, new Point(originX + 4, originY + 3));
                    }
                    else if (!checkBit(7, i))
                        ret.setTile(i, new Point(originX + 5, originY + 3));
                    else
                        ret.setTile(i, new Point(originX + 2, originY + 3));
                }
                else if (!checkBit(3, i))
                {
                    if (!checkBit(1, i))
                        ret.setTile(i, new Point(originX, originY + 2));
                    else
                    {
                        if (!checkBit(4, i))
                        {
                            if (!checkBit(5, i))
                                ret.setTile(i, new Point(originX + 6, originY + 5));
                            else
                                ret.setTile(i, new Point(originX + 6, originY + 3));
                        }
                        else
                        {
                            if (!checkBit(5, i))
                                ret.setTile(i, new Point(originX + 6, originY + 2));
                            else
                                ret.setTile(i, new Point(originX + 1, originY + 2));
                        }
                    }
                }
                else if (!checkBit(1, i))
                {
                    if (!checkBit(6, i))
                    {
                        if (!checkBit(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 4));
                        else
                            ret.setTile(i, new Point(originX + 7, originY + 2));
                    }
                    else
                    {
                        if (!checkBit(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 3));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 2));
                    }
                }
                else
                {
                    if (!checkBit(4, i))
                    {
                        if (!checkBit(5, i))
                        {
                            if (!checkBit(6, i))
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 1, originY + 5));
                            }
                            else
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 1, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY + 4));
                            }
                        }
                        else
                        {
                            if (!checkBit(6, i))
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 2, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 5, originY + 5));
                            }
                            else
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 3, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY + 1));
                            }
                        }
                    }
                    else
                    {
                        if (!checkBit(5, i))
                        {
                            if (!checkBit(6, i))
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 2, originY + 5));
                                else
                                    ret.setTile(i, new Point(originX + 3, originY + 5));
                            }
                            else
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 5, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY));
                            }
                        }
                        else
                        {
                            if (!checkBit(6, i))
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 4, originY + 5));
                                else
                                    ret.setTile(i, new Point(originX + 5, originY));
                            }
                            else
                            {
                                if (!checkBit(7, i))
                                    ret.setTile(i, new Point(originX + 5, originY + 1));
                                else
                                    ret.setTile(i, new Point(originX + 2, originY + 2));
                            }
                        }
                    }
                }
            }
            ret.Size = 47;
            return ret;
        }

        public static AutoTileSettings Default3(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 0; i < 256; i++)
            {
                if (checkBit(0, i) || checkBit(2, i))
                {
                    if (checkBit(1, i) || checkBit(3, i))
                        ret.setTile(i, new Point(originX + 2, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
                else
                {
                    if (checkBit(1, i) || checkBit(3, i))
                        ret.setTile(i, new Point(originX + 1, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
            }
            ret.Size = 3;
            return ret;
        }

        public static AutoTileSettings Default4(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 0; i < 256; i++)
            {
                if (checkBit(2, i))
                    ret.setTile(i, new Point(originX, originY));
                else if (checkBit(0, i))
                    ret.setTile(i, new Point(originX + 1, originY));
                else if (checkBit(3, i))
                    ret.setTile(i, new Point(originX + 2, originY));
                else if (checkBit(1, i))
                    ret.setTile(i, new Point(originX + 3, originY));
            }
            ret.Size = 4;
            return ret;
        }

        private Point GetTile(int data)
        {
            return tiles[data];
        }

        private Point[] tileOrder => new Point[] { new Point(0, -8), new Point(8, 0), new Point(0, 8), new Point(-8, 0), new Point(8, -8), new Point(8, 8), new Point(-8, 8), new Point(-8, -8) };

        public Point GetTile(Predicate<Point> predicate)
        {
            int dat = 0;
            int increment = 1;
            for (int i = 0; i < 8; i++)
            {
                if (predicate(tileOrder[i]))
                {
                    dat += increment;
                }
                increment *= 2;
            }
            return GetTile(dat);
        }
    }
}
