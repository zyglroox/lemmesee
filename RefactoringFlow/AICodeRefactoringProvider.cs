using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using LemmeSee.ChatGPT;
using LemmeSee.UserInput;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;

namespace LemmeSee.RefactoringFlow
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "AICodeRefactoring"), Shared]
	internal class AICodeRefactoringProvider : CodeRefactoringProvider
	{
		private static string _response;
		private static string _actionName;
		private static TextSpan _prevSpan;

		private static string GetActionName() => string.IsNullOrEmpty(_response)
			? "\u2728 Ask an AI"
			: "\u2728 Get an AI suggestion";

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
				_actionName = GetActionName();
			}
			else
			{
				_prevSpan = context.Span;
				_actionName = GetActionName();
			}

			var action = CodeAction.Create(_actionName,
					c => ProcessCode(context.
						Document, node, c));

			context.RegisterRefactoring(action);
		}

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

			var parsedResponse = SyntaxTreeHelper.TryParseSyntax(_response);
			if (parsedResponse == null)
			{
				return document;
			}

			// Get the root syntax node
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			var nodeToReplace = SyntaxTreeHelper.GetNodeToReplace(root, node, parsedResponse);

			//var newRoot = root.ReplaceNode(nodeToReplace, parsedResponse);

			var oldDocAsText = await document.GetTextAsync(cancellationToken);

			// Generate a new document
			var newDocument = document
					.WithText(oldDocAsText.Replace(nodeToReplace.Span, parsedResponse.ToString()));

			// Return the document
			return newDocument;
		}
	}
}
