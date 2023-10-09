using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvergePro2DspPlugin
{
	public static class Extensions
	{
		public static IEnumerable<string> TokenizeParams(this string s, char? separator)
		{
			if (separator == null) separator = ' ';

			var inQuotes = false;
			return s.Split(c =>
			{
				if (c == '\"')
					inQuotes = !inQuotes;
				return !inQuotes && c == separator;
			}).Select(t => t.Trim());
		}

		public static IEnumerable<string> Split(this string s, Func<char, bool> controller)
		{
			var n = 0;
			for (var c = 0; c < s.Length; c++)
			{
				if (!controller(s[c])) continue;
				yield return s.Substring(n, c - n);
				n = c + 1;
			}
			yield return s.Substring(n);
		}

		public static string EmptyIfNull(this string s)
		{
			return s ?? string.Empty;
		}

		public static string DefaultIfNull(this string s, string value)
		{
			return s ?? value;
		}

		public static string Next(this IEnumerator<string> enumerator)
		{
			return enumerator.MoveNext() ? enumerator.Current.EmptyIfNull() : string.Empty;
		}

		public static bool NextEquals(this IEnumerator<string> enumerator, string other, StringComparison comparison)
		{
			return enumerator.Next().Equals(other, comparison);
		}

		public static bool NextContains(this IEnumerator<string> enumerator, string other)
		{
			return enumerator.Next().Contains(other);
		}
	}
}