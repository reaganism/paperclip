using Reaganism.Paperclip.Workspace;

using Spectre.Console;

namespace Reaganism.Paperclip.Build;

internal static class Program
{
    // Don't download files not relevant to basic decompilation; development
    // should reference files from an actual install.
    private const string file_exclusion_regex = @"^.*(?<!\.xnb)(?<!\.xwb)(?<!\.xsb)(?<!\.xgs)(?<!\.bat)(?<!\.txt)(?<!\.xml)(?<!\.msi)$";

    private const string install_depots   = "Install Depots";
    private const string decompile_depots = "Decompile Depots";
    private const string diff_all_depots  = "Diff All Depots";
    private const string diff_all_mods    = "Diff All Mods";
    private const string diff_patch       = "Diff Single Workspace";
    private const string patch_all_mods   = "Patch All Mods";
    private const string patch_patch      = "Patch Single Workspace";

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

        var handler = new PatchSetHandler(PatchSet.FromFile(patchesPath));
        var task = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
               .Title("Select task")
               .AddChoices(
                    install_depots,
                    decompile_depots,
                    diff_all_depots,
                    diff_all_mods,
                    diff_patch,
                    patch_all_mods,
                    patch_patch
                )
        );
        {
            switch (task)
            {
                // Installs all depots.
                case install_depots:
                    var username = AnsiConsole.Prompt(new TextPrompt<string>("Steam username:"));
                    var password = AnsiConsole.Prompt(new TextPrompt<string>("Steam password:").Secret());
                    handler.DownloadDepots(username, password, file_exclusion_regex);
                    break;

                // Decompiles all depots.
                case decompile_depots:
                    handler.DecompileDepots(handler.GetNodesOfType<DepotNode>());
                    break;

                // Diffs all depots.
                case diff_all_depots:
                    handler.DiffNodes(handler.GetNodesOfType<DepotNode>());
                    break;

                // Diffs all mods.
                case diff_all_mods:
                    handler.DiffNodes(handler.GetNodesOfType<ModNode>());
                    break;

                // Diffs a single workspace.
                case diff_patch:
                    handler.DiffNodes(handler.GetNodeWithName(SelectNode(handler)));
                    break;

                // Applies patches to all mods.
                case patch_all_mods:
                    handler.PatchNodes(handler.GetNodesOfType<ModNode>());
                    break;

                // Applies patches to a single workspace.
                case patch_patch:
                    handler.PatchNodes(handler.GetNodeWithName(SelectNode(handler)));
                    break;

                default:
                    throw new ArgumentException($"Invalid task: {task}");
            }
        }

        return 0;
    }

    private static string SelectNode(PatchSetHandler handler)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
               .Title("Select node")
               .AddChoices(handler.AllNodes.Select(x => x.Name))
        );
    }
}