/*
 * Hamming TFTP Client
 * author Jenny Zhen
 * date: 11.02.14
 * language: C#
 * file: Program.cs
 * assignment: HammingTFTP
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammingTFTP
{
	/// <summary>
	/// RFC1350 compliant TFTP client. 
	/// </summary>
	class TFTPreader
	{
		public const string NETASCII = "netascii";
		public const string OCTET = "octet";

		/// <summary>
		/// Main parses the command line arguments, and starts a new TFTP 
		/// session to download a file.
		/// </summary>
		static void Main(string[] args)
		{
			TransferMode transferMode = TransferMode.octet;
			string server = null, file = null;

			if (args.Length == 3)
			{
				// Check the file transfer mode; netascii or octet.
				if (args[0].Trim().ToLower().Equals(NETASCII))
					transferMode = TransferMode.netascii;
				else if (args[0].Trim().ToLower().Equals(OCTET))
					transferMode = TransferMode.octet;
				else
					Console.Error.WriteLine(
						"Usage: [mono] TFTPreader [netascii | octet] "
						+ "tftp-host file");

				// Save the arguments.
				server = args[1];
				file = args[2];

				// Try to execute the operation.
				try
				{
					TFTProtocol session = new TFTProtocol(server, 69);
					session.GetFileFromServer(file, file, transferMode);
				}
				catch (Exception e)
				{
					// Print exception message and exit.
					Console.WriteLine(e.Message);
					return;
				}
			}
			else
			{
				Console.Error.WriteLine(
					"Usage: [mono] TFTPreader [netascii | octet] "
					+ "tftp-host file");
			}
		}
	}
}
