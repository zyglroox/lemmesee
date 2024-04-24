using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace LemmeSee
{
	/// <summary>
	/// TextAdornment1 places red boxes behind all the "a"s in the editor window
	/// </summary>
	internal sealed class AiTextBox
	{
		/// <summary>
		/// The layer of the adornment.
		/// </summary>
		private readonly IAdornmentLayer _layer;

		/// <summary>
		/// Text view where the adornment is created.
		/// </summary>
		private readonly IWpfTextView _textView;

		private TextBox _inputTextControl;
		private readonly Border _inputControl;
		private SnapshotSpan _currentSelectedSpan;

		private string _prompt;
		private string _input;

		public const string Tag = "AiTextBoxElement";

		public Dispatcher Dispatcher => _inputControl?.Dispatcher;

		/// <summary>
		/// Initializes a new instance of the <see cref="AiTextBox"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public AiTextBox(IWpfTextView view)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			this._layer = view.GetAdornmentLayer("AiTextBox");

			this._textView = view;

			// Initialize your input control
			_inputControl = GetUiElement();

			_textView.Selection.SelectionChanged += Selection_SelectionChanged;
			// Add event handlers for user input
			_inputTextControl.TextChanged += InputControl_TextChanged;
		}

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

		private Border GetUiElement()
		{
			var bgColor = ConvertDrawingColorToMediaColor(EnvironmentColors.ToolWindowBackgroundColorKey);
			var textColor = ConvertDrawingColorToMediaColor(EnvironmentColors.EditorExpansionTextColorKey);
			var borderColor = ConvertDrawingColorToMediaColor(EnvironmentColors.AccentBorderColorKey);
			var inputGrid = new Grid
			{
				Background = bgColor, // Set background color
				Width = 600,
				Height = 30
			};
			
			var col1 = new ColumnDefinition
			{
				Width = GridLength.Auto // Auto width for TextBlock
			};
			var col2 = new ColumnDefinition
			{
				Width = new GridLength(1, GridUnitType.Star)
			};
			inputGrid.ColumnDefinitions.Add(col1);
			inputGrid.ColumnDefinitions.Add(col2);

			var title = new TextBlock
			{
				Text = "Enter text:",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brushes.White,
				Background = bgColor,
				TextAlignment = TextAlignment.Left
			};

			_inputTextControl = new TextBox
			{
				Margin = new Thickness(5), // Add some margin
				Background = bgColor,
				Foreground = textColor,
				AcceptsReturn = true,
				AcceptsTab = true,
				FontFamily = new FontFamily("Fira Code"),
				BorderBrush = Brushes.DarkCyan
			};

			Grid.SetColumn(title, 0);
			inputGrid.Children.Add(title);
			Grid.SetColumn(_inputTextControl, 1);
			inputGrid.Children.Add(_inputTextControl);

			var control = new Border
			{
				Child = inputGrid,
				BorderBrush = borderColor, // Set border color
				BorderThickness = new Thickness(2)
			};

			BindKeys(control);
			BindKeys(_textView.VisualElement);

			return control;

			void BindKeys(IInputElement uiElement)
			{
				uiElement.PreviewKeyDown += EnterKeyHandling();
				//uiElement.KeyDown += KeyHandling();
				uiElement.PreviewKeyUp += EscKeyHandling();
				//uiElement.KeyUp += KeyHandling();
			}

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
							FormPrompt();
							HideInputControl();
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
							HideInputControl();
							e.Handled = true;
							break;
					}
				};
			}
		}

		private void FormPrompt()
		{
			_prompt = _input;
		}

		private static Brush ConvertDrawingColorToMediaColor(ThemeResourceKey themeKey)
		{
			var drawingColor = VSColorTheme.GetThemedColor(themeKey);
			var color = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
			return new SolidColorBrush(color);
		}

		public Task Show()
		{
			HideInputControl();
			if (_currentSelectedSpan.IsEmpty)
				return Task.CompletedTask;
			// Calculate the position to display the input control
			var bounds = _textView.TextViewLines.GetLineMarkerGeometry(_currentSelectedSpan).Bounds;
			var isReversed = _textView.Selection.IsReversed;
			var position = isReversed ? bounds.TopLeft : bounds.BottomRight;
			var caretPosition = _textView.Caret.Position.VirtualSpaces;
			var offset = _textView.Caret.ContainingTextViewLine.Length - caretPosition;
			Canvas.SetLeft(_inputControl, position.X + offset * 17);
			Canvas.SetTop(_inputControl, position.Y);
			//var position = _textView.TextViewLines.GetMarkerGeometry(_currentSelectedSpan).Bounds.BottomRight;
			//Canvas.SetLeft(_inputControl, position.X);
			//Canvas.SetTop(_inputControl, position.Y);
			// Add the input control to the adornment layer
			this._layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, _currentSelectedSpan, Tag, _inputControl, null);
			//_inputTextControl.Focus();
			return Task.CompletedTask;
		}

		public async Task HideInputControl()
		{
			// Remove the input control from the adornment layer
			_inputTextControl.Text = string.Empty;
			_input = null;
			this._layer.RemoveAdornment(_inputControl);
			await Task.CompletedTask;
		}

		private void InputControl_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Update the text in the editor when the input control text changes
			// For example, you can replace the selected text with the input control text
			_input = _inputTextControl.Text;
		}

		//This method is constantly checking whether the user has entered prompt, without freezing the UI thread.
		//After the prompt has been formed it is being returned and nullified
		public async Task<string> GetPrompt()
		{
			var result = string.Empty;

			while (string.IsNullOrEmpty(_prompt))
			{
				await DelayedActionOnUiThread(Action);
			}

			_prompt = null;
			_input = null;

			return result;

			void Action()
			{
				// Code to be executed on the UI thread after the delay
				// For example, update UI elements or perform UI-related tasks
				result = _prompt;
			}
		}

		private static async Task DelayedActionOnUiThread(Action action)
		{
			await Task.Delay(TimeSpan.FromSeconds(1));

			Application.Current.Dispatcher.Invoke(action);
		}
	}
}
