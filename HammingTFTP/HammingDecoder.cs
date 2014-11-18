/*
 * Hamming TFTP Client
 * author Jenny Zhen
 * date: 11.02.14
 * language: C#
 * file: HammingDecoder.cs
 * assignment: HammingTFTP
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammingTFTP
{
    /// <summary>
    /// Decode packets encoded with 32-bit hamming check algorithm.
    /// </summary>
    class HammingDecoder
    {
        public ErrorCheckMd em = ErrorCheckMd.noerror;

        int blockcount = 0;
        /// <summary>
        /// Decodes the data payload of a single packet.
        /// </summary>
        /// <param name="datapayload">The data payload off the wire.</param>
        /// <returns>The decoded data contents.</returns>
        public byte[] DecodePacket(byte[] datapayload)
        {
            // Holding variable for carried bits.
            bool[] leftoverbits = null;
            List<byte> ret = new List<byte>();

            // Loop through each block of 32 bits.
            for(int i = 0; i < datapayload.Length; i+=4)
            {
                // Break the bytes off and reverse byte order.
                byte[] block = new byte[4];
                Array.Copy(datapayload, i, block, 0, 4);

                // Convert bytes into bit array then to boolean array.
                BitArray handler = new BitArray(block);
                bool[] data = new bool[handler.Length];
                handler.CopyTo(data, 0);

                // Strip out check bits.
                bool[] results = null;
                try
                {
                    results = this.HandleCheckSumBits(data);
                }
                catch(Exception)
                {
                    // Exception here means invalid data packet.
                    // Send a NAK.
                    return null;
                }

                // Build big bit array.
                List<bool> databits = new List<bool>();

                // Copy any previous leftovers.
                if(leftoverbits != null)
                {
                    foreach(bool bit in leftoverbits)
                    {
                        databits.Add(bit);
                    }
                    leftoverbits = null;
                }

                // Copy the databits.
                foreach (bool bit in results) { databits.Add(bit); }

                List<bool> bytecreate = new List<bool>();
                for(int x = 0; x < databits.Count; x++)
                {
                    // Copy bit to bytecreate.
                    bytecreate.Add(databits[x]);

                    // If count is 8, create byte.
                    if(bytecreate.Count == 8)
                    {
                        bool[] flip = bytecreate.ToArray();
                        //Array.Reverse(flip);
                        BitArray tobyte = new BitArray(flip);
                        byte[] target = new byte[1];
                        tobyte.CopyTo(target, 0);

                        // Add the new byte to output.
                        ret.Add(target[0]);

                        // Clear the bytecreate.
                        bytecreate.Clear();
                    }
                }

                // Copy the remaining bytecreate to leftovers.
                leftoverbits = bytecreate.ToArray();
            }

            // Return decoded bytes.
            return ret.ToArray();
        }

        
        /// <summary>
        /// Handles the checksum bits for the 32-bit message block.
        /// </summary>
        /// <param name="bits">The input bits.</param>
        /// <returns>The bits minus the check bits.</returns>
        private bool[] HandleCheckSumBits(bool[] before)
        {
            // Remove check bits from data into separate array.
            bool[] after = new bool[26];
            bool[] check = new bool[6];

            // Remove 1, 2, 4, 8, 16, and 32.
            int pos = 0;
            int cpos = 0;
            for (int z = 0; z < 32; z++)
            {
                if (z != 0 && z != 1 && z != 3 && z != 7 && z != 15 && z != 31)
                {
                    after[pos] = before[z];
                    pos++;
                }
                else
                {
                    check[cpos] = before[z];
                    cpos++;
                }
            }

            // Calculate parity bits.
            bool[] parity = this.CalculateParityBits(before);

            // This code has some odd inconsistancies, don't call in noerror
            // mode.
            //if (this.em == ErrorCheckMd.error)
            //{
                // First check for a 32-bit error, this is uncorrectable and
                // needs to result in an exception and NAK packet.
                //if (parity[5] != true) { throw new Exception(); }

			
			// Detect the errored bit and repair
            int ebit = 0;

            if (parity[0] != true) { ebit += 1; }
            if (parity[1] != true) { ebit += 2; }
            if (parity[2] != true) { ebit += 4; }
            if (parity[3] != true) { ebit += 8; }
            if (parity[4] != true) { ebit += 16; }

            // Flip the bad bit.
            if (ebit != 0)
            {
                if (before[(ebit - 1)] == true)
					before[(ebit - 1)] = false;
				else
					before[(ebit - 1)] = true;
            }

            // Check parity bits again to see if error is repaired.
            // Detect the errored bit and repair.
            parity = this.CalculateParityBits(before);
            ebit = 0;

            if (parity[0] != true) { ebit += 1; }
            if (parity[1] != true) { ebit += 2; }
            if (parity[2] != true) { ebit += 4; }
            if (parity[3] != true) { ebit += 8; }
            if (parity[4] != true) { ebit += 16; }

            // First check for a 32-bit error, this is not correctable and
            // needs to result in an exception and NAK packet.
            if (parity[5] != true) { throw new Exception(); }
            if (ebit != 0) { throw new Exception(); }

            // Remove 1, 2, 4, 8, 16, and 32.
            pos = 0;
            cpos = 0;
            for (int z = 0; z < 32; z++)
            {
                if (z != 0 && z != 1 && z != 3 && z != 7 && z != 15 && z != 31)
                {
                    after[pos] = before[z];
                    pos++;
                }
                else
                {
                    check[cpos] = before[z];
                    cpos++;
                }
            }

            // Flip bits back again and return.
            Array.Reverse(after);
            blockcount++;
            return (after);
        }

        /// <summary>
        /// Takes the 32-bit data block and calculates the expected 
		/// even parity values.
        /// </summary>
        /// <param name="data">The data block.</param>
        /// <returns>The calculated parity bits.</returns>
        private bool[] CalculateParityBits(bool[] data)
        {
            // Function persistant variables.
            bool[] parity = new bool[6];
            bool[] checkarray;
            int numones = 0;

            // Check the first bit, copy every other bit.
            checkarray = new bool[16];
            checkarray[0] = data[0];
            checkarray[1] = data[2];
            checkarray[2] = data[4];
            checkarray[3] = data[6];
            checkarray[4] = data[8];
            checkarray[5] = data[10];
            checkarray[6] = data[12];
            checkarray[7] = data[14];
            checkarray[8] = data[16];
            checkarray[9] = data[18];
            checkarray[10] = data[20];
            checkarray[11] = data[22];
            checkarray[12] = data[24];
            checkarray[13] = data[26];
            checkarray[14] = data[28];
            checkarray[15] = data[30];

            // Count up the number of ones and set check value.
            foreach (bool c in checkarray) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[0] = true;
			else
				parity[0] = false;

            // Check the second check bit.
            numones = 0;
            checkarray = new bool[16];
            checkarray[0] = data[1];
            checkarray[1] = data[2];
            checkarray[2] = data[5];
            checkarray[3] = data[6];
            checkarray[4] = data[9];
            checkarray[5] = data[10];
            checkarray[6] = data[13];
            checkarray[7] = data[14];
            checkarray[8] = data[17];
            checkarray[9] = data[18];
            checkarray[10] = data[21];
            checkarray[11] = data[22];
            checkarray[12] = data[25];
            checkarray[13] = data[26];
            checkarray[14] = data[29];
            checkarray[15] = data[30];

            // Count up the number of ones and set check value.
            foreach (bool c in checkarray) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[1] = true;
			else
				parity[1] = false;

            // Check the second check bit.
            numones = 0;
            checkarray = new bool[16];
            checkarray[0] = data[3];
            checkarray[1] = data[4];
            checkarray[2] = data[5];
            checkarray[3] = data[6];
            checkarray[4] = data[11];
            checkarray[5] = data[12];
            checkarray[6] = data[13];
            checkarray[7] = data[14];
            checkarray[8] = data[19];
            checkarray[9] = data[20];
            checkarray[10] = data[21];
            checkarray[11] = data[22];
            checkarray[12] = data[27];
            checkarray[13] = data[28];
            checkarray[14] = data[29];
            checkarray[15] = data[30];

            // Count up the number of ones and set check value.
            foreach (bool c in checkarray) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[2] = true;
			else
				parity[2] = false;

            // Check the second check bit.
            numones = 0;
            checkarray = new bool[16];
            checkarray[0] = data[7];
            checkarray[1] = data[8];
            checkarray[2] = data[9];
            checkarray[3] = data[10];
            checkarray[4] = data[11];
            checkarray[5] = data[12];
            checkarray[6] = data[13];
            checkarray[7] = data[14];
            checkarray[8] = data[23];
            checkarray[9] = data[24];
            checkarray[10] = data[25];
            checkarray[11] = data[26];
            checkarray[12] = data[27];
            checkarray[13] = data[28];
            checkarray[14] = data[29];
            checkarray[15] = data[30];

            // Count up the number of ones and set check value.
            foreach (bool c in checkarray) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[3] = true;
			else
				parity[3] = false;

            // Check the second check bit.
            numones = 0;
            checkarray = new bool[16];
            checkarray[0] = data[15];
            checkarray[1] = data[16];
            checkarray[2] = data[17];
            checkarray[3] = data[18];
            checkarray[4] = data[19];
            checkarray[5] = data[20];
            checkarray[6] = data[21];
            checkarray[7] = data[22];
            checkarray[8] = data[23];
            checkarray[9] = data[24];
            checkarray[10] = data[25];
            checkarray[11] = data[26];
            checkarray[12] = data[27];
            checkarray[13] = data[28];
            checkarray[14] = data[29];
            checkarray[15] = data[30];

            // Count up the number of ones and set check value.
            foreach (bool c in checkarray) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[4] = true;
			else
				parity[4] = false;

            // Do total check.
            numones = 0;
            foreach (bool c in data) { if (c == true) { numones++; } }
            if ((numones % 2) == 0)
				parity[5] = true;
			else
				parity[5] = false;

            // Return calculated bits.
            return parity;
        }

        /// <summary>
        /// Convert bit array to string representation for debugging purposes.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <returns>The string.</returns>
        private string BitsToStringDebug(bool[] bits)
        {
            string ret = "";

            foreach(bool bit in bits)
            {
                if (bit == true)
					ret = ret + "1 ";
				else
					ret = ret + "0 ";
            }

            return ret;
        }
    }
}
