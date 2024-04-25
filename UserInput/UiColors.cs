using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;

namespace LemmeSee.UserInput
{
	internal class UiColors
	{
		public static readonly Brush BgColor = ConvertDrawingColorToMediaColor(EnvironmentColors.ToolWindowBackgroundColorKey);
		public static readonly Brush TextColor = ConvertDrawingColorToMediaColor(EnvironmentColors.EditorExpansionTextColorKey);
		public static readonly Brush BorderColor = ConvertDrawingColorToMediaColor(EnvironmentColors.AccentBorderColorKey);

		private static Brush ConvertDrawingColorToMediaColor(ThemeResourceKey themeKey)
		{
			var drawingColor = VSColorTheme.GetThemedColor(themeKey);
			var color = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
			return new SolidColorBrush(color);
		}
	}
}
