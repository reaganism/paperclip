using Mono.Cecil;

namespace Reaganism.Paperclip.Transformation;

internal readonly record struct AssemblyContext(ModuleDefinition Module, AssemblyDefinition Assembly, IAssemblyResolver Resolver, string AssemblyDirectory);

internal readonly record struct TransformerContext(AssemblyContext AssemblyContext, AssemblyDefinition Assembly);