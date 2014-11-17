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
            // Currently just strips out the check bits
            // will fix one bit errors soon
            bool[] after = new bool[26];

            // Remove 1, 2, 4, 8, 16, and 32
            int pos = 0;
            for(int z = 0; z < 32; z++)
            {
                if(z != 0 && z != 1 && z != 3 && z != 7 && z != 15 && z != 31)
                {
                    after[pos] = before[z];
                    pos++;
                }
            }

            // Flip bits back again and return
            Array.Reverse(after);
            return (after);
        }
    }
}
