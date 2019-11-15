using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class RoomGroup : Room
    {
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public bool ContainsRoom(int x, int y, Game game) => RoomDatas.ContainsKey(x * 100 + y);
        public void Load(Game game)
        {
            Objects.Clear();
            for (int i = 0; i < RoomDatas.Count; i++)
            {
                JArray objs = (JArray)RoomDatas.Values[i]["Objects"];
                for (int j = 0; j < objs.Count; j++)
                {
                    Objects.Add(Sprite.LoadSprite(objs[j], game));
                }
            }
        }

        public RoomGroup(Script enter, Script leave) : base(new SpriteCollection(), enter, leave)
        {

        }
    }
}
