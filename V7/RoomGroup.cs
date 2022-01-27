using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class RoomGroup : Room
    {
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public bool ContainsRoom(int x, int y, Game game) => RoomDatas.ContainsKey(x * 100 + y);

        public bool Loaded { get; private set; } = false;
        public bool Loading { get; private set; } = false;

        public override int WidthRooms => w;
        public override int HeightRooms => h;
        private int w;
        private int h;
        public override bool IsGroup => true;

        public override float Width => w * ROOM_WIDTH;
        public override float Height => h * ROOM_HEIGHT;

        public RoomGroup Load(Game game)
        {
            if (!Loaded && !Loading)
            {
                Loading = true;
                if (Objects is object)
                {
                    Objects.Dispose();
                }
                Objects = new SpriteCollection();
                int minX = -1, minY = -1, maxX = -1, maxY = -1;
                for (int i = 0; i < RoomDatas.Count; i++)
                {
                    JObject rd = RoomDatas.Values[i];
                    int x = (int)rd["X"];
                    int y = (int)rd["Y"];
                    if (x < minX || minX == -1) minX = x;
                    if (x > maxX || maxX == -1) maxX = x;
                    if (y < minY || minY == -1) minY = y;
                    if (y > maxY || maxY == -1) maxY = y;
                    Room r = LoadRoom(rd, game);
                    if (r.RoomUp.HasValue) RoomUp = r.RoomUp.Value;
                    if (r.RoomDown.HasValue) RoomDown = r.RoomDown.Value;
                    if (r.RoomLeft.HasValue) RoomLeft = r.RoomLeft.Value;
                    if (r.RoomRight.HasValue) RoomRight = r.RoomRight.Value;
                    if (!string.IsNullOrEmpty(r.Name)) Name = r.Name;
                    if (i == 0)
                    {
                        if (game.RoomPresets.TryGetValue(r.GroupName ?? "", out AutoTileSettings.PresetGroup group) && group.TryGetValue(r.PresetName ?? "", out AutoTileSettings.RoomPreset preset))
                            UsePreset(preset, r.GroupName);
                        TileTexture = r.TileTexture;
                    }
                    Objects.AddRange(r.Objects);
                }
                X = minX;
                Y = minY;
                w = maxX + 1 - minX;
                h = maxY + 1 - minY;
                Loading = false;
                Loaded = true;
            }
            return this;
        }

        public void Unload()
        {
            Loaded = false;
            Objects?.Dispose();
            Objects = null;
        }

        public RoomGroup(Script enter, Script leave) : base(new SpriteCollection(), enter, leave)
        {

        }

        public void SetSize(int width, int height)
        {
            w = width;
            h = height;
        }
    }
}
