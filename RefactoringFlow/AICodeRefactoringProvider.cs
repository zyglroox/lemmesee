using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using LemmeSee.ChatGPT;
using LemmeSee.UserInput;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Document = Microsoft.CodeAnalysis.Document;

namespace LemmeSee.RefactoringFlow
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "AICodeRefactoring"), Shared]
	internal class AICodeRefactoringProvider : CodeRefactoringProvider
	{
		private const string ActionName = "\u2728 Ask an AI";
		private static TextSpan _span;

		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			// Get the root syntax node
			var root = await context.Document.
				GetSyntaxRootAsync(context.CancellationToken).
				ConfigureAwait(false);

			_span = context.Span;

			// Find the node at the selection
			var node = root.FindNode(_span);
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

			var prompt = await InitiateUiFlowAsync(document, node, cancellationToken);
			var response = await GetChatGPTResponse(document, node, cancellationToken, prompt)
				.ConfigureAwait(false);
			//RefactoringsRepository.Save(_span, response);
			await ProcessResponseAsync(root, node, response);

			// Return the document
			return document;
		}

		private static async Task<string> InitiateUiFlowAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			await UiGlobal.ShowTextBox();
			var prompt = await UiGlobal.GetPrompt()
				.ConfigureAwait(false);

			await UiGlobal.HideTextBox();

			return prompt;
		}

		private static async Task<string> GetChatGPTResponse(Document document, SyntaxNode node, CancellationToken cancellationToken,
			string prompt)
		{
			// Get the semantic model for the code file
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
					.ConfigureAwait(false);

			var chat = PromptProcessor.StartConversation();

			return await chat.ProcessAsync(prompt, node.ToString(), semanticModel)
				.ConfigureAwait(false);
		}

		private static async Task ProcessResponseAsync(SyntaxNode root, SyntaxNode node, string response)
		{
			//if (!RefactoringsRepository.TryGet(_span, out var response))
			//	return;

			var parsedResponse = SyntaxTreeHelper.TryParseSyntax(response);
			if (parsedResponse != null)
			{
				var nodeToReplace = SyntaxTreeHelper.GetNodeToReplace(root, node, parsedResponse);
					await UiGlobal.Refactor(new Span(nodeToReplace.SpanStart, nodeToReplace.Span.Length), response);
			}
			else
			{
				var commentedResponse = $"/*{response}*/\r\n{node}";
				await UiGlobal.Refactor(new Span(node.SpanStart, node.Span.Length), commentedResponse);
			}
		}
	}
}
