﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asv.Gnss
{
    public class RtcmV3HeaderBase : ISerializable
    {
        /// <summary>
        /// message no
        /// </summary>
        public ushort MessageNumber { get; set; }

        /// <summary>
        /// Reference station id 
        /// </summary>
        public ushort ReferenceStationId { get; set; }


        public virtual void Serialize(byte[] buffer, uint startIndex = 0)
        {
            uint i = 24 + startIndex;

            BitOperationHelper.SetBitU(buffer, i, 12, MessageNumber);
            i += 12; /* message no */
            BitOperationHelper.SetBitU(buffer, i, 12, ReferenceStationId);
            /* ref station id */
        }

        public virtual void Deserialize(byte[] buffer, uint startIndex = 0)
        {
            uint i = 24 + startIndex;

            MessageNumber = (ushort)BitOperationHelper.GetBitU(buffer, i, 12);
            i += 12; /* message no */
            ReferenceStationId = (ushort)BitOperationHelper.GetBitU(buffer, i, 12);
            /* ref station id */
        }
    }

    public class RtcmV3ObservableHeader: RtcmV3HeaderBase
    {
        /// <summary>
        /// GPS/GLONASS epoch time
        /// </summary>
        public uint Epoch { get; set; }

        /// <summary>
        /// Number of GPS/GLONASS Satellite Signals Processed
        /// </summary>
        public byte NumberOfSat { get; set; }

        /// <summary>
        /// GPS/GLONASS Divergence-free Smoothing Indicator
        /// </summary>
        public byte SmoothIndicator { get; set; }
        /// <summary>
        /// GPS/GLONASS Smoothing interval
        /// </summary>
        public byte SmoothInterval { get; set; }
        /// <summary>
        /// Synchronous GNSS Flag
        /// </summary>
        public byte Sync { get; set; }

        public override void Deserialize(byte[] buffer, uint startIndex = 0)
        {
            base.Deserialize(buffer, startIndex);

<<<<<<< HEAD:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3Header.cs
            MessageNumber = (ushort)RtcmV3Helper.GetBitU(buffer, i, 12);
            i += 12; /* message no */
            ReferenceStationId = (ushort)RtcmV3Helper.GetBitU(buffer, i, 12);
            i += 12; /* ref station id */
            if (MessageNumber < 1009 || MessageNumber > 1012)
=======
            uint i = 48 + startIndex;
            
            if (MessageNumber <= 1004 && MessageNumber >= 1001)
>>>>>>> 48a1caed8db56616bff80b720ea473a619c7bf76:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3ObservableHeader.cs
            {
                Epoch = RtcmV3Helper.GetBitU(buffer, i, 30);
                i += 30; /* gps epoch time */
            }
            else
            {
<<<<<<< HEAD:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3Header.cs
                Epoch = RtcmV3Helper.GetBitU(buffer, i, 27);
                i += 27; /* glonass epoch time */
            }
            Sync = (byte)RtcmV3Helper.GetBitU(buffer, i, 1);
=======
                if (MessageNumber <= 1012 && MessageNumber >= 1009)
                {
                    Epoch = BitOperationHelper.GetBitU(buffer, i, 27);
                    i += 27; /* glonass epoch time */
                }
                else
                {
                    throw new Exception($"Message {MessageNumber} is not observable message");
                }
                
            }

            Sync = (byte)BitOperationHelper.GetBitU(buffer, i, 1);
>>>>>>> 48a1caed8db56616bff80b720ea473a619c7bf76:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3ObservableHeader.cs
            i += 1; /* synchronous gnss flag */
            NumberOfSat = (byte)RtcmV3Helper.GetBitU(buffer, i, 5);
            i += 5; /* no of satellites */
            SmoothIndicator = (byte)RtcmV3Helper.GetBitU(buffer, i, 1);
            i += 1; /* smoothing indicator */
            SmoothInterval = (byte)RtcmV3Helper.GetBitU(buffer, i, 3);
            i += 3; /* smoothing interval */
        }

        public override void Serialize(byte[] buffer, uint startIndex = 0)
        {
            base.Serialize(buffer, startIndex);

<<<<<<< HEAD:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3Header.cs
            RtcmV3Helper.SetBitU(buffer, i, 12, MessageNumber);
            i += 12; /* message no */
            RtcmV3Helper.SetBitU(buffer, i, 12, ReferenceStationId);
            i += 12; /* ref station id */
            RtcmV3Helper.SetBitU(buffer, i, 30, Epoch);
            i += 30; /* gps epoch time */
            RtcmV3Helper.SetBitU(buffer, i, 1, Sync);
=======
            uint i = 48 + startIndex;

            if (MessageNumber <= 1004 && MessageNumber >= 1001)
            {
                BitOperationHelper.SetBitU(buffer, i, 30, Epoch);
                i += 30; /* gps epoch time */
            }

            if (MessageNumber <= 1012 && MessageNumber >= 1009)
            {
                BitOperationHelper.SetBitU(buffer, i, 27, Epoch);
                i += 27; /* glonass epoch time */
            }

            
            BitOperationHelper.SetBitU(buffer, i, 1, Sync);
>>>>>>> 48a1caed8db56616bff80b720ea473a619c7bf76:src/Asv.Gnss/Parsers/RTCMv3/RtcmV3ObservableHeader.cs
            i += 1; /* synchronous gnss flag */
            RtcmV3Helper.SetBitU(buffer, i, 5, NumberOfSat);
            i += 5; /* no of satellites */
            RtcmV3Helper.SetBitU(buffer, i, 1, SmoothIndicator);
            i += 1; /* smoothing indicator */
            RtcmV3Helper.SetBitU(buffer, i, 3, SmoothInterval);
            i += 3; /* smoothing interval */
        }
    }
}