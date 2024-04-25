using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using LemmeSee.ChatGPT;
using LemmeSee.UserInput;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Document = Microsoft.CodeAnalysis.Document;

namespace LemmeSee.RefactoringFlow
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "AICodeRefactoring"), Shared]
	internal class AICodeRefactoringProvider : CodeRefactoringProvider
	{
		private static string _response;
		private static bool _processing;
		private const string ActionName = "\u2728 Ask an AI";

		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			// Get the root syntax node
			var root = await context.Document.
				GetSyntaxRootAsync(context.CancellationToken).
				ConfigureAwait(false);

			// Find the node at the selection
			var node = root.FindNode(context.Span);

			var action = CodeAction.Create(ActionName,
					c => ProcessPromptAsync(context.Document, root, node, c));

			context.RegisterRefactoring(action);
		}

		private static async Task<Document> ProcessPromptAsync(
			Document document,
			SyntaxNode root,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			if (node == null)
				return document;

			var newDocument = document;

			if (!_processing)
			{
				_processing = true;
				_response = await InitiateUiFlowAsync(document, node, cancellationToken);
			}
			else if (!string.IsNullOrEmpty(_response))
			{
				newDocument = await ProcessResponseAsync(document, root, node, cancellationToken);
				_processing = false;
			}

			// Return the document
			return newDocument;
		}

		private static async Task<string> InitiateUiFlowAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			await UiGlobal.ShowTextBox();
			var prompt = await UiGlobal.GetPrompt();

			await UiGlobal.HideTextBox();

			// Get the semantic model for the code file
			var semanticModel =
				await document.GetSemanticModelAsync(cancellationToken)
					.ConfigureAwait(false);

			var chat = new PromptProcessor().NewConversation();

			return await chat.ProcessAsync(prompt, node.ToString(), semanticModel).ConfigureAwait(false);
		}

		private static async Task<Document> ProcessResponseAsync(Document document, SyntaxNode root, SyntaxNode node, CancellationToken cancellationToken)
		{
			var parsedResponse = SyntaxTreeHelper.TryParseSyntax(_response);
			if (parsedResponse == null)
			{
				return document;
			}

			var nodeToReplace = SyntaxTreeHelper.GetNodeToReplace(root, node, parsedResponse);

			var oldDocAsText = await document.GetTextAsync(cancellationToken)
				.ConfigureAwait(false);

			// Generate a new document
			return document
				.WithText(oldDocAsText.Replace(nodeToReplace.Span, parsedResponse.ToString()));
		}
	}
}
