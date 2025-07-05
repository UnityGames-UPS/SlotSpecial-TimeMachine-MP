using System.Collections.Generic;

public class Helper
{
  public static List<string> RemoveDuplicates(List<string> inputList)
  {
    if (inputList == null) return null;

    HashSet<string> uniqueStrings = new HashSet<string>(inputList);
    return new List<string>(uniqueStrings);
  }
}
