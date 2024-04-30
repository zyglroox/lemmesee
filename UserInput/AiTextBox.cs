using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace LemmeSee.UserInput
{
	internal sealed class AiTextBox
	{
		public const string Tag = "AiTextBoxElement";

		private readonly IAdornmentLayer _layer;
		private readonly IWpfTextView _textView;
		private readonly Border _inputControl;
		private readonly TextBox _inputTextControl;

		private SnapshotSpan _currentSelectedSpan;

		private string _prompt;
		private string _input;

		public Dispatcher Dispatcher => _inputControl?.Dispatcher;

		/// <summary>
		/// Initializes a new instance of the <see cref="AiTextBox"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public AiTextBox(IWpfTextView view)
		{
			this._textView = view ?? throw new ArgumentNullException(nameof(view));
			this._layer = view.GetAdornmentLayer(nameof(AiTextBox));

			_inputTextControl = CreateTextBox();
			_inputControl = CreateUIElementWrapper(_inputTextControl);

			BindKeys(_inputControl);
			BindKeys(_textView.VisualElement);

			_textView.Selection.SelectionChanged += Selection_SelectionChanged;
			_inputTextControl.TextChanged += InputControl_TextChanged;
		}

		#region Create Controls

		private static TextBox CreateTextBox() =>
			new TextBox
			{
				Margin = new Thickness(5),
				Background = UiColors.BgColor,
				Foreground = UiColors.TextColor,
				AcceptsReturn = true,
				AcceptsTab = true,
				FontFamily = new FontFamily("Fira Code"),
				BorderBrush = Brushes.DarkCyan
			};

		private static Border CreateUIElementWrapper(UIElement uiElement)
		{
			var inputGrid = CreateGrid();

			var title = CreateTextBlock();
			Grid.SetColumn(title, 0);
			inputGrid.Children.Add(title);

			Grid.SetColumn(uiElement, 1);
			inputGrid.Children.Add(uiElement);

			return CreateBorder(inputGrid);

			#region Create UI Elements Methods

			Grid CreateGrid()
			{
				var grid = new Grid
				{
					Background = UiColors.BgColor,
					Width = 600,
					Height = 30
				};

				var col1 = new ColumnDefinition
				{
					Width = GridLength.Auto
				};
				var col2 = new ColumnDefinition
				{
					Width = new GridLength(1, GridUnitType.Star)
				};

				grid.ColumnDefinitions.Add(col1);
				grid.ColumnDefinitions.Add(col2);
				return grid;
			}

			TextBlock CreateTextBlock() =>
				new TextBlock
				{
					Text = "Enter prompt:",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.White,
					Background = UiColors.BgColor,
					TextAlignment = TextAlignment.Left
				};

			Border CreateBorder(UIElement child) =>
				new Border
				{
					Child = child,
					BorderBrush = UiColors.BorderColor,
					BorderThickness = new Thickness(2)
				};

			#endregion
		}

		#endregion

		#region Bind Controls Keys

		private void BindKeys(IInputElement inputElement)
		{
			inputElement.PreviewKeyDown += EnterKeyHandling();
			//uiElement.KeyDown += KeyHandling();
			inputElement.PreviewKeyUp += EscKeyHandling();
			//uiElement.KeyUp += KeyHandling();

			KeyEventHandler EnterKeyHandling()
			{
				return (sender, e) =>
				{
					if (!_inputTextControl.IsVisible)
						return;

					switch (e.Key)
					{
						case Key.OemQuestion:
						case Key.Tab:
						case Key.Enter:
							AcceptPrompt();
							Hide();
							e.Handled = true;
							break;
					}
				};
			}

			KeyEventHandler EscKeyHandling()
			{
				return (sender, e) =>
				{
					if (!_inputTextControl.IsVisible)
						return;

					switch (e.Key)
					{
						case Key.Escape:
						case Key.Cancel:
							Hide();
							e.Handled = true;
							break;
					}
				};
			}
		}

		#endregion

		private void AcceptPrompt()
		{
			_prompt = _input;
		}

		//This method is constantly checking whether the user has entered prompt, without freezing the UI thread.
		//After the prompt has been formed it is being returned and nullified
		public async Task<string> GetPromptAsync()
		{
			var result = string.Empty;

			while (string.IsNullOrEmpty(_prompt))
			{
				await DelayedActionOnUiThreadAsync(() => result = _prompt);
			}

			_prompt = null;
			_input = null;

			return result;
		}

		public Task RefactorAsync(Span spanToReplace, string newCode)
		{
			_textView.TextBuffer.Replace(spanToReplace, newCode);
			return Task.CompletedTask;
		}

		public Task Show()
		{
			if (_currentSelectedSpan.IsEmpty || _inputTextControl.IsVisible)
				return Task.CompletedTask;
			// Calculate the position to display the input control
			var bounds = _textView.TextViewLines.GetLineMarkerGeometry(_currentSelectedSpan).Bounds;
			var isReversed = _textView.Selection.IsReversed;
			var position = isReversed ? bounds.TopLeft : bounds.BottomRight;
			var caretPosition = _textView.Caret.Position.VirtualSpaces;
			var offset = _textView.Caret.ContainingTextViewLine.Length - caretPosition;
			Canvas.SetLeft(_inputControl, position.X + offset * 12);
			Canvas.SetTop(_inputControl, position.Y);
			//var position = _textView.TextViewLines.GetMarkerGeometry(_currentSelectedSpan).Bounds.BottomRight;
			//Canvas.SetLeft(_inputControl, position.X);
			//Canvas.SetTop(_inputControl, position.Y);
			// Add the input control to the adornment layer
			this._layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _currentSelectedSpan, Tag, _inputControl, null);
			//_inputTextControl.Focus();
			return Task.CompletedTask;
		}

		public Task Hide()
		{
			if (!_inputTextControl.IsVisible)
				return Task.CompletedTask;
			// Remove the input control from the adornment layer
			_inputTextControl.Text = string.Empty;
			_input = null;
			this._layer.RemoveAdornment(_inputControl);
			return Task.CompletedTask;
		}

		#region Events

		private void Selection_SelectionChanged(object sender, EventArgs e)
		{
			var selection = _textView.Selection;
			if (selection.IsEmpty)
			{
				_currentSelectedSpan = new SnapshotSpan();
				return;
			}

			// Get the selected span of text
			_currentSelectedSpan = selection.SelectedSpans[0];
		}

		private void InputControl_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Update the text in the editor when the input control text changes
			// For example, you can replace the selected text with the input control text
			_input = _inputTextControl.Text;
		}

		#endregion

		private static async Task DelayedActionOnUiThreadAsync(Action action)
		{
			await Task.Delay(TimeSpan.FromSeconds(1));

			Application.Current.Dispatcher.Invoke(action);
		}
	}
}
