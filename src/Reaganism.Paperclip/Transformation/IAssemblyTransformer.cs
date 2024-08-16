namespace Reaganism.Paperclip.Transformation;

internal interface IAssemblyTransformer
{
    bool TransformAssembly(in TransformerContext ctx);
}