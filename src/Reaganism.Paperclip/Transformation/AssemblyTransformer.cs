using Mono.Cecil;

using Reaganism.Paperclip.Transformation.Transformers;

namespace Reaganism.Paperclip.Transformation;

internal sealed class AssemblyTransformer
{
    private static readonly Dictionary<string, IAssemblyTransformer> known_transformers = new()
    {
        { "decompiler-parity", new DecompilerParityTransformer() },
    };

    public static AssemblyContext GetAssemblyContextWithUniversalAssemblyResolverFromPath(string assemblyPath)
    {
        var assemblyDir = Path.GetDirectoryName(assemblyPath)!;
        var resolver    = new UniversalAssemblyResolver();
        resolver.AddSearchDirectory(assemblyDir);

        var module = ModuleDefinition.ReadModule(
            assemblyPath,
            new ReaderParameters
            {
                AssemblyResolver = resolver,
            }
        );
        resolver.AddEmbeddedAssembliesFrom(module);

        return new AssemblyContext(module, module.Assembly, resolver, assemblyDir);
    }

    public static IAssemblyTransformer[] GetTransformers(IEnumerable<string> requestedTransformers)
    {
        return requestedTransformers.Select(x => known_transformers[x]).ToArray();
    }

    public static void TransformAssembly(AssemblyContext ctx, params IAssemblyTransformer[] transformers)
    {
        var mod      = ctx.Module;
        var asm      = ctx.Assembly;
        var resolver = ctx.Resolver;

        var pendingWrites = new Dictionary<string, AssemblyDefinition>();
        var streams       = new Dictionary<string, MemoryStream>();

        var referenceDefinitions = mod.AssemblyReferences.Select(x => resolver.Resolve(x)).ToList();

        foreach (var refDef in referenceDefinitions)
        {
            if (TransformAssembly(new TransformerContext(ctx, refDef), transformers))
            {
                pendingWrites.Add(refDef.MainModule.FileName, refDef);
            }
        }

        if (TransformAssembly(new TransformerContext(ctx, asm), transformers))
        {
            pendingWrites.Add(mod.FileName, asm);
        }

        foreach (var (fileName, asmRef) in pendingWrites)
        {
            var ms = new MemoryStream();
            asmRef.Write(ms);

            var path = asmRef.MainModule.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = asmRef.Name.Name + ".dll";
            }

            streams.Add(Path.GetFileName(asmRef.MainModule.FileName), ms);
        }

        foreach (var refDef in referenceDefinitions)
        {
            refDef.Dispose();
        }

        asm.Dispose();

        foreach (var (fileName, stream) in streams)
        {
            using var fs = File.OpenWrite(Path.Combine(ctx.AssemblyDirectory!, fileName));
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(fs);
            stream.Dispose();
        }
    }

    private static bool TransformAssembly(in TransformerContext context, params IAssemblyTransformer[] transformers)
    {
        var anyEdits = false;
        {
            foreach (var transformer in transformers)
            {
                anyEdits |= transformer.TransformAssembly(in context);
            }
        }
        return anyEdits;
    }
}