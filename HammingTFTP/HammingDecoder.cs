using System;
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
            return datapayload;
        }
    }
}
