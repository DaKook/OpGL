using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class AutoTileSettings
    {
        public class RoomPreset
        {
            public Initializer Ground;
            public Initializer Background;
            public Initializer Spikes;
            public Color Color;
            public Texture Texture;
            public string Name;

            public RoomPreset(Initializer ground, Initializer background, Initializer spikes, Color color, Texture texture)
            {
                Ground = ground;
                Background = background;
                Spikes = spikes;
                Color = color;
                Texture = texture;
            }
        }
        public class PresetGroup : SortedList<string, RoomPreset>
        {
            public string Name;
            public string Background;

            public PresetGroup(string name, string background)
            {
                Name = name;
                Background = background;
            }

            public void Add(RoomPreset preset)
            {
                if (ContainsKey(preset.Name))
                    Remove(preset.Name);
                Add(preset.Name, preset);
            }
        }
        public class Initializer
        {
            public string Name;
            public Point Origin;
            public int Size;
            public Point Size2;
            public Initializer(string name, Point origin, int size, Point size2)
            {
                Name = name;
                Origin = origin;
                Size = size;
                Size2 = size2;
            }
            public AutoTileSettings Initialize()
            {
                AutoTileSettings ret = null;
                switch (Size)
                {
                    case 3:
                        ret = Default3(Origin.X, Origin.Y);
                        ret.Name = Name;
                        break;
                    case 4:
                        ret = Default4(Origin.X, Origin.Y);
                        ret.Name = Name;
                        break;
                    case 13:
                        ret = Default13(Origin.X, Origin.Y);
                        ret.Name = Name;
                        break;
                    case 47:
                        ret = Default47(Origin.X, Origin.Y);
                        ret.Name = Name;
                        break;
                }
                ret.Size2 = Size2;
                return ret;
            }
        }

        public string Name;
        private SortedList<int, Point> tiles = new SortedList<int, Point>();
        public Point Origin { get; private set; }
        public int Size { get; private set; }
        public Point Size2 { get; private set; } = new Point(1, 1);
        public JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Name", Name);
            ret.Add("OriginX", Origin.X);
            ret.Add("OriginY", Origin.Y);
            ret.Add("Size", Size);
            if (Size2.X != 1 || Size2.Y != 1)
            {
                JArray s2 = new JArray() { Size2.X, Size2.Y };
                ret.Add("Size2", s2);
            }
            return ret;
        }
        public static AutoTileSettings Load(JToken loadFrom)
        {
            if (loadFrom is null) return null;
            string name = (string)loadFrom["Name"];
            int x = (int)(loadFrom["OriginX"] ?? 0);
            int y = (int)(loadFrom["OriginY"] ?? 0);
            int size = (int)(loadFrom["Size"] ?? 13);
            Point size2 = new Point(1, 1);
            JArray s2 = (JArray)loadFrom["Size2"];
            if (s2 is object && s2.Count == 2)
            {
                int sizex = (int)s2[0];
                int sizey = (int)s2[1];
                
            }
            return new Initializer(name, new Point(x, y), size, size2).Initialize();
        }
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
            ret.Name = originX.ToString() + "," + originY.ToString();
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
            ret.Name = originX.ToString() + "," + originY.ToString();
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
            ret.Name = originX.ToString() + "," + originY.ToString();
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
                else
                    ret.setTile(i, new Point(originX + 3, originY));
            }
            ret.Size = 4;
            ret.Name = originX.ToString() + "," + originY.ToString();
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
