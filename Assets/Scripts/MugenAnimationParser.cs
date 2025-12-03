using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MugenAnimationParser
{
    // Represents one animation frame
    public class Frame
    {
        public int Group;
        public int Index;
        public int XOffset;
        public int YOffset;
        public int Duration;
    }

    // Represents one whole animation
    public class Animation
    {
        public int ActionId;
        public List<Frame> Frames = new List<Frame>();
    }

    // Parse an AIR text file (converted from Fighter Factory)
    public static Dictionary<int, Animation> ParseAirFile(string path)
    {
        var animations = new Dictionary<int, Animation>();

        if (!File.Exists(path))
        {
            Debug.LogError($"AIR file not found: {path}");
            return animations;
        }

        string[] lines = File.ReadAllLines(path);

        Animation currentAnim = null;

        foreach (string raw in lines)
        {
            string line = raw.Trim();

            // skip empty & comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                continue;

            // Start of a new animation block
            if (line.StartsWith("[Begin Action", System.StringComparison.OrdinalIgnoreCase))
            {
                // Example: [Begin Action 200]
                int id = ExtractActionId(line);

                currentAnim = new Animation { ActionId = id };
                animations[id] = currentAnim;
                continue;
            }

            // Parse animation frame inside an animation block
            if (currentAnim != null && IsFrameLine(line))
            {
                var frame = ParseFrameLine(line);
                if (frame != null)
                    currentAnim.Frames.Add(frame);
            }
        }

        return animations;
    }

    private static int ExtractActionId(string line)
    {
        // Example input: [Begin Action 200]
        // Extract "200"
        string digits = "";
        foreach (char c in line)
        {
            if (char.IsDigit(c) || c == '-')
                digits += c;
        }
        return int.Parse(digits);
    }

    private static bool IsFrameLine(string line)
    {
        // AIR frame format (most common):
        // group, index, xOffset, yOffset, time
        // e.g.:  0, 1, 10, 5, 3
        return line.Contains(",");
    }

    private static Frame ParseFrameLine(string line)
    {
        try
        {
            string[] parts = line.Split(',');

            return new Frame()
            {
                Group = int.Parse(parts[0].Trim()),
                Index = int.Parse(parts[1].Trim()),
                XOffset = int.Parse(parts[2].Trim()),
                YOffset = int.Parse(parts[3].Trim()),
                Duration = int.Parse(parts[4].Trim())
            };
        }
        catch
        {
            Debug.LogWarning($"Failed to parse frame line: {line}");
            return null;
        }
    }
}
