using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StreamNoDiscardAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StreamNoDiscardAnalyzerCodeFixProvider)), Shared]
    public class StreamNoDiscardAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Capture return value";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(StreamNoDiscardAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach(var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => AddVariableDeclarationForCall(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Solution> AddVariableDeclarationForCall(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var originalSolution = document.Project.Solution;
            var varTypeSyntax = SyntaxFactory.IdentifierName("var");
            var varTypeName = SyntaxFactory.Identifier("readCount");
            var equalsClose = SyntaxFactory.EqualsValueClause(invocation);
            var variableDeclarator = SyntaxFactory.VariableDeclarator(varTypeName, null, equalsClose);
            var variableList = new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(variableDeclarator);
            var variableAssignment = SyntaxFactory.VariableDeclaration(varTypeSyntax, variableList);
            var assignment = SyntaxFactory.LocalDeclarationStatement(variableAssignment);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation.Parent, assignment);
            return originalSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }
    }
}
