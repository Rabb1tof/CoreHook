
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CoreHook.Generator;

[Generator(LanguageNames.CSharp)]
public class HookGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(typeof(GenerateHookAttribute).FullName, IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
                                                      .Where(static m => m is not (null, null));

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (context, source) => Execute(context, source.Item1, source.Item2));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _)
    {
        return node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    //public static void ReportDiagnostics(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<DiagnosticInfo> diagnostics)
    //{
    //    context.RegisterSourceOutput(diagnostics, static (context, diagnostic) =>
    //    {
    //        context.ReportDiagnostic(diagnostic.ToDiagnostic());
    //    });
    //}

    private static (MethodDeclarationSyntax?, AttributeData?) GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {

        var methodDeclarationSyntax = (MethodDeclarationSyntax)context.TargetNode;

        foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                IMethodSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                if (attributeSymbol == null)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == typeof(GenerateHookAttribute).FullName)
                {
                    return (methodDeclarationSyntax, context.Attributes.First());
                }
            }
        }

        return (null, null);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<(MethodDeclarationSyntax, AttributeData)> methods)
    {
        if (methods.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }


        var methodsByClass = methods.GroupBy(method => (ClassDeclarationSyntax)method.Item1.Parent!);

        foreach (var classdecl in methodsByClass)
        {
            var ns = (classdecl.Key.Parent as NamespaceDeclarationSyntax)?.Name ?? (classdecl.Key.Parent as FileScopedNamespaceDeclarationSyntax)?.Name;
            string result = @$"
using System;
using System.Runtime.InteropServices;

using CoreHook.HookDefinition;

namespace {ns};

public partial class {classdecl.Key.Identifier.ValueText} {{";

            foreach (var (meth, attrData) in classdecl)
            {
                string targetDllName = String.Empty;
                var callingConvention = CallingConvention.StdCall;
                var charSet = CharSet.Unicode;
                string? targetMethod = null;
                string? description = null;
                ulong targetRelativeAddress = 0;
                bool setLastError = false;

                foreach (var attrArg in attrData.NamedArguments)
                {
                    switch (attrArg.Key)
                    {
                        case nameof(GenerateHookAttribute.CallingConvention):
                            callingConvention = (CallingConvention)attrArg.Value.Value!; break;

                        case nameof(GenerateHookAttribute.TargetDllName):
                            targetDllName = (string)attrArg.Value.Value; break;

                        case nameof(GenerateHookAttribute.TargetMethod):
                            targetMethod = (string)attrArg.Value.Value; break;

                        case nameof(GenerateHookAttribute.TargetRelativeAddress):
                            targetRelativeAddress = (ulong)attrArg.Value.Value; break;

                        case nameof(GenerateHookAttribute.CharSet):
                            charSet = (CharSet)attrArg.Value.Value!; break;

                        case nameof(GenerateHookAttribute.Description):
                            description = (string)attrArg.Value.Value; break;

                        case nameof(GenerateHookAttribute.SetLastError):
                            setLastError = (bool)attrArg.Value.Value!; break;

                        default:
                            break;

                    }
                }

                result += $@"
    [UnmanagedFunctionPointer(CallingConvention.{callingConvention}, CharSet = CharSet.{charSet}, SetLastError = {(setLastError ? "true" : "false")})]
    public delegate {meth.ReturnType} {meth.Identifier.ValueText}Delegate{meth.ParameterList};

    [DllImport(""{targetDllName}"", EntryPoint = ""{targetMethod}"", CallingConvention = CallingConvention.{callingConvention}, CharSet = CharSet.{charSet}, SetLastError = {(setLastError ? "true" : "false")})]
    private static extern {meth.ReturnType} {meth.Identifier.ValueText}Native{meth.ParameterList};

    [Hook(TargetDllName = ""{targetDllName}"", TargetMethod = ""{targetMethod ?? "null"}"", TargetRelativeAddress = {targetRelativeAddress.ToString("x")}, Description = ""{description}"", DelegateType = typeof({meth.Identifier.ValueText}Delegate))]
    public {meth.ReturnType} {meth.Identifier.ValueText}Hook{meth.ParameterList} 
    {{
        return {meth.Identifier.ValueText}({String.Join(", ", meth.ParameterList.Parameters.Select(param => param.Identifier))});
    }}
";
            }

            result += "}";

            context.AddSource($"{ns}/{classdecl.Key.Identifier.ValueText}.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}
