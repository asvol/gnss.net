﻿namespace Asv.Gnss
{
    /// <summary>
    /// This block contains the 250 bits of a SBAS L1 navigation frame, after Viterbi decoding
    ///
    /// NAVBits contains the 250 bits of a SBAS navigation frame.
    /// Encoding: NAVBits contains all the bits of the frame, including the
    /// preamble. The first received bit is stored as the MSB of NAVBits[0].
    /// The unused bits in NAVBits[7] must be ignored by the decoding
    /// software.
    /// </summary>
    public class SbfPacketGeoRawL1 : SbfPacketGnssRawNavMsgBase
    {
        public override ushort MessageId => 4020;

        protected override int NavBitsU32Length => 8;

    }
}