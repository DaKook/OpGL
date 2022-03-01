using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class SpriteProperty
    {
        public enum Types { Int, Float, String, Bool, Texture, Animation, Sound, Color, Script, ColorModifier }
        public string Name;
        public string Description;
        public Func<JToken> GetValue;
        public Action<JToken, Game> SetValue;
        public bool CanSet;
        public JToken DefaultValue;
        public Types Type;
        public SpriteProperty(string name, Func<JToken> get, Action<JToken, Game> set, JToken defaultValue, Types type, string description = "", bool canSet = true)
        {
            Name = name;
            GetValue = get;
            SetValue = set;
            DefaultValue = defaultValue;
            Description = description;
            CanSet = canSet;
            Type = type;
        }
        public bool IsDefault
        {
            get
            {
                if (GetValue() is null)
                    return DefaultValue is null;
                else if (DefaultValue is null) return false;
                return GetValue().Equals(DefaultValue);
            }
        }
    }
}
