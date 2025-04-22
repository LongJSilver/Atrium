using Atrium.Data;
using System;
using System.Collections.Generic;

namespace Atrium.Data
{
    internal class ListIndexer<Key, Value> : Dictionary<Key, IList<Value>>
    {
        public ListIndexer() : base()
        {
        }


        public ListIndexer(IEnumerable<Value> toAdd, Func<Value, Key> GetKey) : this()
        {
            AddRange(toAdd, GetKey);
        }

        public void AddRange(IEnumerable<Value> toAdd, Func<Value, Key> GetKey)
        {
            foreach (var val in toAdd)
            {
                Add(GetKey(val), val);
            }
        }

        public void Add(Key k, Value v)
        {
            IList<Value> valori = this[k];
            valori.Add(v);
        }

        public void Remove(Key k, Value v)
        {
            IList<Value> valori = this[k];
            valori.Remove(v);
        }

        public new IList<Value> this[Key key]
        {
            get
            {
                IList<Value> target;
                if (base.ContainsKey(key)) target = base[key];
                else { base[key] = target = new List<Value>(); }
                return target;
            }
        }
    }

    public class SetIndexer<Key, Value> : Dictionary<Key, ISet<Value>>
    {
        public SetIndexer() : base()
        {

        }

        public void Add(Key key, Value v)
        {
            this[key].Add(v);
        }

        public void Remove(Key k, Value v)
        {
            ISet<Value> valori = this[k];
            valori.Remove(v);
        }
        public new ISet<Value> this[Key key]
        {
            get
            {
                ISet<Value> target;
                if (base.ContainsKey(key)) target = base[key];
                else { base[key] = target = new HashSet<Value>(); }
                return target;
            }
        }
    }

    internal class CaseInsensitiveListIndexer<Value> : CIDictionary<IList<Value>>
    {
        public CaseInsensitiveListIndexer() : base()
        {

        }

        public void Add(string key, Value v)
        {
            if (TryGetValue(key, out IList<Value>? target))
            {
                target.Add(v);
            }
            else
            {
                throw new ArgumentException($"key {key} not found!");
            }
        }

        public new IList<Value> this[string key]
        {
            get
            {
                if (!TryGetValue(key, out IList<Value> target))
                {
                    base[key] = target = new List<Value>();
                }
                return target;
            }
        }
    }
    internal class CaseInsensitiveSetIndexer<Value> : CIDictionary<ISet<Value>>
    {
        public CaseInsensitiveSetIndexer() : base()
        {

        }

        public bool Add(string key, Value v)
        {
            if (TryGetValue(key, out ISet<Value>? target))
            {
                return target.Add(v);
            }
            else
            {
                throw new ArgumentException($"key {key} not found!");
            }
        }

        public new ISet<Value> this[string key]
        {
            get
            {
                if (!TryGetValue(key, out ISet<Value> target))
                {
                    base[key] = target = new HashSet<Value>();
                }
                return target;
            }
        }

    }
}
