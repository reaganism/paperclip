using System.Text;

using Mono.Cecil;

namespace Reaganism.Paperclip.Transformation;

internal sealed class UniversalAssemblyResolver : IAssemblyResolver
{
    private static readonly string[]     caches    = ["GAC_MSIL", "GAC_32", "GAC_64", "GAC"];
    private static readonly string[]     prefixes  = [string.Empty, "v4.0_"];
    private static readonly List<string> gac_paths = GetGacPaths();

    private readonly DefaultAssemblyResolver  baseResolver       = new();
    private readonly List<AssemblyDefinition> embeddedAssemblies = [];

    AssemblyDefinition IAssemblyResolver.Resolve(AssemblyNameReference name)
    {
        return Resolve(name, new ReaderParameters());
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
#pragma warning disable ERP022
        try
        {
            if (baseResolver.Resolve(name, parameters) is { } asm)
            {
                return asm;
            }
        }
        catch
        {
            // ignored
        }
#pragma warning restore ERP022

        {
            var asm = ResolveInternal(name);
            if (asm is not null)
            {
                return asm;
            }

            foreach (var embeddedAssembly in embeddedAssemblies)
            {
                if (embeddedAssembly.Name.Name == name.Name)
                {
                    return embeddedAssembly;
                }
            }
        }

        throw new AssemblyResolutionException(name);
    }

    public void AddSearchDirectory(string directory)
    {
        baseResolver.AddSearchDirectory(directory);
    }

    public void AddEmbeddedAssembliesFrom(ModuleDefinition module)
    {
        foreach (var resource in module.Resources)
        {
            if (resource is not EmbeddedResource embeddedResource)
            {
                continue;
            }

            if (!embeddedResource.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var assembly = AssemblyDefinition.ReadAssembly(
                embeddedResource.GetResourceStream(),
                new ReaderParameters
                {
                    AssemblyResolver = this,
                }
            );
            embeddedAssemblies.Add(assembly);
        }
    }

    void IDisposable.Dispose()
    {
        foreach (var reference in embeddedAssemblies)
        {
            reference.Dispose();
        }

        baseResolver.Dispose();
    }

    private AssemblyDefinition? ResolveInternal(AssemblyNameReference name)
    {
        return GetAssemblyInGac(name);
    }

    private static AssemblyDefinition? GetAssemblyInGac(AssemblyNameReference name)
    {
        return GetAssemblyInNetGac(name);
    }

    private static AssemblyDefinition? GetAssemblyInNetGac(AssemblyNameReference name)
    {
        foreach (var gacPath in gac_paths)
        foreach (var cache in caches)
        foreach (var prefix in prefixes)
        {
            var gac  = Path.Combine(gacPath, cache);
            var file = GetAssemblyFile(name, prefix, gac);
            if (Directory.Exists(gac) && File.Exists(file))
            {
                return AssemblyDefinition.ReadAssembly(file);
            }
        }

        return null;
    }

    private static string GetAssemblyFile(AssemblyNameReference name, string prefix, string gac)
    {
        var gacFolder = new StringBuilder();
        gacFolder.Append(prefix);
        gacFolder.Append(name.Version);
        {
            if (name.PublicKeyToken is { Length: > 0 } publicKeyToken)
            {
                gacFolder.Append("__");
                foreach (var b in publicKeyToken)
                {
                    gacFolder.Append(b.ToString("x2"));
                }
            }
        }
        return Path.Combine(gac, name.Name!, gacFolder.ToString(), name.Name + ".dll");
    }

    private static List<string> GetGacPaths()
    {
        var paths  = new List<string>(2);
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(winDir))
        {
            return paths;
        }

        paths.Add(Path.Combine(winDir, "assembly"));
        paths.Add(Path.Combine(winDir, "Microsoft.NET", "assembly"));
        return paths;
    }
}