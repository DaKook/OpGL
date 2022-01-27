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
        private float _value;
        public float AssignedValue => _value;
        public Func<Script.Executor, float> GetValue;
        public float Value(Script.Executor e)
        {
            if (GetValue == null) return _value;
            else return GetValue(e);
        }
        public string Name;

        public Number(string name, float value)
        {
            Name = name;
            _value = value;
        }
        public Number(string name, Func<Script.Executor, float> getVal)
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
            _value = value;
        }
    }
}
