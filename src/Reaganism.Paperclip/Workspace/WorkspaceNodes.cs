using System.Text.Json;
using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace Reaganism.Paperclip.Workspace;

/// <summary>
///     A workspace node denoting a declared workspace and how it should be
///     treated.
/// </summary>
[PublicAPI]
public abstract class WorkspaceNode(MetaNode meta)
{
    /// <summary>
    ///     "depot" key; a workspace sourced from a Steam depot.
    /// </summary>
    [PublicAPI]
    public const string KIND_DEPOT = "depot";

    /// <summary>
    ///     "mod" key; a workspace sourced from patches stored and applied
    ///     locally.
    /// </summary>
    [PublicAPI]
    public const string KIND_MOD = "mod";

    /// <summary>
    ///     The node "kind".
    /// </summary>
    /// <seealso cref="KIND_DEPOT"/>
    /// <seealso cref="KIND_MOD"/>
    [PublicAPI]
    public abstract string Kind { get; }

    /// <summary>
    ///     The name of the workspace.
    /// </summary>
    [PublicAPI]
    public string Name
    {
        [PublicAPI] get => Meta.Name;
    }

    /// <summary>
    ///     The directory in which patches for this workspace reside.
    /// </summary>
    [PublicAPI]
    public string PatchDir
    {
        [PublicAPI] get => Meta.PatchDir;
    }

    /// <summary>
    ///     The parent workspace of this workspace.
    /// </summary>
    [PublicAPI]
    public WorkspaceNode? Parent { [PublicAPI] get; [PublicAPI] set; }

    /// <summary>
    ///     The children of this workspace.
    /// </summary>
    [PublicAPI]
    public List<WorkspaceNode> Children { [PublicAPI] get; } = [];

    /// <summary>
    ///     The meta node this node was sourced from.
    /// </summary>
    [PublicAPI]
    public MetaNode Meta { [PublicAPI] get; } = meta;

    /// <summary>
    ///     Creates a workspace node from a meta node.
    /// </summary>
    /// <param name="meta">The meta node to turn into a workspace.</param>
    /// <param name="rootDir">
    ///     The root directory of the patch-set being read, for dependency
    ///     resolution.
    /// </param>
    /// <returns>The workspace node.</returns>
    [PublicAPI]
    public static WorkspaceNode FromMeta(MetaNode meta, string rootDir)
    {
        var node = (WorkspaceNode)(meta.Kind switch
        {
            KIND_DEPOT => new DepotNode(meta),
            KIND_MOD   => new ModNode(meta),
            _          => throw new ArgumentException($"Unknown node kind: {meta.Kind}"),
        });

        foreach (var childMeta in meta.Children)
        {
            var childNode = FromMeta(childMeta, rootDir);
            node.Children.Add(childNode);
            childNode.Parent = node;
        }

        if (!Path.IsPathFullyQualified(meta.PatchDir))
        {
            meta.PatchDir = Path.Combine(rootDir, meta.PatchDir);
        }

        return node;
    }
}

/// <summary>
///     A workspace node denoting a declared workspace sourced from a Steam
///     depot.
/// </summary>
[PublicAPI]
public sealed class DepotNode(MetaNode meta) : WorkspaceNode(meta)
{
    [PublicAPI]
    public override string Kind
    {
        [PublicAPI] get => KIND_DEPOT;
    }

    /// <summary>
    ///     The path to the executable that should be decompiled.
    /// </summary>
    /// <remarks>
    ///     Doesn't necessarily need to be an executable.
    /// </remarks>
    [PublicAPI]
    public string PathToExecutable
    {
        [PublicAPI]
        get
        {
            if (!Meta.Data.TryGetValue("pathToExecutable", out var pathToExecutable))
            {
                return string.Empty;
            }

            if (pathToExecutable is not JsonElement element)
            {
                return string.Empty;
            }

            return element.GetString() ?? string.Empty;
        }
    }

    /// <summary>
    ///     The Steam App ID of the depot.
    /// </summary>
    [PublicAPI]
    public int AppId
    {
        [PublicAPI]
        get
        {
            // (int)(Meta.Data["appId"] as long? ?? 0L);

            if (!Meta.Data.TryGetValue("appId", out var appId))
            {
                return 0;
            }

            if (appId is not JsonElement element)
            {
                return 0;
            }

            return element.GetInt32();
        }
    }

    /// <summary>
    ///     The Steam Depot ID of the depot.
    /// </summary>
    [PublicAPI]
    public int DepotId
    {
        [PublicAPI]
        get
        {
            if (!Meta.Data.TryGetValue("depotId", out var depotId))
            {
                return 0;
            }

            if (depotId is not JsonElement element)
            {
                return 0;
            }

            return element.GetInt32();
        }
    }

    /// <summary>
    ///     The transformers to apply to the decompiled code.
    /// </summary>
    [PublicAPI]
    public List<string> Transformers
    {
        [PublicAPI]
        get
        {
            // Meta.Data["transformers"] as List<string> ?? [];

            if (!Meta.Data.TryGetValue("transformers", out var transformers))
            {
                return [];
            }

            if (transformers is not JsonElement array)
            {
                return [];
            }

            return array.EnumerateArray().Select(x => x.GetString()).ToList()!;
        }
    }

    [PublicAPI]
    public List<string> DecompiledLibraries
    {
        [PublicAPI]
        get
        {
            if (!Meta.Data.TryGetValue("decompiledLibraries", out var decompiledLibraries))
            {
                return [];
            }

            if (decompiledLibraries is not JsonElement array)
            {
                return [];
            }

            return array.EnumerateArray().Select(x => x.GetString()).ToList()!;
        }
    }

    [PublicAPI]
    public List<string> ResourceNamespaces
    {
        [PublicAPI]
        get
        {
            if (!Meta.Data.TryGetValue("resourceNamespaces", out var resourceNamespaces))
            {
                return [];
            }

            if (resourceNamespaces is not JsonElement array)
            {
                return [];
            }

            return array.EnumerateArray().Select(x => x.GetString()).ToList()!;
        }
    }
}

/// <summary>
///     A workspace node denoting a declared workspace sourced from local
///     patches.
/// </summary>
[PublicAPI]
public sealed class ModNode(MetaNode meta) : WorkspaceNode(meta)
{
    [PublicAPI]
    public override string Kind
    {
        [PublicAPI] get => KIND_MOD;
    }
}