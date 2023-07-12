namespace WpfPilot.Utility;

using System;

internal static class LevenshteinDistance
{
	// Modified from https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560#file-levenshteindistance-cs-L5
	public static int Calculate(string a, string b)
	{
		var matrix = new int[a.Length + 1, b.Length + 1];
		for (var i = 0; i <= a.Length; matrix[i, 0] = i++)
		{
		}

		for (var j = 0; j <= b.Length; matrix[0, j] = j++)
		{
		}

		for (var i = 1; i <= a.Length; i++)
		{
			for (var j = 1; j <= b.Length; j++)
			{
				var cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
				matrix[i, j] = Math.Min(
					Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
					matrix[i - 1, j - 1] + cost);
			}
		}

		return matrix[a.Length, b.Length];
	}
}