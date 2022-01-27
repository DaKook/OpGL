using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Number
    {
        public float AssignedValue { get; private set; }
        public Func<float> GetValue;
        public Sprite[] SpritePointer { get; private set; }
        public float Value
        {
            get
            {
                if (GetValue == null) return AssignedValue;
                else return GetValue();
            }
            set
            {
                AssignedValue = value;
            }
        }
        public string Name;

        public Number(string name, float value)
        {
            Name = name;
            AssignedValue = value;
        }
        public Number(string name, Func<float> getVal)
        {
            Name = name;
            GetValue = getVal;
        }
        public static implicit operator Number(float f)
        {
            return new Number("", f);
        }
        public static bool TryParse(string s, out Number result)
        {
            bool ret = float.TryParse(s, out float i);
            result = i;
            return ret;
        }
        public void SetValue(float value)
        {
            GetValue = null;
            AssignedValue = value;
        }

        public static Number GetSpritePointer(Sprite[] sprite)
        {
            return new Number("sprite", 0f) { SpritePointer = sprite };
        }
    }
}
