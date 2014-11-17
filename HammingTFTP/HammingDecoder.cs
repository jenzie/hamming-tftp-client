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
                bool[] results = this.HandleCheckSumBits(data);

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

            // First check for a 32bit error, this is uncorrectable and
            // needs to result in an exception and NAK packet
            if (parity[5] != check[5]) { throw new Exception(); }

            // Detect the errored bit and repair
            int ebit = 0;

            if (parity[0] != check[0]) { ebit += 1; }
            if (parity[1] != check[1]) { ebit += 2; }
            if (parity[2] != check[2]) { ebit += 4; }
            if (parity[3] != check[3]) { ebit += 8; }
            if (parity[4] != check[4]) { ebit += 16; }

            // Flip the bad bit
            if(ebit != 0)
            {
                if (before[ebit] == true) { before[ebit] = false; } else { before[ebit] = true; }
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
            // Parity bit array
            bool[] parity = new bool[6];
            int numones = 0;
            int count = 0;
            bool flipflag = true;

            // Calculate first parity bit by checking every other bit
            for(int x = 0; x <= 31; x++)
            {
                if(flipflag == true)
                {
                    if (x != 0 && x != 1 && x != 3 && x != 7 && x != 15)
                    {
                        if (data[x] == true) { numones++; }
                    }
                    flipflag = false;
                }
                else
                {
                    flipflag = true;
                }
            }
            
            if(numones % 2 != 0)
            {
                parity[0] = true;
            }
            else
            {
                parity[0] = false;
            }

            // Calculate the second parity bit, take two skip two
            flipflag = true;
            numones = 0;
            for (int x = 1; x <= 31; x++ )
            {
                if(flipflag == true)
                {
                    if (x != 0 && x != 1 && x != 3 && x != 7 && x != 15)
                    {
                        if (data[x] == true) { numones++; }
                    }
                }

                count++;

                // If count is 2, flip
                if (count == 2) {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            if (numones % 2 != 0)
            {
                parity[1] = true;
            }
            else
            {
                parity[1] = false;
            }

            // Calculate the fourth parity bit, take four skip four
            flipflag = true;
            numones = 0;
            count = 0;
            for (int x = 3; x <= 31; x++)
            {
                if (flipflag == true)
                {
                    if (x != 0 && x != 1 && x != 3 && x != 7 && x != 15)
                    {
                        if (data[x] == true) { numones++; }
                    }
                }

                count++;

                // If count is 2, flip
                if (count == 4)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            if (numones % 2 != 0)
            {
                parity[2] = true;
            }
            else
            {
                parity[2] = false;
            }

            // Calculate the eighth parity bit, take eight skip eight
            flipflag = true;
            numones = 0;
            count = 0;
            for (int x = 7; x <= 31; x++)
            {
                if (flipflag == true)
                {
                    if (x != 0 && x != 1 && x != 3 && x != 7 && x != 15)
                    {
                        if (data[x] == true) { numones++; }
                    }
                }

                count++;

                // If count is 8, flip
                if (count == 8)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            if (numones % 2 != 0)
            {
                parity[3] = true;
            }
            else
            {
                parity[3] = false;
            }

            // Calculate the sixteenth parity bit, take 16 skip 16
            flipflag = true;
            numones = 0;
            count = 0;
            for (int x = 15; x <= 31; x++)
            {
                if (flipflag == true)
                {
                    if (x != 0 && x != 1 && x != 3 && x != 7 && x != 15)
                    {
                        if (data[x] == true) { numones++; }
                    }
                }

                count++;

                // If count is 8, flip
                if (count == 16)
                {
                    if (flipflag == true) { flipflag = false; } else { flipflag = true; }
                    count = 0;
                }
            }

            if (numones % 2 != 0)
            {
                parity[4] = true;
            }
            else
            {
                parity[4] = false;
            }

            // Calculate last bit which checks all bits
            numones = 0;
            for (int x = 0; x < 31; x++)
            {
                if(data[x] == true)
                {
                    numones++;
                }
            }

            if (numones % 2 != 0)
            {
                parity[5] = true;
            }
            else
            {
                parity[5] = false;
            }

            // Return calculated bits
            return parity;
        }
    }
}
