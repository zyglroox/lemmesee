using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace LemmeSee
{
	//[Export(typeof(IKeyProcessorProvider))]
	//[TextViewRole(PredefinedTextViewRoles.Document)]
	//[ContentType("any")]
	//[Name("ButtonProvider")]
	//[Order(Before = "default")]
	//internal class ButtonProvider : IKeyProcessorProvider
	//{
	//	[ImportingConstructor]
	//	public ButtonProvider()
	//	{
	//	}

	//	public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
	//	{
	//		return new ButtonKeyProc(wpfTextView);
	//	}
	//}


	//internal class ButtonKeyProc : KeyProcessor
	//{
	//	internal static event KeyEventHandler KeyDownEvent;

	//	public ButtonKeyProc(ITextView textView)
	//	{
	//	}

	//	public override void KeyDown(KeyEventArgs args)
	//	{
	//		if (args.Key == Key.E && IsAlt)
	//		{
	//			if (KeyDownEvent != null)
	//			{
	//				KeyDownEvent(this, args);
	//			}
	//		}
	//	}

	//	public bool IsAlt => Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
	//}
}
