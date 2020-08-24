using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StreamNoDiscardAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StreamNoDiscardAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "StreamNoDiscardAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Bugs";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        static bool HasValidParent(InvocationExpressionSyntax invocation)
        {
            return invocation.Parent is EqualsValueClauseSyntax
                || invocation.Parent.Parent is EqualsValueClauseSyntax
                || invocation.Parent.Parent is IfStatementSyntax;
        }

        static bool IsActualReadCallOnSystemIoStreamType(IMethodSymbol methodSymbol)
        {
            var container = methodSymbol.ContainingType ;
            do
            {
                if (container.Name == "Stream" && container.ContainingNamespace.Name == "IO" && container.ContainingNamespace.ContainingNamespace.Name == "System")
                    return true;
                container = container.BaseType;
            } while (container != null);
            return false;
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;
            if ((invocation.Expression as MemberAccessExpressionSyntax).Name.Identifier.ValueText != "Read")
                return; //early return for perf
            var methodSymbol = context
                                .SemanticModel
                                .GetSymbolInfo(invocation)
                                .Symbol as IMethodSymbol;
            if (methodSymbol.ReturnType?.SpecialType != SpecialType.System_Int32)
                return;
            if (!IsActualReadCallOnSystemIoStreamType(methodSymbol))
                return;
            if (HasValidParent(invocation))
                return;
            var d = Diagnostic.Create(Rule, invocation.GetLocation());
            context.ReportDiagnostic(d);
        }
    }
}
