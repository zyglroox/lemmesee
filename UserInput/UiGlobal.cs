using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LemmeSee.UserInput
{
	internal class UiGlobal
	{
		private static AiTextBox _textBox;
		private static Dispatcher _dispatcher;

		public static void SetUiElement(AiTextBox aiTextBox)
		{
			_textBox = aiTextBox;
			_dispatcher = aiTextBox.Dispatcher;
		}

		public static void GetUiElement(Action<AiTextBox> action)
		{
			_dispatcher.Invoke(() => action(_textBox));
		}

		public static Task ShowTextBox()
		{
			return _dispatcher.Invoke(() => _textBox.Show());
		}

		public static Task HideTextBox()
		{
			return _dispatcher.Invoke(() => _textBox.Hide());
		}

		public static Task<string> GetPrompt()
		{
			return _dispatcher.Invoke(() => _textBox.GetPromptAsync());
		}
	}
}