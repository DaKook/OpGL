using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Number
    {
        private float _value;
        private Sprite _source;
        private SourceTypes _srcType;
        public enum SourceTypes { X, Y, CheckX, CheckY }
        private float Value
        {
            get
            {
                if (_source != null)
                {
                    switch (_srcType)
                    {
                        case SourceTypes.X:
                            return _source.X;
                        case SourceTypes.Y:
                            return _source.Y;
                        case SourceTypes.CheckX:
                            return (_source as Crewman)?.CheckpointX ?? 0;
                        case SourceTypes.CheckY:
                            return (_source as Crewman)?.CheckpointY ?? 0;
                        default:
                            return _value;
                    }
                }
                else
                    return _value;
            }
            set
            {
                _value = value;
            }
        }
        public string Name;

        public Number(string name, float value)
        {
            Name = name;
            Value = value;
        }
        public Number(string name, Sprite sourceSprite, SourceTypes source)
        {
            Name = name;
            _source = sourceSprite;
            _srcType = source;
        }

        public static implicit operator float(Number n)
        {
            return n.Value;
        }
        public static implicit operator Number(float f)
        {
            return new Number("", f);
        }
        public static implicit operator JToken(Number n)
        {
            return n.Value;
        }
        public static bool TryParse(string s, out Number result)
        {
            bool ret = int.TryParse(s, out int i);
            result = i;
            return ret;
        }
    }
}
