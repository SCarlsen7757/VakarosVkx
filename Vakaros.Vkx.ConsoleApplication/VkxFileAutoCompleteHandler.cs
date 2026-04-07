class VkxFileAutoCompleteHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public string[] GetSuggestions(string text, int index)
    {
        // text   = full input typed so far
        // index  = start of current word (ReadLine replaces from here onward)
        // text[..index] ends with the last separator, making it a valid directory path
        string dir = index > 0
            ? text[..index]
            : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
        string prefix = text[index..];

        try
        {
            var suggestions = new List<string>();
            foreach (var entry in Directory.GetFileSystemEntries(dir, $"{prefix}*"))
            {
                string name = Path.GetFileName(entry)!;
                if (Directory.Exists(entry))
                    suggestions.Add(name + Path.DirectorySeparatorChar);
                else if (entry.EndsWith(".vkx", StringComparison.OrdinalIgnoreCase))
                    suggestions.Add(name);
            }
            return [.. suggestions];
        }
        catch
        {
            return [];
        }
    }
}