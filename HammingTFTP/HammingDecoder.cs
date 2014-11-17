using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammingTFTP
{
    /// <summary>
    /// Decode packets encoded with 32bit Hamming check algorithm
    /// </summary>
    class HammingDecoder
    {
        public ErrorCheckMd em = ErrorCheckMd.noerror;

        int blockcount = 0;
        /// <summary>
        /// Decodes the data payload of a single packet.
        /// </summary>
        /// <param name="datapayload">The data payload off the wire</param>
        /// <returns>The decoded data contents</returns>
        public byte[] DecodePacket(byte[] datapayload)
        {
            // Holding variable for carried bits
            bool[] leftoverbits = null;
            List<byte> ret = new List<byte>();

            // Loop through each block of 32 bits
            for(int i = 0; i < datapayload.Length; i+=4)
            {
                // Brake the bytes off and reverse byte order
                byte[] block = new byte[4];
                Array.Copy(datapayload, i, block, 0, 4);

                // Convert bytes into bitarray then to bool array
                BitArray handler = new BitArray(block);
                bool[] data = new bool[handler.Length];
                handler.CopyTo(data, 0);

                // Strip out check bits
                bool[] results = null;
                try
                {
                    results = this.HandleCheckSumBits(data);
                }
                catch(Exception)
                {
                    // Exception here means invalid data packet.
                    // Send a NAK
                    return null;
                }

                // Build big bit array
                List<bool> databits = new List<bool>();

                // Copy any previous leftovers
                if(leftoverbits != null)
                {
                    foreach(bool bit in leftoverbits)
                    {
                        databits.Add(bit);
                    }
                    leftoverbits = null;
                }

                // Copy the databits
                foreach (bool bit in results) { databits.Add(bit); }

                List<bool> bytecreate = new List<bool>();
                for(int x = 0; x < databits.Count; x++)
                {
                    // Copy bit to bytecreate
                    bytecreate.Add(databits[x]);

                    // If count is 8, create byte
                    if(bytecreate.Count == 8)
                    {
                        bool[] flip = bytecreate.ToArray();
                        //Array.Reverse(flip);
                        BitArray tobyte = new BitArray(flip);
                        byte[] target = new byte[1];
                        tobyte.CopyTo(target, 0);

                        // Add the new byte to output
                        ret.Add(target[0]);

                        // Clear the bytecreate
                        bytecreate.Clear();
                    }
                }

                // Copy the remaining bytecreate to leftovers
                leftoverbits = bytecreate.ToArray();
            }

            // Return decoded bytes
            return ret.ToArray();
        }

        
        /// <summary>
        /// Handles the checksum bits for the 32bit message block.
        /// </summary>
        /// <param name="bits">The input bits</param>
        /// <returns>The bits minus the check bits</returns>
        private bool[] HandleCheckSumBits(bool[] before)
        {
            // Remove check bits from data into seperate array
            bool[] after = new bool[26];
            bool[] check = new bool[6];

            // Remove 1, 2, 4, 8, 16, and 32
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

            // Calculate parity bits
            bool[] parity = this.CalculateParityBits(before);

            // This code has some odd inconsistancies, don't call in noerror
            // mode
            if (this.em != ErrorCheckMd.noerror)
            {
                // First check for a 32bit error, this is uncorrectable and
                // needs to result in an exception and NAK packet
                if (parity[5] != true) { throw new Exception(); }

                // Detect the errored bit and repair
                int ebit = 0;

                if (parity[0] != true) { ebit += 1; }
                if (parity[1] != true) { ebit += 2; }
                if (parity[2] != true) { ebit += 4; }
                if (parity[3] != true) { ebit += 8; }
                if (parity[4] != true) { ebit += 16; }

                // Flip the bad bit
                if (ebit != 0)
                {
                    if (before[ebit] == true) { before[ebit] = false; } else { before[ebit] = true; }
                }
            }

            // Remove 1, 2, 4, 8, 16, and 32
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

            // Flip bits back again and return
            Array.Reverse(after);
            blockcount++;
            return (after);
        }

        /// <summary>
        /// Takes the 32bit data block and calculate the expected even parity values.
        /// </summary>
        /// <param name="data">The data block</param>
        /// <returns>The calculated parity bits</returns>
        private bool[] CalculateParityBits(bool[] data)
        {
            // Function persistant variables
            bool[] parity = new bool[6];
            bool flipflag = true;
            int onescount = 0;
            int count = 0;

            // Loop through every other bit between 0 and 30 and count the number of ones
            for (int x = 0; x < (data.Length - 1); x += 2)
            {
                if (data[x] == true) { onescount++; }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[0] = true;
            }
            else { parity[0] = false; }

            // Loop through every two bits between 1 and 30 and count the number of ones
            onescount = 0;
            count = 0;
            flipflag = true;
            for (int x = 1; x < (data.Length - 1); x++)
            {
                if (flipflag == true)
                {
                    if (data[x] == true) { onescount++; }
                }

                count++;

                // Flip if counted off by two
                if (count == 2)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[1] = true;
            }
            else { parity[1] = false; }

            // Loop through every four bits between 3 and 30 and count the number of ones
            onescount = 0;
            count = 0;
            flipflag = true;

            for (int x = 3; x < (data.Length - 1); x++)
            {
                if (flipflag == true)
                {
                    if (data[x] == true) { onescount++; }
                }

                count++;

                // Flip if counted off by two
                if (count == 4)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[2] = true;
            }
            else { parity[2] = false; }

            // Loop through every 8 bits between 7 and 30 and count the number of ones
            onescount = 0;
            count = 0;
            flipflag = true;
            for (int x = 7; x < (data.Length - 1); x++)
            {
                if (flipflag == true)
                {
                    if (data[x] == true) { onescount++; }
                }

                count++;

                // Flip if counted off by two
                if (count == 8)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[3] = true;
            }
            else { parity[3] = false; }

            // Loop through every 16 bits between 15 and 30 and count the number of ones
            onescount = 0;
            count = 0;
            flipflag = true;
            for (int x = 15; x < (data.Length - 1); x++)
            {
                if (flipflag == true)
                {
                    if (data[x] == true) { onescount++; }
                }

                count++;

                // Flip if counted off by two
                if (count == 16)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[4] = true;
            }
            else { parity[4] = false; }

            // Check every bit in the block for the last one
            onescount = 0;
            for (int x = 0; x < data.Length; x++)
            {
                if(data[x] == true)
                {
                    onescount++;
                }
            }

            // If there are an even number of ones, there are no errors
            if ((onescount % 2) == 0)
            {
                parity[5] = true;
            }
            else { parity[5] = false; }

            // Return calculated bits
            return parity;
        }

        /// <summary>
        /// Convert bit array to string representation for debugging purposes
        /// </summary>
        /// <param name="bits">the bits</param>
        /// <returns>the string</returns>
        private string BitsToStringDebug(bool[] bits)
        {
            string ret = "";

            foreach(bool bit in bits)
            {
                if (bit == true) { ret = ret + "1 "; } else { ret = ret + "0 "; }
            }

            return ret;
        }
    }
}
