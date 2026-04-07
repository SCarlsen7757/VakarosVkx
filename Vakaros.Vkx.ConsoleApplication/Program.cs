using System.Text.Json;
using Vakaros.Vkx.Parser;

ReadLine.AutoCompletionHandler = new VkxFileAutoCompleteHandler();

var input = ReadLine.Read("Enter .vkx file path: ");

if (File.Exists(input) && input.EndsWith(".vkx", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine($"File found: {input}");

    var log = Vakaros.Vkx.Parser.VkxParser.ParseFile(input);

    var json = JsonSerializer.Serialize<VkxSession>(log, new JsonSerializerOptions() { WriteIndented = true });
    if (json is null)
    {
        Console.WriteLine("Failed to serialize log to JSON.");
        return;
    }

    var fileName = Path.GetFileNameWithoutExtension(input) + ".json";

    var path = Path.Combine(Path.GetDirectoryName(input)!, fileName);
    File.WriteAllText(path, json);
    Console.WriteLine(json);
}
else
{
    Console.WriteLine("File not found or invalid format.");
}
