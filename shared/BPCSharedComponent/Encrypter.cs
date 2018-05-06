/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
namespace BPCSharedComponent.Security
{
	/// <summary>
	/// Encrypts and decrypts sounds.
	/// </summary>
	public class Encrypter
	{
		private static byte key = 129;

		public static byte[] getData(string i, string el)
		{
			if (!el.Equals("TDV123"))
			{
				throw new ArgumentException("Security error in encryption procedure. " +
								"The file could not be processed.");
			}
			try
			{
				FileStream infile = new FileStream(i, FileMode.Open, FileAccess.Read, FileShare.Read);
				byte[] infileBytes = new byte[infile.Length];
				infile.Read(infileBytes, 0, (int)infile.Length);
				infile.Close();
				infile.Dispose();
				infile = null;
				encryptDecrypt(infileBytes);
				return infileBytes;
			}
			catch (Exception e)
			{
				StreamWriter theFile = File.CreateText("error_bpcsharedcom.log");

				theFile.Write("Error log, created with BPCSharedComponent.dll");
				theFile.Write("Error base exception: {0}{1}Error Description: {2}{3}Stack trace: {4}", e.GetBaseException(), Environment.NewLine, e.Message, Environment.NewLine, e.StackTrace);
				theFile.Flush();
				theFile.Close();
				return null;
			}
		}

		private static void encryptDecrypt(byte[] data)
		{
			byte c = 0;
			int i = 0;
			for (i = 0; i <= data.Length - 1; i++)
			{
				c = data[i];
				c = (byte)(c ^ key);
				data[i] = c;
			}
		}
	}
}
