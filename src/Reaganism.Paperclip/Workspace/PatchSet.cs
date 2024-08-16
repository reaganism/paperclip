using System.Collections;
using System.Text.Json;

using JetBrains.Annotations;

namespace Reaganism.Paperclip.Workspace;

/// <summary>
///     A patch-set; that is, a set of "patches" (defined workspaces).
/// </summary>
[PublicAPI]
public sealed class PatchSet
{
    /// <summary>
    ///     The patch-sets this patch-set depends on.
    /// </summary>
    [PublicAPI]
    public List<PatchSet> Dependencies { [PublicAPI] get; } = [];

    /// <summary>
    ///     The workspaces this patch-set declares.
    /// </summary>
    [PublicAPI]
    public List<WorkspaceNode> Nodes { [PublicAPI] get; } = [];

    /// <summary>
    ///     The top-level nodes in this patch-set, including its dependencies.
    /// </summary>
    [PublicAPI]
    public IEnumerable<WorkspaceNode> TopLevelNodes
    {
        [PublicAPI]
        get
        {
            foreach (var node in Nodes)
            {
                if (node.Parent is null)
                {
                    yield return node;
                }
            }

            foreach (var dependency in Dependencies)
            {
                foreach (var node in dependency.TopLevelNodes)
                {
                    yield return node;
                }
            }
        }
    }

    /// <summary>
    ///     Gets all nodes related to the given node, or all nodes if none is
    ///     specified.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>All nodes.</returns>
    [PublicAPI]
    public IEnumerable<WorkspaceNode> GetAllNodes(WorkspaceNode? node = null)
    {
        if (node is null)
        {
            foreach (var child in TopLevelNodes.SelectMany(GetAllNodes))
            {
                yield return child;
            }
        }
        else
        {
            yield return node;

            foreach (var child in node.Children.SelectMany(GetAllNodes))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    ///     Creates a patch-set from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    [PublicAPI]
    public static PatchSet FromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Patch-set file not found.", path);
        }

        return FromJson(File.ReadAllText(path), Path.GetDirectoryName(Path.GetFileName(path)) ?? string.Empty);
    }

    /// <summary>
    ///     Creates a patch-set from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="rootDir">The root directory of this patch-set.</param>
    [PublicAPI]
    public static PatchSet FromJson(string json, string rootDir)
    {
        var meta = JsonSerializer.Deserialize<MetaSet>(json);
        if (meta is null)
        {
            throw new JsonException("Failed to deserialize patch-set.");
        }

        return FromMeta(meta, rootDir);
    }

    /// <summary>
    ///     Creates a patch-set from a meta-set.
    /// </summary>
    /// <param name="meta">The meta-set.</param>
    /// <param name="rootDir">The root directory of this patch-set.</param>
    [PublicAPI]
    public static PatchSet FromMeta(MetaSet meta, string rootDir)
    {
        var patchSet = new PatchSet();

        foreach (var dependencyPath in meta.Dependencies)
        {
            var fullyQualifiedDependencyPath = dependencyPath;
            if (!Path.IsPathFullyQualified(fullyQualifiedDependencyPath))
            {
                fullyQualifiedDependencyPath = Path.Combine(rootDir, fullyQualifiedDependencyPath);
            }

            var dependency = FromFile(fullyQualifiedDependencyPath);
            patchSet.Dependencies.Add(dependency);
        }

        foreach (var nodeMeta in meta.Nodes)
        {
            var node = WorkspaceNode.FromMeta(nodeMeta, rootDir);
            patchSet.Nodes.Add(node);

            // If the node already has a parent or the meta does not declare one
            // then we don't need to resolve a parent here.
            if (node.Parent is not null || node.Meta.Parent is null)
            {
                continue;
            }

            var parent = patchSet.GetAllNodes().First(x => x.Meta.Name == node.Meta.Parent);
            node.Parent = parent;
            parent.Children.Add(node);
        }

        return patchSet;
    }
}