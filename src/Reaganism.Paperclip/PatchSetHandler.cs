using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

using JetBrains.Annotations;

using Reaganism.CDC.Decompilation;
using Reaganism.CDC.Diffing;
using Reaganism.CDC.Patching;
using Reaganism.CDC.Utilities.Extensions;
using Reaganism.Paperclip.Transformation;
using Reaganism.Paperclip.Workspace;

namespace Reaganism.Paperclip;

/// <summary>
///     Wraps around and handles managing patch-sets.
/// </summary>
[PublicAPI]
public sealed class PatchSetHandler(PatchSet patchSet)
{
    private const string downloads_dir = "downloads";
    private const string sources_dir   = "sources";
    private const string cloned_dir    = "cloned";

    /// <summary>
    ///     All the nodes in the patch-set.
    /// </summary>
    [PublicAPI]
    public IEnumerable<WorkspaceNode> AllNodes
    {
        [PublicAPI] get => PatchSet.GetAllNodes();
    }

    [PublicAPI]
    public PatchSet PatchSet { [PublicAPI] get; } = patchSet;

    [PublicAPI]
    public IEnumerable<T> GetNodesOfType<T>() where T : WorkspaceNode
    {
        return AllNodes.OfType<T>();
    }

    [PublicAPI]
    public void DownloadDepots(string username, string password, string regex)
    {
        File.WriteAllText("filelist.txt", $"regex:{regex}");
        {
            var hashset = new HashSet<(int, int)>();
            foreach (var node in GetNodesOfType<DepotNode>())
            {
                if (!hashset.Add((node.AppId, node.DepotId)))
                {
                    continue;
                }

                DownloadManifest(username, password, node.AppId, node.DepotId);
            }
        }
        File.Delete("filelist.txt");
    }

    public void DecompileDepots(IEnumerable<DepotNode> nodes)
    {
        foreach (var node in nodes)
        {
            var dir = Path.Combine(sources_dir, node.Name);
            Console.WriteLine($"Decompiling {node.Name}...");
            {
                Console.WriteLine("    Transforming assemblies...");
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
                Directory.CreateDirectory(dir);

                var depotDir = Path.Combine(downloads_dir, node.AppId.ToString(), node.DepotId.ToString());
                if (!Directory.Exists(depotDir))
                {
                    throw new DirectoryNotFoundException($"Depot directory not found: {depotDir}");
                }

                var clonedDir = Path.Combine(cloned_dir, node.Name);
                if (Directory.Exists(clonedDir))
                {
                    Directory.Delete(clonedDir, true);
                }
                CopyRecursively(depotDir, clonedDir);

                var exePath = Path.Combine(clonedDir, node.PathToExecutable);

                var transformers = AssemblyTransformer.GetTransformers(node.Transformers);
                var ctx          = AssemblyTransformer.GetAssemblyContextWithUniversalAssemblyResolverFromPath(exePath);
                AssemblyTransformer.TransformAssembly(ctx, transformers);

                var decompilerSettings = new DecompilerSettings
                {
                    CSharpFormattingOptions = FormattingOptionsFactory.CreateAllman(),
                    Ranges                  = false,
                };
                ProjectDecompiler.Decompile(exePath, dir, decompilerSettings, node.DecompiledLibraries.ToArray(), node.ResourceNamespaces.ToArray());
            }
        }
    }

    public void DiffNodes(IEnumerable<WorkspaceNode> nodes)
    {
        foreach (var node in nodes)
        {
            var parent = node.Parent;

            // If there is no parent node to diff against, create an empty
            // directory for this node and continue since there's nothing to
            // diff against.
            if (parent is null)
            {
                Directory.CreateDirectory(node.PatchDir);
                continue;
            }

            Console.WriteLine($"Diffing {node.Name}...");
            if (Directory.Exists(node.PatchDir))
            {
                Directory.Delete(node.PatchDir, true);
            }
            Directory.CreateDirectory(node.PatchDir);

            var differSettings = new DifferSettings(
                    Path.Combine(sources_dir, parent.Name),
                    Path.Combine(sources_dir, node.Name),
                    node.PatchDir
                ).IgnoreCommonDirectories()
                 .HandleCommonFileTypes();
            ProjectDiffer.Diff(differSettings);
        }
    }

    public void PatchNodes(IEnumerable<WorkspaceNode> nodes)
    {
        foreach (var node in nodes)
        {
            var parent = node.Parent;

            // If there is no parent node to patch against, create an empty
            // directory for this node and continue since there's nothing to
            // patch against.
            if (parent is null)
            {
                Directory.CreateDirectory(Path.Combine(sources_dir, node.Name));
                continue;
            }

            Console.WriteLine($"Patching {node.Name}...");
            if (Directory.Exists(node.PatchDir))
            {
                Directory.Delete(node.PatchDir, true);
            }

            if (Directory.Exists(Path.Combine(sources_dir, node.Name)))
            {
                Directory.Delete(Path.Combine(sources_dir, node.Name), true);
            }

            var patcherSettings = new PatcherSettings(
                Path.Combine(sources_dir, parent.Name),
                Path.Combine(sources_dir, node.Name),
                node.PatchDir
            );
            ProjectPatcher.Patch(patcherSettings);
        }
    }

    public IEnumerable<WorkspaceNode> GetNodeWithName(string name)
    {
        return [AllNodes.Single(n => n.Name == name)];
    }

    private static void DownloadManifest(string username, string password, int appId, int depotId)
    {
        var dir = Path.Combine(downloads_dir, appId.ToString(), depotId.ToString());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }

        DepotDownloader.Program.Main(
            [
                "-app", appId.ToString(),
                "-depot", depotId.ToString(),
                "-filelist", "filelist.txt",
                "-username", username,
                "-password", password,
                "-dir", dir,
                // "-remember-password" 
            ]
        );
    }

    private static void CopyRecursively(string fromDir, string toDir)
    {
        foreach (var dir in Directory.GetDirectories(fromDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(fromDir, toDir));
        }

        foreach (var file in Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(fromDir, toDir), true);
        }
    }
}