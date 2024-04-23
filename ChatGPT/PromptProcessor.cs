using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

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
}
