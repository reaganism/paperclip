namespace Reaganism.Paperclip.Transformation.Transformers;

/// <summary>
///     Makes modifications to known libraries to resolve differences in
///     decompiled code.
/// </summary>
internal sealed class DecompilerParityTransformer : IAssemblyTransformer
{
    bool IAssemblyTransformer.TransformAssembly(in TransformerContext ctx)
    {
        return ctx.Assembly.Name.Name switch
        {
            "mscorlib" => Mscorlib(in ctx),
            "FNA"      => Fna(in ctx),
            _          => false,
        };
    }

    private static bool Mscorlib(in TransformerContext ctx)
    {
        var asm = ctx.Assembly;
        var mod = asm.MainModule;

        var anyEdits = false;
        {
            if (mod.GetType("System.MathF") is { } mathF)
            {
                if (mathF.Fields.FirstOrDefault(x => x.Name == "PI") is { } pi)
                {
                    anyEdits |= mathF.Fields.Remove(pi);
                }
            }

            if (mod.GetType("System.String") is { } @string)
            {
                if (@string.Methods.FirstOrDefault(x => x.Name == "Split" && x.Parameters.Count == 2 && x.Parameters[1].ParameterType.Name == "StringSplitOptions") is { } split)
                {
                    split.Parameters[1].HasDefault = false;
                    split.Parameters[1].IsOptional = false;
                }
            }
        }
        return anyEdits;
    }

    private static bool Fna(in TransformerContext ctx)
    {
        var asm = ctx.Assembly;
        var mod = asm.MainModule;

        var anyEdits = false;
        {
            if (mod.GetType("Microsoft.Xna.Framework.Color") is { } color)
            {
                var ctors = color.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 4).ToArray();

                foreach (var ctor in ctors)
                {
                    var alpha = ctor.Parameters.FirstOrDefault(x => x.Name == "alpha");
                    if (alpha is null || alpha.Name != "alpha")
                    {
                        continue;
                    }

                    alpha.Name = "a";
                    anyEdits   = true;
                }
            }

            if (mod.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch") is { } spriteBatch)
            {
                var method = spriteBatch.Methods.FirstOrDefault(
                    x => x.Name             == "Begin"
                      && x.Parameters.Count == 7
                      && x.Parameters.Any(y => y.ParameterType.Name == "Matrix")
                );

                if (method is null || method.Parameters[6].Name == "transformMatrix")
                {
                    return anyEdits;
                }
                method.Parameters[6].Name = "transformMatrix";
                anyEdits                  = true;
            }
        }
        return anyEdits;
    }
}