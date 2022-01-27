using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public class SpriteGroup : List<Sprite>
    {
        public SpriteCollection AddedTo { get; private set; }

        private float _x;
        private float _y;
        public float X
        {
            get => _x;
            set
            {
                float distance = value - _x;
                foreach (Sprite sprite in this)
                {
                    sprite.X += distance;
                }
                _x = value;
            }
        }
        public float Y
        {
            get => _y;
            set
            {
                float distance = value - _y;
                foreach (Sprite sprite in this)
                {
                    sprite.Y += distance;
                }
                _y = value;
            }
        }

        public SpriteGroup() { }
        public SpriteGroup(IEnumerable<Sprite> sprites)
        {
            AddRange(sprites);
        }
        public SpriteGroup(params Sprite[] sprites)
        {
            AddRange(sprites);
        }

        public void AddToCollection(SpriteCollection collection)
        {
            if (AddedTo is object)
            {
                foreach (Sprite sprite in this)
                {
                    AddedTo.Remove(sprite);
                }
            }
            AddedTo = collection;
            foreach (Sprite sprite in this)
            {
                collection.Add(sprite);
            }
        }

        public void RemoveFromCollection(SpriteCollection collection)
        {
            if (AddedTo != collection) return;
            foreach (Sprite sprite in this)
            {
                collection.Remove(sprite);
            }
            AddedTo = null;
        }

        public new void Add(Sprite sprite)
        {
            base.Add(sprite);
            if (AddedTo is object)
                AddedTo.Add(sprite);
        }

        public new void Remove(Sprite sprite)
        {
            base.Remove(sprite);
            if (AddedTo is object)
                AddedTo.Remove(sprite);
        }

        public new void RemoveAt(int index)
        {
            Sprite s = this[index];
            base.RemoveAt(index);
            if (AddedTo is object)
                AddedTo.Remove(s);
        }

        public new void Clear()
        {
            if (AddedTo is object)
            {
                foreach (Sprite sprite in this)
                {
                    AddedTo.Remove(sprite);
                }
            }
            base.Clear();
        }

        public new void Insert(int index, Sprite sprite)
        {
            base.Insert(index, sprite);
            if (AddedTo is object)
                AddedTo.Add(sprite);
        }

        public new void AddRange(IEnumerable<Sprite> sprites)
        {
            foreach (Sprite sprite in sprites)
            {
                Add(sprite);
            }
        }

        public void Include(SpriteGroup other)
        {
            foreach (Sprite sprite in other)
            {
                base.Add(sprite);
            }
            if (other.AddedTo is null && AddedTo is object)
            {
                other.AddToCollection(AddedTo);
            }
        }
    }
}
