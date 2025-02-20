using System.Collections.Generic;

namespace LoadoutEnigma.Utilities.Extensions
{
    public static class CollectionExtensions
    {
        public static void EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
        }
    }
}
