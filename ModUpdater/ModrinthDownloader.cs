using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;


class ModrinthDownloader
{
    private static bool IsModrinthUrl(string url)
    {
        return url.StartsWith("https://modrinth.com/mod/");
    }

    private static void ClearFolder(string folderPath)
    {
        foreach (string filePath in Directory.GetFiles(folderPath))
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete {filePath}. Reason: {e.Message}");
            }
        }
    }

    private static string StripNonAscii(string input)
    {
        StringBuilder output = new StringBuilder();
        foreach (char c in input)
        {
            if (c >= 32 && c <= 126)
            {
                output.Append(c);
            }
        }
        return output.ToString();
    }

    private static List<string> ListFilesInFolder(string folderPath)
    {
        string filePath = Path.Combine(folderPath, "profile.json");
        Console.WriteLine(StripNonAscii(filePath));

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        dynamic mods = JsonConvert.DeserializeObject(json);
        File.WriteAllText("file.json", JsonConvert.SerializeObject(mods, Formatting.Indented));

        return new List<string>(mods);
    }


}