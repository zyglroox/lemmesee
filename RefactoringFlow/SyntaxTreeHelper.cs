using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace LemmeSee.RefactoringFlow
{
	internal static class SyntaxTreeHelper
	{
		public static SyntaxNode TryParseSyntax(string code)
		{
			// Try to parse the code string into a syntax tree
			var syntaxTree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None));

			// Check if there are any syntax errors in the tree
			return syntaxTree.GetDiagnostics()
				.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
				// Parsing failed due to syntax errors
				? null
				// Return the root node of the syntax tree
				: syntaxTree.GetRoot();
		}

		public static SyntaxNode GetNodeToReplace(SyntaxNode root, SyntaxNode originalNode, SyntaxNode newNode)
		{
			var nodeToReplace = originalNode ?? throw new ArgumentNullException(nameof(originalNode));
			var newNodeType = newNode.GetType().Name;

			if (newNodeType == "CompilationUnitSyntax" && newNode.ChildNodes().Any())
				newNodeType = newNode.ChildNodes().First().GetType().Name;

			while (nodeToReplace.Parent != null)
			{
				if (nodeToReplace.GetType().Name == newNodeType)
				{
					return nodeToReplace;
				}

				nodeToReplace = nodeToReplace.Parent;
			}

			return root;
		}
	}
}
