namespace System.Collections
{
    public static class EnumerableExtensions
    {
        public static object? ElementAt(this IEnumerable entities, int index)
        {
            if (entities is IList list)
                return list[index];
            else
            {
                int counter = 0;
                foreach (var entity in entities)
                {
                    if (counter == index)
                        return entity;
                    counter++;
                }
                return null;
            }
        }
        public static bool SetElementAt(this IEnumerable entities, int index, object? value)
        {
            if (entities is IList list)
                list[index] = value;
            else
                return false;
            return true;
        }
        public static bool RemoveElementAt(this IEnumerable entities, int index, out IEnumerable newEntities, out object? value)
        {
            if (entities is Array array)
            {
                IList newArray = Array.CreateInstance(array.GetType().GetElementType()!, array.Length - 1);
                IList oldArray = array;
                int adder = 0;
                value = null;
                for (int i = 0; i < oldArray.Count; i++)
                {
                    if (i != index)
                        newArray[i - adder] = oldArray[i];
                    else
                    {
                        adder = 1;
                        value = oldArray[i];
                    }
                }
                newEntities = newArray;
            }
            else if (entities is IList list)
            {
                value = list[index];
                list.RemoveAt(index);
                newEntities = list;
            }
            else
            {
                value = null;
                newEntities = null!;
                return false;
            }
            return true;
        }
    }
}
