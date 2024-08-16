using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace Reaganism.Paperclip.Workspace;

/// <summary>
///     Serializable, JSON representation of a workspace node.
/// </summary>
[PublicAPI]
public sealed class MetaNode
{
    /// <summary>
    ///     The kind of node.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    ///     The name of the node.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The directory in which this node's patches reside.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("patchDir")]
    public string PatchDir { get; set; } = string.Empty;

    /// <summary>
    ///     The parent of this node.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("parent")]
    public string? Parent { get; set; } = null;

    /// <summary>
    ///     The custom data associated with this node.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    ///     The children of this node.
    /// </summary>
    [PublicAPI]
    [JsonPropertyName("children")]
    public MetaNode[] Children { get; set; } = [];
}