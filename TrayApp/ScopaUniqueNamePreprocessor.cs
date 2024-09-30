using System;
using System.IO;

namespace Scopa.Editor
{
    /// <summary>
    /// Appends a unique unityname property and name(based on the classname) if one does not exist
    /// on each non brush map entity
    /// </summary>
    public static class UniqueNamePreprocessor
    {
        public static void Parse(string path)
        {
            var lines        = File.ReadAllLines(path);
            var classes      = new Dictionary<string, int>();
            var names        = new List<string>();
            var updatedLines = new List<string>();

            // Collect all current names in a list
            foreach (var line in lines)
                if (line.BeginsWith("unityname"))
                    names.Add(line.GetValue());

            for (int i = 0; i < lines.Length; i++)
            {
                // Trim the line to remove any leading or trailing whitespace
                var line = lines[i].Trim();
                updatedLines.Add(line);

                // Check if the line starts with "classname"
                if (!line.BeginsWith("classname")) 
                    continue;
                
                var classType = line.GetValue();
                // Skip these classes
                // func_group appears to be a tb specific entity used for layers or grouping
                if (classType == "func_group" || classType == "worldspawn")
                    continue;

                // Increment the count for this class name
                if (!classes.ContainsKey(classType))
                    classes.Add(classType, 1);
                else
                    classes[classType]++;

                var hasUnityName = lines.GetNextLine(i).BeginsWith("unityname");
                if (hasUnityName) 
                    continue;
                
                // Create a unique unityname using classType and count
                var baseUnityName   = $"{classType} {classes[classType]}";
                var uniqueUnityName = baseUnityName;

                // Ensure the unityname is unique
                int suffix = 1;
                while (names.Contains(uniqueUnityName))
                {
                    uniqueUnityName = $"{baseUnityName} ({suffix++})";
                }

                // Add the new unique name to the names list
                names.Add(uniqueUnityName);

                // Insert a new line with the unique unityname
                var newUnityNameLine = $"\"unityname\" \"{uniqueUnityName}\"";
                updatedLines.Add(newUnityNameLine);
            }


            // Optional: Print the count of each class type
            /*foreach (var kvp in classes)
            {
                Debug.Log($"Class: {kvp.Key}, Count: {kvp.Value}");
            }*/

            // Overwrite the original file with the updated lines
            File.WriteAllLines(path, updatedLines);
            //Debug.Log(updatedLines.Count);
        }

    }
    
    /// <summary>
    /// String reading methods for reducing complexity of the name preprocessor
    /// </summary>
    public static class MapPreProcessorStringUtility
    {
        public static bool BeginsWith(this string line, string prefix)
        {
            return line.StartsWith($"\"{prefix}\"");
        }
    
        public static string GetValue(this string line)
        {
            // Find the start and end of the value inside quotes.
            int firstQuoteIndex  = line.IndexOf('"');
            int secondQuoteIndex = line.IndexOf('"', firstQuoteIndex  + 1);
            int thirdQuoteIndex  = line.IndexOf('"', secondQuoteIndex + 1);
            int fourthQuoteIndex = line.IndexOf('"', thirdQuoteIndex  + 1);

            // If quotes are found, extract the value between the third and fourth quotes.
            if (thirdQuoteIndex != -1 && fourthQuoteIndex != -1)
            {
                return line.Substring(thirdQuoteIndex + 1, fourthQuoteIndex - thirdQuoteIndex - 1);
            }
            return string.Empty; // Return an empty string if no value is found.
        }

        public static string GetNextLine(this string[] lines, int index)
        {
            var nextIndex = index + 1;
            if (nextIndex <= lines.Length)
                return lines[index + 1].Trim();
        
            else if(nextIndex>=lines.Length)
                throw new IndexOutOfRangeException();
        
            return string.Empty;
        }
    }
}