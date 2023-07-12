namespace WpfPilot.Tests.TestUtility;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public static class AssertImage
{
	// https://stackoverflow.com/a/25509503
	public static void IsValid(byte[] bytes)
	{
		var stream = new MemoryStream(bytes);
		stream.Seek(0, SeekOrigin.Begin);

		List<string> jpg = new List<string> { "FF", "D8" };
		List<string> bmp = new List<string> { "42", "4D" };
		List<string> gif = new List<string> { "47", "49", "46" };
		List<string> png = new List<string> { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };
		List<List<string>> imgTypes = new List<List<string>> { jpg, bmp, gif, png };

		List<string> bytesIterated = new List<string>();

		for (int i = 0; i < 8; i++)
		{
			string bit = stream.ReadByte().ToString("X2");
			bytesIterated.Add(bit);

			bool isImage = imgTypes.Any(img => !img.Except(bytesIterated).Any());
			if (isImage)
				return;
		}

		Assert.Fail("File is not a JPG, BMP, GIF, or PNG.");
	}

	public static void ExistsAndCleanup(string path)
	{
		var evalPath = Environment.ExpandEnvironmentVariables(path);
		evalPath = Path.GetFullPath(evalPath);
		Assert.True(File.Exists(evalPath), $"File '{evalPath}' does not exist.");
		File.Delete(evalPath);
	}
}
