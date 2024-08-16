using JetBrains.Annotations;

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
}