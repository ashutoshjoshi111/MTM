using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MTS
{
    public class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary = new ConcurrentDictionary<T, byte>();

        // Add item to the set
        public bool Add(T item) => _dictionary.TryAdd(item, 0);

        // Remove item from the set
        public bool Remove(T item) => _dictionary.TryRemove(item, out _);

        // Check if the set contains the item
        public bool Contains(T item) => _dictionary.ContainsKey(item);
    }
}
