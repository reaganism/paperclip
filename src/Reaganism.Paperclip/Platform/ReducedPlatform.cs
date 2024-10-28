/*
using DepotDownloader.Platform;

namespace Reaganism.Paperclip.Platform;

/// <summary>
///     A reduced platform implementation that wraps around a real
///     <see cref="IPlatform"/> implementation and avoids unnecessary operations
///     in the context of paperclip.
/// </summary>
/// <param name="platform">The platform to wrap.</param>
internal sealed class ReducedPlatform(IPlatform platform) : IPlatform
{
    void IPlatform.SetExecutable(string path, bool value)
    {
        // Don't do anything in the context of paperclip.  We don't need
        // executables to actually be executable.
    }
}
*/