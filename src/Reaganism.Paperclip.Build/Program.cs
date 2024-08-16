using Spectre.Console;

namespace Reaganism.Paperclip.Build;

internal static class Program
{
    public static int Main(string[] args)
    {
        const int argument_id_patches = 1;
        const int max_arguments       = argument_id_patches;

        if (args.Length > max_arguments)
        {
            Console.WriteLine("Usage: paperclip [patches.json]");
            return 1;
        }

        // If no arguments are provided, we must assume that the `patches.json`
        // file or similar is provided within the current working directory.
        var relativePatches = args.Length < argument_id_patches;
        var patchesPath = relativePatches
            ? "patches.json"
            : args[argument_id_patches - 1];

        if (!File.Exists(patchesPath))
        {
            throw new FileNotFoundException($"The specified patches file does not exist (relative = {relativePatches}): {patchesPath}");
        }

        // TODO: read patches file.

        AnsiConsole.Write(
            new Panel("Test")
               .Header($"paperclip [[{patchesPath}{(relativePatches ? " (relative)" : "")}]]")
               .Expand()
               .RoundedBorder()
        );
        return 0;
    }
}