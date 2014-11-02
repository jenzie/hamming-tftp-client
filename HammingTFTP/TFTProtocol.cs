/*
 * Hamming TFTP Client
 * author Jenny Zhen
 * date: 11.02.14
 * language: C#
 * file: TFTProtocol.cs
 * assignment: HammingTFTP
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace HammingTFTP
{
	/// <summary>
	/// Implements the TFTP protocol at the network level.
	/// </summary>
	public class TFTProtocol
	{
		// Connection session variables.
		private IPEndPoint tftphost;
		private Socket sconnection;
		private EndPoint reccon;

		/// <summary>
		/// Validate and set the server details for the connection.
		/// </summary>
		/// <param name="host">DNS name or IP of the host to contact.</param>
		/// <param name="socket">The UDP socket to connect on.</param>
		public TFTProtocol(string host, int socket)
		{
			// Check if the host is an IP address.
			IPAddress ipCheck;
			if (IPAddress.TryParse(host, out ipCheck) == false)
			{
				// String is not a valid IP address.
				// Check to see if it is a valid hostname.
				IPAddress[] lookup = System.Net.Dns.GetHostAddresses(host);

				if (lookup.Length > 1)
				{
					// Host is not valid IP, or hostname.
					throw new Exception(
						"Fatal Error: " + host +
						" is not a valid hostname or IP address.");
				}

				// Lookup was successful, use first address.
				ipCheck = lookup[0];
			}

			// Create IP endpoint based on information.
			this.tftphost = new IPEndPoint(ipCheck, socket);

			// All set for connection attempt.
			return;
		}


		/// <summary>
		/// Downloads the requested file from the server and stores it locally.
		/// </summary>
		/// <param name="hostfilename">The file on the remote server.</param>
		/// <param name="localfilename">The local file location to save to.
		/// </param>
		/// <param name="mode">Transfer in binary or ascii.</param>
		/// <returns>True on success, false on error.</returns>
		public bool GetFileFromServer(
			string hostfilename, string localfilename, TransferMode mode)
		{
			// Create a new socket and connect to the server.
			this.sconnection = new Socket(
				AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			// Open the local file for writing.
			FileStream lfile = File.Open(localfilename, FileMode.Create);

			// Create a request packet and send it.
			byte[] packet = this.GenerateRequestReadPacket(hostfilename, mode);
			this.sconnection.SendTo(packet, this.tftphost);

			// Go into loop listening for response packets.
			while (true)
			{
				// Listen for the response packet.
				byte[] raw = new byte[1000];
				this.reccon = (EndPoint)this.tftphost;
				int psize = this.sconnection.ReceiveFrom(raw, ref reccon);
				packet = new byte[psize];
				Array.Copy(raw, 0, packet, 0, psize);

				// Read the packet type.
				byte[] rtype = new byte[2];
				Array.Copy(packet, 0, rtype, 0, 2);

				// Handle endianness conversion issue.
				if (BitConverter.IsLittleEndian)
					Array.Reverse(rtype);

				// Handle the packet type.
				if (BitConverter.ToInt16(rtype, 0) == 3)
				{
					// This is a DATA packet.
					int response = this.SaveDataPacket(packet, lfile, mode);

					// If response is -1, transmission is done.
					if (response == -1)
						break;
				}
				else if (BitConverter.ToInt16(rtype, 0) == 5)
				{
					// This is an ERROR packet.
					// Close and delete the file and connection.
					// Send the error code to the error handler.
					lfile.Close();
					File.Delete(localfilename);
					this.sconnection.Close();

					this.HandleError(packet);
				}
				else
				{
					// Unknown bad packet.
					lfile.Close();
					File.Delete(localfilename);
					this.sconnection.Close();
					throw new Exception(
						"Fatal Error: Packet with unknown type " +
						BitConverter.ToInt16(rtype, 0).ToString() +
						" recieved.");
				}
			}

			// Finished recieving packets.
			// Close socket, close file and return success.
			lfile.Flush();
			lfile.Close();
			this.sconnection.Close();

			return true;
		}



		/// <summary>
		/// Does the low level generation of the request packet.
		/// </summary>
		/// <param name="filename">The filename to request from the server.
		/// </param>
		/// <param name="mode">The transfer mode to use.</param>
		/// <returns>The packet in byte form.</returns>
		private byte[] GenerateRequestReadPacket(string filename, TransferMode mode)
		{
			// Convert the filename to bytes.
			byte[] fname = System.Text.Encoding.ASCII.GetBytes(filename);

			// Get the length of the additional bytes.
			byte[] moderep;
			if (mode == TransferMode.netascii)
				moderep = Encoding.ASCII.GetBytes("netascii");
			else
				moderep = Encoding.ASCII.GetBytes("octet");

			// Create packet data buffer.
			// Length of filename, length of mode field, plus 4 static bytes.
			byte[] packet = new byte[fname.Length + moderep.Length + 4];

			// Since client always implements read, first 2 bytes will always 
			// be 01 00 (the OPCode of 1 in big endian network format).
			packet[0] = (byte)0;
			packet[1] = (byte)1;

			// Copy bytes of the filename to packet.
			int pos = 2;
			for (int i = 0; i < filename.Length; i++, pos++)
				packet[pos] = fname[i];

			// Add null delimiter into packet.
			packet[pos] = (byte)0;
			pos++;

			// Copy the mode string into the packet.
			for (int i = 0; i < moderep.Length; i++, pos++)
				packet[pos] = moderep[i];

			// Add the final null and return the packet for transmission.
			packet[pos] = (byte)0;
			return packet;
		}

		/// <summary>
		/// Takes the recieved data packet and writes it to the local file. 
		/// Then sends the proper ACK packet.
		/// </summary>
		/// <param name="dpacket">The recieved packet.</param>
		/// <param name="lfile">The file stream for writing.</param>
		/// <param name="mode">Binary or ASCII file, changes how file 
		/// write is handled.</param>
		/// <returns>Block number or -1 for end of transmission.</returns>
		private int SaveDataPacket(
			byte[] dpacket, FileStream lfile, TransferMode mode)
		{
			// We already know it is a data packet, so skip the opcode 
			// field and grab the block number.
			byte[] bnum = new byte[2];
			Array.Copy(dpacket, 2, bnum, 0, 2);

			// Convert this to an int, deal with endianness issue.
			if (BitConverter.IsLittleEndian) { Array.Reverse(bnum); }
			int block = BitConverter.ToInt16(bnum, 0);

			// If the program is in ascii mode, 
			// zero out the lf+cr in the last two bytes.
			if (mode == TransferMode.netascii)
			{
				dpacket[dpacket.Length - 1] = (byte)0;
				dpacket[dpacket.Length - 2] = (byte)0;
			}

			// Write the bytes to the file.
			lfile.Write(dpacket, 4, (dpacket.Length - 4));

			// Make a 4 byte response packet, 
			// with the first two bites being 0 and 5.
			byte[] ack = new byte[4];
			ack[0] = (byte)0;
			ack[1] = (byte)4;

			// Flip the block number back and append.
			if (BitConverter.IsLittleEndian) { Array.Reverse(bnum); }
			Array.Copy(bnum, 0, ack, 2, 2);

			// Send the ACK packet.
			this.sconnection.SendTo(ack, this.reccon);

			// Check to see if array size if 516, the max 512 payload plus the 
			// 4 code bytes. If the packet is shorter then this, 
			// it is the last one.
			if (dpacket.Length < 516)
				block = -1;

			// Return the block code.
			return block;
		}

		/// <summary>
		/// Formats error message, and throws exception for main program 
		/// body to catch.
		/// </summary>
		/// <param name="dpacket">The error packet.</param>
		private void HandleError(byte[] dpacket)
		{
			// Grab the error number from the second two bytes.
			// Deal with endianness issue.
			byte[] ernum = new byte[2];
			Array.Copy(dpacket, 2, ernum, 0, 2);
			if (BitConverter.IsLittleEndian) { Array.Reverse(ernum); }
			int errcode = BitConverter.ToInt16(ernum, 0);

			// Copy the rest of the error to a string.
			string errormsg = Encoding.ASCII.GetString(
				dpacket, 4, (dpacket.Length - 4));

			// Throw the exception.
			throw new Exception(
				"TFTPReader: Error Code " +
				errcode.ToString() + ": " + errormsg);
		}
	}

	/// <summary>
	/// Represents the two possible transfer modes, binary and ascii.
	/// </summary>
	public enum TransferMode { netascii, octet }
}
