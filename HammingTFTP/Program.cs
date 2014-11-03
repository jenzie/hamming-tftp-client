/*
 * Hamming TFTP Client
 * author Jenny Zhen
 * date: 11.02.14
 * language: C#
 * file: Program.cs
 * assignment: HammingTFTP
 */

using System;
using System.Text;

namespace HammingTFTP
{
	/// <summary>
	/// RFC1350 compliant TFTP client. 
	/// </summary>
	class TFTPreader
	{
		public const string ERROR = "error";
		public const string NOERR = "noerror";
        public const int altport = 7000;

		/// <summary>
		/// Main parses the command line arguments, and starts a new TFTP 
		/// session to download a file.
		/// </summary>
		static void Main(string[] args)
		{
            ErrorCheckMd errmode = ErrorCheckMd.noerror;
			string server = null, file = null;

			if (args.Length == 3)
			{
				// Check the file transfer mode; netascii or octet.
                if (args[0].Trim().ToLower().Equals(ERROR))
                    errmode = ErrorCheckMd.error;
                else if (args[0].Trim().ToLower().Equals(NOERR))
                    errmode = ErrorCheckMd.noerror;
                else
                    Console.Error.WriteLine(
                        "Usage: [mono] TFTPreader [ error | noerror ] "
                        + "tftp-host file");

				// Save the arguments.
				server = args[1];
				file = args[2];

				// Try to execute the operation.
				try
				{
					TFTProtocol session = new TFTProtocol(server, altport);
					session.GetFileFromServer(file, file, errmode);
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
