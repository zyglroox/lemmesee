using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using LemmeSee.ChatGPT;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;
using Microsoft.CodeAnalysis.CSharp;

namespace LemmeSee
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "AICodeRefactoring"), Shared]
	internal class AICodeRefactoringProvider : CodeRefactoringProvider
	{
		private static string _response;
		private static string _actionName;
		private static TextSpan _prevSpan;

		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			// Get the root syntax node
			var root = await context.Document.
				GetSyntaxRootAsync(context.CancellationToken).
				ConfigureAwait(false);

			// Find the node at the selection
			var node = root.FindNode(context.Span);

			if (!string.IsNullOrEmpty(_response) && !_prevSpan.Equals(context.Span))
			{
				_response = null;
			}
			else
			{
				_prevSpan = context.Span;
			}

			var action =
				CodeAction.Create(GetActionName(),
					c => ProcessCode(context.
						Document, node, c));

			context.RegisterRefactoring(action);
		}

		private string GetActionName() => string.IsNullOrEmpty(_response)
			? "\u2728 Ask an AI"
			: "\u2728 Get an AI suggestion";

		private static async Task<Document> ProcessCode(
			Document document,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			if (node == null)
				return document;

			if (string.IsNullOrEmpty(_response))
			{
				await UiGlobal.ShowTextBox();

				var prompt = await UiGlobal.GetPrompt();

				await UiGlobal.HideTextBox();

				// Get the semantic model for the code file
				var semanticModel =
					await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

				var chat = new PromptProcessor().NewConversation();

				_response = await chat.Process(prompt, node.ToString(), semanticModel);
			}

			//var newNode = await SyntaxFactory.ParseSyntaxTree(response).GetRootAsync(cancellationToken);

			var parsedResponse = TryParseSyntax(_response);
			if (parsedResponse == null)
			{
				return document;
			}

			// Get the root syntax node
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			var nodeToReplace = GetNodeToReplace(root, node, parsedResponse);

			//var newRoot = root.ReplaceNode(nodeToReplace, parsedResponse);

			var oldDocAsText = await document.GetTextAsync(cancellationToken);

			// Generate a new document
			var newDocument =
				document.WithText(oldDocAsText.Replace(nodeToReplace.Span, parsedResponse.ToString()));

			// Return the document
			return newDocument;
		}

		public static SyntaxNode TryParseSyntax(string code)
		{
			// Try to parse the code string into a syntax tree
			var syntaxTree = CSharpSyntaxTree.ParseText(code);
			var root = syntaxTree.GetRoot();

			// Check if there are any syntax errors in the tree
			if (syntaxTree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
			{
				// Parsing failed due to syntax errors
				return null;
			}

			// Return the root node of the syntax tree
			return root;
		}

		public static SyntaxNode GetNodeToReplace(SyntaxNode root, SyntaxNode originalNode, SyntaxNode newNode)
		{
			var nodeToReplace = originalNode;
			var newNodeType = newNode.GetType().Name;
			if (newNodeType == "CompilationUnitSyntax" && newNode.ChildNodes().Any())
				newNodeType = newNode.ChildNodes().First().GetType().Name;

			while (true)
			{
				if (nodeToReplace.GetType().Name == newNodeType)
				{
					return nodeToReplace;
				}

				if (nodeToReplace.Parent == null)
				{
					if (root.GetType().Name == newNodeType)
						return root;
				}

				nodeToReplace = nodeToReplace.Parent;
			}
		}
	}
}
