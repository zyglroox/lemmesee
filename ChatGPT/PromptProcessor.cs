using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using OpenAI_API;
using OpenAI_API.Chat;

namespace LemmeSee.ChatGPT
{
	internal class PromptProcessor
	{
		private readonly OpenAIAPI _api = new OpenAIAPI("");

		public Conversation NewConversation()
		{
			var chat = _api.Chat.CreateConversation();
			chat.RequestParameters.Temperature = 0.1;
			chat.Model.ModelID = "gpt-4";
			chat.AppendSystemMessage("You are a programmer assistant. Your role is to analyze a piece of code and see the prompt attached to it. Then process this code according to the prompt.");
			chat.AppendSystemMessage("Please return the revised code and if you have any comments regarding your work, make it as short and understandable as possible and place it as a comment either before the code or along the way.");
			chat.AppendSystemMessage("Send the code back in the form it can go directly into IDE, as a plain text not in the 'code' text-box element");
			chat.AppendSystemMessage("Don't use any words/sentences/phrases that are not necessary, you don't have to greet or be welcoming, just strict response");
			return chat;
		}
	}

	internal static class PromptHelper
	{
		public static async Task<string> Process(this Conversation chat,
			string prompt,
			string code,
			SemanticModel semanticModel)
		{
			chat.AppendUserInput($"Hi! {prompt}");
			chat.AppendUserInput($"The code is in {semanticModel.Language} language");
			chat.AppendUserInput($"Here's the code: {code}");
			chat.AppendUserInput($"Here's the context: {semanticModel.SyntaxTree}");
			if (prompt.Contains("explain"))
				chat.AppendUserInput($"Please, return revised version of this code, if you have any thoughts on it, include them as comments in the code");
			var result = await chat.GetResponseFromChatbotAsync();
			return result.Replace("```csharp", string.Empty)
				.Replace("```", string.Empty);
		}
	}
}
