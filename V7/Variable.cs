using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class DecimalVariable : Variable<float>
    {
        public override VarTypes VarType => VarTypes.Decimal;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Integer)
            {
                v = new IntegerVariable(Name, (int)Value);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.ToString());
                return true;
            }
            else if (type == VarTypes.Boolean)
            {
                v = new BooleanVariable(Name, Value != 0);
                return true;
            }
            else if (type == VarTypes.Texture)
            {
                if (Value < 0 || Value >= context.Game.Textures.Count)
                {
                    v = Null;
                    return false;
                }
                v = new TextureVariable(Name, context.Game.Textures.Values[(int)Value]);
                return true;
            }
            else if (type == VarTypes.Sound)
            {
                if (Value < 0 || Value >= context.Game.Sounds.Count)
                {
                    v = Null;
                    return false;
                }
                v = new SoundVariable(Name, context.Game.Sounds.Values[(int)Value]);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as DecimalVariable).Value;
        }
        public DecimalVariable(string name, float value) : base(name, value)
        {
        }
        public DecimalVariable(string name, Func<float> getVal, Action<float> setVal) : base(name, getVal, setVal)
        {
        }
        public static implicit operator DecimalVariable(float v)
        {
            return new DecimalVariable("", v);
        }
        public static implicit operator DecimalVariable(IntegerVariable i)
        {
            return new DecimalVariable(i.Name, () => i.Value, (f) => i.Value = (int)f);
        }
        public override Variable Operate(string op, Variable other, Script.Executor e)
        {
            if (other.TryConvert(VarTypes.Decimal, out Variable o, e))
            {
                DecimalVariable v = o as DecimalVariable;
                if (op == "+") return new DecimalVariable(Name, Value + v.Value);
                if (op == "-") return new DecimalVariable(Name, Value - v.Value);
                if (op == "*") return new DecimalVariable(Name, Value * v.Value);
                if (op == "/") return new DecimalVariable(Name, Value / v.Value);
                if (op == "^") return new DecimalVariable(Name, (float)Math.Pow(Value, v.Value));
                if (op == "%") return new DecimalVariable(Name, Value % v.Value);
                if (op == ">") return new BooleanVariable(Name, Value > v.Value);
                if (op == "<") return new BooleanVariable(Name, Value < v.Value);
                if (op == "=") return new BooleanVariable(Name, Value == v.Value);
            }
            return base.Operate(op, other, e);
        }

    }
    public class IntegerVariable : Variable<int>
    {
        public override VarTypes VarType => VarTypes.Integer;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Decimal)
            {
                v = new DecimalVariable(Name, Value);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.ToString());
                return true;
            }
            else if (type == VarTypes.Boolean)
            {
                v = new BooleanVariable(Name, Value != 0);
                return true;
            }
            else if (type == VarTypes.Texture)
            {
                if (Value < 0 || Value >= context.Game.Textures.Count)
                {
                    v = Null;
                    return false;
                }
                v = new TextureVariable(Name, context.Game.Textures.Values[Value]);
                return true;
            }
            else if (type == VarTypes.Sound)
            {
                if (Value < 0 || Value >= context.Game.Sounds.Count)
                {
                    v = Null;
                    return false;
                }
                v = new SoundVariable(Name, context.Game.Sounds.Values[Value]);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as IntegerVariable).Value;
        }
        public IntegerVariable(string name, int value) : base(name, value)
        {
        }
        public IntegerVariable(string name, Func<int> getVal, Action<int> setVal) : base(name, getVal, setVal)
        {
        }
        public override Variable Operate(string op, Variable other, Script.Executor e)
        {
            TryConvert(VarTypes.Decimal, out Variable v, e);
            return v.Operate(op, other, e);
        }
    }
    public class StringVariable : Variable<string>
    {
        public override VarTypes VarType => VarTypes.String;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Integer)
            {
                bool can;
                if (can = int.TryParse(Value, out int i))
                    v = new IntegerVariable(Name, i);
                else v = Null;
                return can;
            }
            else if (type == VarTypes.Decimal)
            {
                bool can;
                if (can = float.TryParse(Value, out float f))
                    v = new DecimalVariable(Name, f);
                else v = Null;
                return can;
            }
            else if (type == VarTypes.Boolean)
            {
                bool can;
                if (can = bool.TryParse(Value, out bool b))
                    v = new BooleanVariable(Name, b);
                else v = Null;
                return can;
            }
            else if (type == VarTypes.Color)
            {
                Color? c = context.Game.GetColor(Value, context.Sender, context.Target);
                if (c.HasValue)
                    v = new ColorVariable(Name, c.Value);
                else
                    v = Null;
                return c.HasValue;
            }
            else if (type == VarTypes.Texture)
            {
                Texture t = context.Game.TextureFromName(Value);
                bool can;
                if (can = t is object)
                    v = new TextureVariable(Name, t);
                else v = Null;
                return can;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as StringVariable).Value;
        }
        public StringVariable(string name, string value) : base(name, value)
        {
        }
        public StringVariable(string name, Func<string> getVal, Action<string> setVal) : base(name, getVal, setVal)
        {
        }
        public override Variable Operate(string op, Variable other, Script.Executor e)
        {
            if (op == "+")
            {
                if (other.TryConvert(VarTypes.String, out Variable s, e))
                    return new StringVariable(Name, Value + (s as StringVariable).Value);
            }
            return base.Operate(op, other, e);
        }
    }
    public class BooleanVariable : Variable<bool>
    {
        public override VarTypes VarType => VarTypes.Boolean;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Decimal)
            {
                v = new DecimalVariable(Name, Value ? 1 : 0);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.ToString());
                return true;
            }
            else if (type == VarTypes.Integer)
            {
                v = new IntegerVariable(Name, Value ? 1 : 0);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as BooleanVariable).Value;
        }
        public BooleanVariable(string name, bool value) : base(name, value)
        {
        }
        public BooleanVariable(string name, Func<bool> getVal, Action<bool> setVal) : base(name, getVal, setVal)
        {
        }
        public override Variable Operate(string op, Variable other, Script.Executor e)
        {
            if (op == "&" || op == "|")
            {
                if (other.TryConvert(VarTypes.Boolean, out Variable b, e))
                {
                    if (op == "&")
                        return new BooleanVariable(Name, Value & (b as BooleanVariable).Value);
                    else
                        return new BooleanVariable(Name, Value | (b as BooleanVariable).Value);
                }
            }
            else if (op == "+" || op == "-" || op == "*" || op == "/")
            {
                if (TryConvert(other.VarType, out Variable o, e))
                {
                    return o.Operate(op, other, e);
                }
            }
            return base.Operate(op, other, e);
        }
    }
    public class ColorVariable : Variable<Color>
    {
        public ColorVariable(string name, Color value) : base(name, value)
        {
        }

        public ColorVariable(string name, Func<Color> getVal, Action<Color> setVal) : base(name, getVal, setVal)
        {
        }

        public override VarTypes VarType => VarTypes.Color;

        public override Variable GetProperty(string property, Script.Executor e)
        {
            switch (property.ToLower())
            {
                case "r":
                    return new IntegerVariable(property, () => Value.R, (i) => Value = Color.FromArgb(Value.A, Math.Max(Math.Min(i, 255), 0), Value.G, Value.B));
                case "g":
                    return new IntegerVariable(property, () => Value.G, (i) => Value = Color.FromArgb(Value.A, Value.R, Math.Max(Math.Min(i, 255), 0), Value.B));
                case "b":
                    return new IntegerVariable(property, () => Value.B, (i) => Value = Color.FromArgb(Value.A, Value.R, Value.G, Math.Max(Math.Min(i, 255), 0)));
                case "a":
                    return new IntegerVariable(property, () => Value.A, (i) => Value = Color.FromArgb(Math.Max(Math.Min(i, 255), 0), Value.R, Value.G, Value.B));
            }
            return base.GetProperty(property, e);
        }

        public override void Set(Variable other)
        {
            Value = (other as ColorVariable).Value;
        }

        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.Name);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
    }

    public class SpriteVariable : Variable<Sprite>
    {
        public override VarTypes VarType => VarTypes.Sprite;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Texture)
            {
                v = new TextureVariable(Name, Value.Texture);
                return true;
            }
            else if (type == VarTypes.Animation)
            {
                v = new AnimationVariable(Name, Value.Animation);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as SpriteVariable).Value;
        }
        public SpriteVariable(string name, Sprite value) : base(name, value)
        {
        }
        public SpriteVariable(string name, Func<Sprite> getVal, Action<Sprite> setVal) : base(name, getVal, setVal)
        {
        }

        public override Variable GetProperty(string property, Script.Executor e)
        {
            if (Value is null) return Null;
            if (Value.Properties.TryGetValue(property, out SpriteProperty p))
            {
                switch (p.Type)
                {
                    case SpriteProperty.Types.Int:
                        return new IntegerVariable(property, () => (int)p.GetValue(), (i) => { if (p.CanSet) p.SetValue(i, e.Game); });
                    case SpriteProperty.Types.Float:
                        return new DecimalVariable(property, () => (float)p.GetValue(), (f) => { if (p.CanSet) p.SetValue(f, e.Game); });
                    case SpriteProperty.Types.String:
                        return new StringVariable(property, () => (string)p.GetValue(), (s) => { if (p.CanSet) p.SetValue(s, e.Game); });
                    case SpriteProperty.Types.Bool:
                        return new BooleanVariable(property, () => (bool)p.GetValue(), (b) => { if (p.CanSet) p.SetValue(b, e.Game); });
                    case SpriteProperty.Types.Texture:
                        return new TextureVariable(property, () => e.Game.TextureFromName((string)p.GetValue() ?? ""), (t) => { if (p.CanSet) p.SetValue(t.Name, e.Game); });
                    case SpriteProperty.Types.Animation:
                        return new AnimationVariable(property, () => Value.Texture.AnimationFromName((string)p.GetValue() ?? ""), (a) => { if (p.CanSet) p.SetValue(a.Name, e.Game); });
                    case SpriteProperty.Types.Sound:
                        break;
                    case SpriteProperty.Types.Color:
                        return new ColorVariable(property, () => Color.FromArgb((int)p.GetValue()), (c) => p.SetValue(c.ToArgb(), e.Game));
                    case SpriteProperty.Types.Script:
                        break;
                    case SpriteProperty.Types.ColorModifier:
                        break;
                }
            }
            switch (property.ToLower())
            {
                case "x":
                    return new DecimalVariable(property, () => Value.X - e.Game.CurrentRoom.GetX, (f) => Value.X = f + e.Game.CurrentRoom.GetX);
                case "y":
                    return new DecimalVariable(property, () => Value.Y - e.Game.CurrentRoom.GetY, (f) => Value.Y = f + e.Game.CurrentRoom.GetY);
                case "right":
                    return new DecimalVariable(property, () => Value.Right - e.Game.CurrentRoom.GetX, (f) => Value.Right = f + e.Game.CurrentRoom.GetX);
                case "bottom":
                    return new DecimalVariable(property, () => Value.Bottom - e.Game.CurrentRoom.GetY, (f) => Value.Bottom = f + e.Game.CurrentRoom.GetY);
                case "centerx":
                    return new DecimalVariable(property, () => Value.CenterX - e.Game.CurrentRoom.GetX, (f) => Value.CenterX = f + e.Game.CurrentRoom.GetX);
                case "centery":
                    return new DecimalVariable(property, () => Value.CenterY - e.Game.CurrentRoom.GetY, (f) => Value.CenterY = f + e.Game.CurrentRoom.GetY);
                case "color":
                    return new ColorVariable(property, () => Value.Color, (c) => Value.Color = c);
            }
            if (property.ToLower() == "trinkets" && Value is Crewman)
            {
                return new IntegerVariable(property, (Value as Crewman).HeldTrinkets.Count + (Value as Crewman).PendingTrinkets.Count);
            }
            return Null;
        }
    }

    public class TextureVariable : Variable<Texture>
    {
        public override VarTypes VarType => VarTypes.Texture;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Integer)
            {
                int i = context.Game.Textures.IndexOfKey(Value.Name);
                v = new IntegerVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.Decimal)
            {
                int i = context.Game.Textures.IndexOfKey(Value.Name);
                v = new DecimalVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.Name);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as TextureVariable).Value;
        }
        public TextureVariable(string name, Texture value) : base(name, value)
        {
        }
        public TextureVariable(string name, Func<Texture> getVal, Action<Texture> setVal) : base(name, getVal, setVal)
        {
        }
        public override Variable GetProperty(string property, Script.Executor e)
        {
            if (Value is null) return Null;
            Animation a = Value.AnimationFromName(property);
            if (a is object)
            {
                return new AnimationVariable(property, a);
            }
            switch (property)
            {
                case "TileSize":
                case "TileSizeX":
                    return new IntegerVariable(property, Value.TileSizeX);
                case "TileSizeY":
                    return new IntegerVariable(property, Value.TileSizeY);
                default:
                    if (property.StartsWith("Animation[") && property.EndsWith("]"))
                    {
                        string s = property.Substring(10, property.Length - 11);
                        //if (Value.Animations.TryGetValue(s, out Animation a)) return new AnimationVariable(property, a);
                        Variable num = GetFullVariable(s, e);
                        if (num is object && (num.VarType == VarTypes.Integer || num.TryConvert(VarTypes.Integer, out num, e)))
                        {
                            int i = (num as IntegerVariable).Value;
                            if (i > -1 && i < Value.Animations.Count)
                            {
                                return new AnimationVariable(property, Value.Animations.Values[i]);
                            }
                        }
                    }
                    break;
            }
            return Null;
        }
    }

    public class AnimationVariable : Variable<Animation>
    {
        public override VarTypes VarType => VarTypes.Animation;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Integer)
            {
                int i = Value.Texture.Animations.IndexOfKey(Value.Name);
                v = new IntegerVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.Decimal)
            {
                int i = Value.Texture.Animations.IndexOfKey(Value.Name);
                v = new DecimalVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.Name);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as AnimationVariable).Value;
        }
        public AnimationVariable(string name, Animation value) : base(name, value)
        {
        }
        public AnimationVariable(string name, Func<Animation> getVal, Action<Animation> setVal) : base(name, getVal, setVal)
        {
        }

        public override Variable GetProperty(string property, Script.Executor e)
        {
            if (Value is null) return Null;
            switch (property)
            {
                case "Width":
                    return new IntegerVariable(property, Value.Hitbox.Width);
                case "Height":
                    return new IntegerVariable(property, Value.Hitbox.Height);
            }
            return Null;
        }
    }

    public class SoundVariable : Variable<SoundEffect>
    {
        public override VarTypes VarType => VarTypes.Sound;
        public override bool TryConvert(VarTypes type, out Variable v, Script.Executor context)
        {
            if (type == VarTypes.Integer)
            {
                int i = context.Game.Sounds.IndexOfKey(Value.Name);
                v = new IntegerVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.Decimal)
            {
                int i = context.Game.Sounds.IndexOfKey(Value.Name);
                v = new DecimalVariable(Name, i);
                return true;
            }
            else if (type == VarTypes.String)
            {
                v = new StringVariable(Name, Value.Name);
                return true;
            }
            else if (type == VarType)
            {
                v = this;
                return true;
            }
            v = Null;
            return false;
        }
        public override void Set(Variable other)
        {
            Value = (other as SoundVariable).Value;
        }
        public SoundVariable(string name, SoundEffect value) : base(name, value)
        {
        }
        public SoundVariable(string name, Func<SoundEffect> getVal, Action<SoundEffect> setVal) : base(name, getVal, setVal)
        {
        }

        public override Variable GetProperty(string property, Script.Executor e)
        {
            if (Value is null) return Null;
            
            return Null;
        }
    }

    public abstract class Variable<T> : Variable
    {
        public string StringValue
        {
            get => Value?.ToString() ?? "";
            set
            {

            }
        }
        
        public T AssignedValue { get; private set; }
        public Func<T> GetValue;
        public Action<T> SetValue;
        public T Value
        {
            get
            {
                if (GetValue == null) return AssignedValue;
                else return GetValue();
            }
            set
            {
                if (SetValue is null)
                    AssignedValue = value;
                else
                    SetValue(value);
            }
        }
        public string Name;

        public Variable(string name, T value)
        {
            Name = name;
            AssignedValue = value;
        }
        public Variable(string name, Func<T> getVal, Action<T> setVal)
        {
            Name = name;
            GetValue = getVal;
            SetValue = setVal;
        }
        public static bool TryParse(string s, out DecimalVariable result)
        {
            bool ret = float.TryParse(s, out float i);
            result = i;
            return ret;
        }

        public override Variable Operate(string op, Variable other, Script.Executor e)
        {
            if (op == "=")
            {
                if (other.TryConvert(VarType, out Variable o, e))
                {
                    return new BooleanVariable(Name, Value.Equals((o as Variable<T>).Value));
                }
            }
            return base.Operate(op, other, e);
        }

        public override string ToString()
        {
            return StringValue;
        }
    }

    public abstract class Variable
    {
        public static Variable Null => new DecimalVariable("", 0);
        public virtual SortedSet<string> Properties => new SortedSet<string>() { };
        public enum VarTypes { Decimal, Integer, String, Boolean, Color, Sprite, Texture, Animation, Sound }
        public abstract VarTypes VarType { get; }
        public abstract bool TryConvert(VarTypes type, out Variable v, Script.Executor context);
        public abstract void Set(Variable other);
        public virtual Variable Operate(string op, Variable other, Script.Executor e)
        {
            return this;
        }
        public virtual Variable GetProperty(string property, Script.Executor e)
        {
            return Null;
        }

        public static readonly SortedSet<string> Keywords = new SortedSet<string>()
        {
            "player", "this", "self", "target"
        };
        public static Variable GetVariable(string v, Script.Executor context)
        {
            if (v.StartsWith("\"") && v.EndsWith("\""))
            {
                string s = v.Substring(1, v.Length - 2);
                return new StringVariable(v, s);
            }
            if (v.StartsWith("tex[") && v.EndsWith("]"))
            {
                string s = v.Substring(4, v.Length - 5);
                return new TextureVariable(v, context.Game.TextureFromName(s));
            }
            if (v.StartsWith("sound[") && v.EndsWith("]"))
            {
                string s = v.Substring(6, v.Length - 7);
                return new SoundVariable(v, context.Game.GetSound(s));
            }
            if (v.StartsWith("clr[") && v.EndsWith("]"))
            {
                string s = v.Substring(4, v.Length - 4);
                Color? c = context.Game.GetColor(s, context.Sender, context.Target);
                if (c.HasValue)
                    return new ColorVariable(v, c.Value);
            }
            if (v == "player")
                return new SpriteVariable(v, () => context.Game.ActivePlayer, (s) =>
                {
                    if (s is Crewman)
                        context.Game.ActivePlayer = s as Crewman;
                });
            if (v == "self" || v == "this")
                return new SpriteVariable(v, () => context.Sender, null);
            if (v == "target")
                return new SpriteVariable(v, () => context.Target, null);
            if (v.ToLower() == "true") return new BooleanVariable(v, true);
            if (v.ToLower() == "false") return new BooleanVariable(v, false);
            if (v.StartsWith("?"))
            {
                if (v.ToLower() == "?totaltrinkets") return new IntegerVariable(v, context.Game.LevelTrinkets.Count);
                if (v.ToLower() == "?roomx") return new IntegerVariable(v, context.Game.CurrentRoom.X);
                if (v.ToLower() == "?roomy") return new IntegerVariable(v, context.Game.CurrentRoom.Y);
                if (v.ToLower() == "?trinkets") return new IntegerVariable(v, context.Game.CollectedTrinkets.Count);
                if (v.ToLower() == "?player") return new SpriteVariable(v, () => context.Game.ActivePlayer, (s) =>
                {
                    if (s is Crewman)
                        context.Game.ActivePlayer = s as Crewman;
                });
                if (v.ToLower() == "?target") return new SpriteVariable(v, () => context.Target, null);
                if (v.ToLower() == "?self" || v.ToLower() == "?this") return new SpriteVariable(v, () => context.Sender, null);
            }

            if (context.Locals.ContainsKey(v))
                return context.Locals[v];
            if (context.Game.Vars.ContainsKey(v))
                return context.Game.Vars[v];
            if (context.Game.UserAccessSprites.ContainsKey(v))
                return new SpriteVariable(v, context.Game.SpriteFromName(v));

            if (int.TryParse(v, out int i))
                return new IntegerVariable(v, i);
            if (float.TryParse(v, out float f))
                return new DecimalVariable(v, f);

            return Null;
        }

        public static Variable GetFullVariable(string s, Script.Executor e)
        {
            Variable var;
            int ind = s.IndexOf(':');
            string arg;
            bool done;
            if (done = ind == -1) ind = s.Length;
            arg = s.Substring(0, ind);
            var = GetVariable(arg, e);
            while (!done && var is object)
            {
                s = s.Substring(ind + 1);
                if (done = (ind = s.IndexOf(':')) == -1)
                    ind = s.Length;
                arg = s.Substring(0, ind);
                var = var.GetProperty(arg, e);
            }
            return var;
        }

        private static char[] operators = new char[] { '+', '-', '(', ')', '*', '/', '^', '&', '|', '>', '<', '=', '%' };
        private static SortedDictionary<char, int> priorities = new SortedDictionary<char, int>()
        {
            { '+', 2 }, { '-', 2 },
            { '*', 3 }, { '/', 3 },
            { '^', 4 }, { '%', 4 },
            { '(', 5 }, { ')', -1 },
            { '=', 1 }, { '>', 1 }, { '<', 1 }, 
            { '&', 0 }, { '|', 0 }
        };
        private static int IndexOfAny(string s, int start)
        {
            while (start < s.Length && !priorities.ContainsKey(s[start]))
                start++;
            return start;
        }
        private static SortedSet<string> maths = new SortedSet<string>() { "sin", "cos", "tan", "asin", "acos", "atan", "sqrt", "rand", "int", "abs" };
        private static Random r = new Random();
        public static Variable DoMath(string s, Script.Executor e)
        {
            List<string> operations = new List<string>();
            List<Variable> ops = new List<Variable>();
            s = " " + s;
            List<char> symbols = new List<char>();
            int index = 0;
            int lastIndex = 0;
            while (index < s.Length - 1 && index > -1)
            {
                index = IndexOfAny(s, index + 1);
                if (index == s.Length)
                {
                    s += " ";
                }
                string op = s.Substring(lastIndex, index - lastIndex).Trim();
                char symbol = s[index];
                if (symbol == '-' && op == "") continue;
                operations.Add(op);
                symbols.Add(symbol);
                if (op != "" && !maths.Contains(op))
                {
                    ops.Add(GetFullVariable(op, e));
                }
                else
                    ops.Add(null);
                lastIndex = index + 1;
            }
            if (symbols.Last() != ' ')
            {
                symbols.Add(' ');
                ops.Add(null);
                operations.Add("");
            }
            index = 0;
            Stack<int> returns = new Stack<int>();
            Stack<int> parReturns = new Stack<int>();
            int priority;
            while (symbols.Count > 0)
             {
                if (index < 0) index = 0;
                char sym = symbols[index];
                priorities.TryGetValue(sym, out priority);
                if (index < symbols.Count - 1)
                {
                    if (priorities.TryGetValue(symbols[index + 1], out int otherPriority) && otherPriority > priority && priority != -1)
                    {
                        if (otherPriority == 5)
                            parReturns.Push(index);
                        else
                            returns.Push(index);
                        index++;
                        if (otherPriority == 5) index++;
                        continue;
                    }
                    else if (priority == 5)
                    {
                        parReturns.Push(index - 1);
                        index++;
                        continue;
                    }
                    else if (priority == -1)
                    {
                        symbols.RemoveAt(index);
                        ops.RemoveAt(index + 1);
                        operations.RemoveAt(index + 1);
                        if (priority != 5)
                            index = parReturns.Pop();
                        else
                            index--;
                        //if (index < 0) index = 0;
                        if (maths.Contains(operations[index + 1]))
                        {
                            if (ops[index + 2].TryConvert(VarTypes.Decimal, out Variable v, e))
                            {
                                DecimalVariable dv = v as DecimalVariable;
                                switch (operations[index + 1])
                                {
                                    case "rand":
                                        ops[index + 2] = new IntegerVariable("", (int)(r.NextDouble() * dv.Value));
                                        break;
                                    case "sin":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Sin(dv.Value));
                                        break;
                                    case "cos":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Cos(dv.Value));
                                        break;
                                    case "tan":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Tan(dv.Value));
                                        break;
                                    case "asin":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Asin(dv.Value));
                                        break;
                                    case "acos":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Acos(dv.Value));
                                        break;
                                    case "atan":
                                        if (ops.Count > index + 3 && symbols[index + 3] == '/' && ops[index + 3].TryConvert(VarTypes.Decimal, out Variable v2, e))
                                            ops[index + 2] = new DecimalVariable("", (float)Math.Atan2(dv.Value, (v2 as DecimalVariable).Value));
                                        else
                                            ops[index + 2] = new DecimalVariable("", (float)Math.Atan(dv.Value));
                                        break;
                                    case "sqrt":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Sqrt(dv.Value));
                                        break;
                                    case "int":
                                        ops[index + 2] = new IntegerVariable("", (int)dv.Value);
                                        break;
                                    case "abs":
                                        ops[index + 2] = new DecimalVariable("", (float)Math.Abs(dv.Value));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        ops.RemoveAt(index + 1);
                        operations.RemoveAt(index + 1);
                        symbols.RemoveAt(index + 1);
                        continue;
                    }
                    else if (otherPriority < priority && returns.Count > 0 && symbols[index + 1] != ' ')
                    {
                        index = returns.Pop();
                        continue;
                    }
                    if (ops[index] is object && ops[index + 1] is object)
                    {
                        Variable v1 = ops[index], v2 = ops[index + 1];
                        Variable result = v1.Operate(sym.ToString(), v2, e);
                        ops[index] = result;
                        ops.RemoveAt(index + 1);
                        operations.RemoveAt(index + 1);
                        symbols.RemoveAt(index);
                    }
                    else
                        index++;
                }
                else if (!returns.TryPop(out index))
                {
                    break;
                }
            }
            if (ops.Count > 0)
                return ops[0];
            else
                return Null;
        }
    }
}
