﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Scopa.Editor
{
    /// <summary>
    /// Appends a unique unityname property and name(based on the classname) if one does not exist
    /// on each non brush map entity
    /// </summary>
    public static class UniqueNamePreprocessor
    {
        /// <summary>
        /// Safely reads all lines from a file, handling cases where the file might be in use by another process
        /// </summary>
        private static string[] SafeReadAllLines(string path)
        {
            const int maxRetries = 5;
            const int baseDelayMs = 50;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream);
                    var lines = new List<string>();
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    return lines.ToArray();
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // Wait with exponential backoff before retrying
                    Thread.Sleep(baseDelayMs * (int)Math.Pow(2, attempt));
                }
            }

            // If all retries failed, fall back to the original method and let the exception propagate
            return File.ReadAllLines(path);
        }

        /// <summary>
        /// Safely writes all lines to a file, handling cases where the file might be in use by another process
        /// </summary>
        private static void SafeWriteAllLines(string path, IEnumerable<string> lines)
        {
            const int maxRetries = 5;
            const int baseDelayMs = 50;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(fileStream);
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    return;
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // Wait with exponential backoff before retrying
                    Thread.Sleep(baseDelayMs * (int)Math.Pow(2, attempt));
                }
            }

            // If all retries failed, fall back to the original method and let the exception propagate
            File.WriteAllLines(path, lines);
        }

        public static void Parse(string path)
        {
            var lines        = SafeReadAllLines(path);
            var classes      = new Dictionary<string, int>();
            var existingNames = new HashSet<string>();
            var updatedLines = new List<string>();

            // Collect all current names in a HashSet for faster lookup
            foreach (var line in lines)
                if (line.BeginsWith("unityname"))
                    existingNames.Add(line.GetValue());

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

                var hasUnityName = lines.GetNextLine(i).BeginsWith("unityname");
                if (hasUnityName)
                    continue;

                // Increment the count for this class name
                if (!classes.ContainsKey(classType))
                    classes.Add(classType, 1);
                else
                    classes[classType]++;

                // Create a unique unityname using classType and count
                var uniqueUnityName = $"{classType} {classes[classType]}";

                // Ensure the unityname is unique against all existing names by incrementing the number
                while (existingNames.Contains(uniqueUnityName))
                {
                    classes[classType]++;
                    uniqueUnityName = $"{classType} {classes[classType]}";
                }

                // Add the new unique name to the existing names set
                existingNames.Add(uniqueUnityName);

                // Insert a new line with the unique unityname
                var newUnityNameLine = $"\"unityname\" \"{uniqueUnityName}\"";
                updatedLines.Add(newUnityNameLine);
            }

            // Overwrite the original file with the updated lines
            SafeWriteAllLines(path, updatedLines);
        }

        
        // New method to check for duplicate names and rename them, plus clean up parentheses
        public static void RenameDuplicateUnityNames(string path)
        {
            var lines = SafeReadAllLines(path);
            var nameOccurrences = new Dictionary<string, List<int>>();
            var updatedLines = new List<string>();
            var allExistingNames = new HashSet<string>();
            var namesToCleanup = new List<(int lineIndex, string originalName)>();

            // First pass: collect all unityname lines and their positions
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                updatedLines.Add(line);

                if (line.BeginsWith("unityname"))
                {
                    var value = line.GetValue();
                    allExistingNames.Add(value);

                    // Track names that need cleanup (have parentheses)
                    if (value.Contains(" ("))
                    {
                        namesToCleanup.Add((updatedLines.Count - 1, value));
                    }

                    if (!nameOccurrences.ContainsKey(value))
                    {
                        nameOccurrences[value] = new List<int>();
                    }
                    nameOccurrences[value].Add(updatedLines.Count - 1);
                }
            }

            // Second pass: rename all duplicates by continuing the numbering sequence
            foreach (var kvp in nameOccurrences)
            {
                var originalName = kvp.Key;
                var lineIndices = kvp.Value;

                // Only process if there are duplicates
                if (lineIndices.Count > 1)
                {
                    // Parse the original name to get base name and starting number
                    var (baseName, startingNumber) = ParseUnityName(originalName);

                    // Find the highest existing number for this base name across ALL names (not just duplicates)
                    int highestNumber = FindHighestNumberForBaseName(baseName, allExistingNames);

                    // Start renaming from the highest number + 1
                    int currentNumber = highestNumber + 1;

                    // Rename ALL duplicate occurrences (including the first one) to clean sequential numbering
                    for (int i = 0; i < lineIndices.Count; i++)
                    {
                        var lineIndex = lineIndices[i];
                        string newUnityName;

                        // Keep generating new names until we find one that doesn't exist
                        do
                        {
                            newUnityName = $"{baseName} {currentNumber}";
                            currentNumber++;
                        } while (allExistingNames.Contains(newUnityName));

                        updatedLines[lineIndex] = $"\"unityname\" \"{newUnityName}\"";

                        // Remove the old name and add the new name to track it
                        allExistingNames.Remove(originalName);
                        allExistingNames.Add(newUnityName);
                    }
                }
            }

            // Third pass: clean up any remaining names with parentheses that weren't duplicates
            foreach (var (lineIndex, originalName) in namesToCleanup)
            {
                // Check if this line was already processed in the duplicate renaming
                var currentLineContent = updatedLines[lineIndex];
                if (currentLineContent.Contains(" ("))
                {
                    // Parse the name to get clean base name and number
                    var (baseName, number) = ParseUnityName(originalName);

                    // Create clean name without parentheses
                    var cleanName = $"{baseName} {number}";

                    // Make sure the clean name doesn't conflict with existing names
                    while (allExistingNames.Contains(cleanName))
                    {
                        number++;
                        cleanName = $"{baseName} {number}";
                    }

                    // Update the line with clean name
                    updatedLines[lineIndex] = $"\"unityname\" \"{cleanName}\"";
                    allExistingNames.Remove(originalName);
                    allExistingNames.Add(cleanName);
                }
            }

            // Overwrite the file with the updated lines
            SafeWriteAllLines(path, updatedLines);
        }

        // Helper method to parse unity name into base name and number
        private static (string baseName, int number) ParseUnityName(string unityName)
        {
            // First, check if the name has parentheses (from the old naming system)
            var parenIndex = unityName.IndexOf(" (");
            if (parenIndex > 0)
            {
                // Extract the part before the parentheses
                var nameBeforeParens = unityName.Substring(0, parenIndex);

                // Now parse the name before parentheses to get base name and number
                var lastSpaceIndex = nameBeforeParens.LastIndexOf(' ');
                if (lastSpaceIndex > 0 && lastSpaceIndex < nameBeforeParens.Length - 1)
                {
                    var potentialNumber = nameBeforeParens.Substring(lastSpaceIndex + 1);
                    if (int.TryParse(potentialNumber, out int number))
                    {
                        var baseName = nameBeforeParens.Substring(0, lastSpaceIndex);
                        return (baseName, number);
                    }
                }

                // If no number found before parentheses, treat the whole part as base name
                return (nameBeforeParens, 1);
            }

            // No parentheses, look for the last space followed by a number
            var lastSpaceIndexNormal = unityName.LastIndexOf(' ');
            if (lastSpaceIndexNormal > 0 && lastSpaceIndexNormal < unityName.Length - 1)
            {
                var potentialNumber = unityName.Substring(lastSpaceIndexNormal + 1);
                if (int.TryParse(potentialNumber, out int number))
                {
                    var baseName = unityName.Substring(0, lastSpaceIndexNormal);
                    return (baseName, number);
                }
            }

            // If no number found, treat the whole name as base name with number 1
            return (unityName, 1);
        }

        // Helper method to find the highest number used for a given base name
        private static int FindHighestNumberForBaseName(string baseName, HashSet<string> existingNames)
        {
            int highest = 0;
            foreach (var name in existingNames)
            {
                // Parse each existing name to extract its base name and number
                var (nameBase, number) = ParseUnityName(name);

                // If the base names match, track the highest number
                if (nameBase.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    highest = Math.Max(highest, number);
                }
            }
            return highest;
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