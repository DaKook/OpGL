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
        private static bool isTile(int index, int data)
        {
            return (data & (1 << index)) != 0;
        }
        public AutoTileSettings(Point defaultTile)
        {
            tiles.Add(0, defaultTile);
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
                if (!isTile(0, i))
                {
                    if (!isTile(3, i))
                        ret.setTile(i, new Point(originX, originY + 2));
                    else if (!isTile(1, i))
                        ret.setTile(i, new Point(originX + 2, originY + 2));
                    else
                        ret.setTile(i, new Point(originX + 1, originY + 2));
                }
                else if (!isTile(2, i))
                {
                    if (!isTile(3, i))
                        ret.setTile(i, new Point(originX, originY + 4));
                    else if (!isTile(1, i))
                        ret.setTile(i, new Point(originX + 2, originY + 4));
                    else
                        ret.setTile(i, new Point(originX + 1, originY + 4));
                }
                else if (!isTile(3, i))
                {
                    ret.setTile(i, new Point(originX, originY + 3));
                }
                else if (!isTile(1, i))
                {
                    ret.setTile(i, new Point(originX + 2, originY + 3));
                }
                else
                {
                    if (!isTile(7, i))
                        ret.setTile(i, new Point(originX + 2, originY + 1));
                    else if (!isTile(4, i))
                        ret.setTile(i, new Point(originX + 1, originY + 1));
                    else if (!isTile(6, i))
                        ret.setTile(i, new Point(originX + 2, originY));
                    else if (!isTile(5, i))
                        ret.setTile(i, new Point(originX + 1, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
            }
            return ret;
        }

        public static AutoTileSettings Default47(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 0; i < 256; i++)
            {
                if (!isTile(0, i))
                {
                    if (!isTile(2, i))
                    {
                        if (!isTile(1, i))
                        {
                            if (!isTile(3, i))
                                ret.setTile(i, new Point(originX, originY));
                            else
                                ret.setTile(i, new Point(originX + 3, originY));
                        }
                        else if (!isTile(3, i))
                            ret.setTile(i, new Point(originX + 1, originY));
                        else
                            ret.setTile(i, new Point(originX + 2, originY));
                    }
                    else if (!isTile(3, i))
                    {
                        if (!isTile(1, i))
                            ret.setTile(i, new Point(originX, originY + 1));
                        else
                        {
                            if (!isTile(5, i))
                                ret.setTile(i, new Point(originX + 6, originY));
                            else
                                ret.setTile(i, new Point(originX + 1, originY + 1));
                        }
                    }
                    else if (!isTile(1, i))
                    {
                        if (!isTile(6, i))
                            ret.setTile(i, new Point(originX + 7, originY));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 1));
                    }
                    else if (!isTile(5, i))
                    {
                        if (!isTile(6, i))
                            ret.setTile(i, new Point(originX + 6, originY + 4));
                        else
                            ret.setTile(i, new Point(originX + 4, originY + 2));
                    }
                    else if (!isTile(6, i))
                        ret.setTile(i, new Point(originX + 5, originY + 2));
                    else
                        ret.setTile(i, new Point(originX + 2, originY + 1));
                }
                else if (!isTile(2, i))
                {
                    if (!isTile(1, i))
                    {
                        if (!isTile(3, i))
                            ret.setTile(i, new Point(originX, originY + 3));
                        else if (!isTile(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 1));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 3));
                    }
                    else if (!isTile(3, i))
                    {
                        if (!isTile(4, i))
                            ret.setTile(i, new Point(originX + 6, originY + 1));
                        else
                            ret.setTile(i, new Point(originX + 1, originY + 3));
                    }
                    else if (!isTile(4, i))
                    {
                        if (!isTile(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 5));
                        else
                            ret.setTile(i, new Point(originX + 4, originY + 3));
                    }
                    else if (!isTile(7, i))
                        ret.setTile(i, new Point(originX + 5, originY + 3));
                    else
                        ret.setTile(i, new Point(originX + 2, originY + 3));
                }
                else if (!isTile(3, i))
                {
                    if (!isTile(1, i))
                        ret.setTile(i, new Point(originX, originY + 2));
                    else
                    {
                        if (!isTile(4, i))
                        {
                            if (!isTile(5, i))
                                ret.setTile(i, new Point(originX + 6, originY + 5));
                            else
                                ret.setTile(i, new Point(originX + 6, originY + 3));
                        }
                        else
                        {
                            if (!isTile(5, i))
                                ret.setTile(i, new Point(originX + 6, originY + 2));
                            else
                                ret.setTile(i, new Point(originX + 1, originY + 2));
                        }
                    }
                }
                else if (!isTile(1, i))
                {
                    if (!isTile(6, i))
                    {
                        if (!isTile(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 4));
                        else
                            ret.setTile(i, new Point(originX + 7, originY + 2));
                    }
                    else
                    {
                        if (!isTile(7, i))
                            ret.setTile(i, new Point(originX + 7, originY + 3));
                        else
                            ret.setTile(i, new Point(originX + 3, originY + 2));
                    }
                }
                else
                {
                    if (!isTile(4, i))
                    {
                        if (!isTile(5, i))
                        {
                            if (!isTile(6, i))
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 1, originY + 5));
                            }
                            else
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 1, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY + 4));
                            }
                        }
                        else
                        {
                            if (!isTile(6, i))
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 2, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 5, originY + 5));
                            }
                            else
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 3, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY + 1));
                            }
                        }
                    }
                    else
                    {
                        if (!isTile(5, i))
                        {
                            if (!isTile(6, i))
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 2, originY + 5));
                                else
                                    ret.setTile(i, new Point(originX + 3, originY + 5));
                            }
                            else
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 5, originY + 4));
                                else
                                    ret.setTile(i, new Point(originX + 4, originY));
                            }
                        }
                        else
                        {
                            if (!isTile(6, i))
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 4, originY + 5));
                                else
                                    ret.setTile(i, new Point(originX + 5, originY));
                            }
                            else
                            {
                                if (!isTile(7, i))
                                    ret.setTile(i, new Point(originX + 5, originY + 1));
                                else
                                    ret.setTile(i, new Point(originX + 2, originY + 2));
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public static AutoTileSettings Default3(int originX, int originY)
        {
            AutoTileSettings ret = new AutoTileSettings(new Point(originX, originY));
            for (int i = 0; i < 256; i++)
            {
                if (isTile(0, i) || isTile(2, i))
                {
                    if (isTile(1, i) || isTile(3, i))
                        ret.setTile(i, new Point(originX + 2, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
                else
                {
                    if (isTile(1, i) || isTile(3, i))
                        ret.setTile(i, new Point(originX + 1, originY));
                    else
                        ret.setTile(i, new Point(originX, originY));
                }
            }
            return ret;
        }

        public Point GetTile(int data)
        {
            return tiles[data];
        }
    }
}
