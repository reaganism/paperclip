using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace Reaganism.Paperclip.Workspace;

/// <summary>
///     Serializable, JSON representation of a patch-set.
/// </summary>
[PublicAPI]
public sealed class MetaSet
{
    /// <summary>
    ///     The name of the patch-set.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("dependencies")]
    public string[] Dependencies { get; set; } = [];

    /// <summary>
    ///     The nodes in the patch-set.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("nodes")]
    public MetaNode[] Nodes { get; set; } = [];
}