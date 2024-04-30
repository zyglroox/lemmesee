using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace LemmeSee.RefactoringFlow
{
	internal static class RefactoringsRepository
	{
		private static readonly Dictionary<TextSpan, string> Responses = new Dictionary<TextSpan, string>();

		public static void Save(TextSpan span, string response) => Responses.Add(span, response);

		public static bool TryGet(TextSpan span, out string response)
		{
			var result = Responses.TryGetValue(span, out response);
			if (result)
				Responses.Remove(span);
			return result;
		}
	}
}
