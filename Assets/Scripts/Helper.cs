using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour
{

    internal static List<string> FlattenSymbolsToEmit(List<List<string>> symbolsToEmit)
    {
        List<string> flattenedList = new List<string>();

        // Flatten the list
        foreach (var innerList in symbolsToEmit)
        {
            flattenedList.AddRange(innerList);
        }

        return flattenedList;
    }

        public static List<string> RemoveDuplicates(List<string> inputList)
    {
        if (inputList == null) return null;

        HashSet<string> uniqueStrings = new HashSet<string>(inputList);
        return new List<string>(uniqueStrings);
    }
}
