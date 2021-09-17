﻿using System;

namespace Asv.Gnss
{
    public class EcefPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    
    public class RtcmV3Message1020 : RtcmV3MessageBase
    {
        public const int RtcmMessageId = 1020;

        public override uint Deserialize(byte[] buffer, uint offsetBits = 0)
        {
            var utc = DateTime.UtcNow;
            var bitIndex = offsetBits + base.Deserialize(buffer, offsetBits);

            var sys = NavigationSystemEnum.SYS_GLO;

            var prn = RtcmV3Helper.GetBitU(buffer, bitIndex, 6); bitIndex += 6;
            FrequencyNumber = (int)RtcmV3Helper.GetBitU(buffer, bitIndex, 5) - 7; bitIndex += 5 + 2 + 2;
            var tkH = RtcmV3Helper.GetBitU(buffer, bitIndex, 5); bitIndex += 5;
            var tkM = RtcmV3Helper.GetBitU(buffer, bitIndex, 6); bitIndex += 6;
            var tkS = RtcmV3Helper.GetBitU(buffer, bitIndex, 1) * 30.0; bitIndex += 1;
            var bn = RtcmV3Helper.GetBitU(buffer, bitIndex, 1); bitIndex += 1 + 1;
            var tb = RtcmV3Helper.GetBitU(buffer, bitIndex, 7); bitIndex += 7;
            
            Velocity.X = RtcmV3Helper.GetBitG(buffer, bitIndex, 24) * RtcmV3Helper.P2_20 * 1E3; bitIndex += 24;
            Position.X = RtcmV3Helper.GetBitG(buffer, bitIndex, 27) * RtcmV3Helper.P2_11 * 1E3; bitIndex += 27;
            Acceleration.X = RtcmV3Helper.GetBitG(buffer, bitIndex, 5) * RtcmV3Helper.P2_30 * 1E3; bitIndex += 5;
            Velocity.Y = RtcmV3Helper.GetBitG(buffer, bitIndex, 24) * RtcmV3Helper.P2_20 * 1E3; bitIndex += 24;
            Position.Y = RtcmV3Helper.GetBitG(buffer, bitIndex, 27) * RtcmV3Helper.P2_11 * 1E3; bitIndex += 27;
            Acceleration.Y = RtcmV3Helper.GetBitG(buffer, bitIndex, 5) * RtcmV3Helper.P2_30 * 1E3; bitIndex += 5;
            Velocity.Z = RtcmV3Helper.GetBitG(buffer, bitIndex, 24) * RtcmV3Helper.P2_20 * 1E3; bitIndex += 24;
            Position.Z = RtcmV3Helper.GetBitG(buffer, bitIndex, 27) * RtcmV3Helper.P2_11 * 1E3; bitIndex += 27;
            Acceleration.Z = RtcmV3Helper.GetBitG(buffer, bitIndex, 5) * RtcmV3Helper.P2_30 * 1E3; bitIndex += 5 + 1;
            RelativeFreqBias = RtcmV3Helper.GetBitG(buffer, bitIndex, 11) * RtcmV3Helper.P2_40; bitIndex += 11 + 3;
            SvClockBias = RtcmV3Helper.GetBitG(buffer, bitIndex, 22) * RtcmV3Helper.P2_30; bitIndex += 22;
            DelayL1ToL2 = RtcmV3Helper.GetBitG(buffer, bitIndex, 5) * RtcmV3Helper.P2_30; bitIndex += 5;
            OperationAge = (int)RtcmV3Helper.GetBitU(buffer, bitIndex, 5); bitIndex += 5;

            var sat = RtcmV3Helper.satno(sys, (int)prn);
            if (sat == 0)
            {
                throw new Exception($"rtcm3 1020 satellite number error: prn={prn}");
            }

            SatelliteNumber = sat;
            OperationHealth = (int)bn;
            Iode = (int)(tb & 0x7F);

            var week = 0;
            var tow = 0.0;
            RtcmV3Helper.GetFromTime(utc, ref week, ref tow);
            
            var tod = Math.Abs(tow - 86400.0); tow -= tod;
            var tof = tkH * 3600.0 + tkM * 60.0 + tkS - 10800.0; /* lt->utc */
            
            if (tof < tod - 43200.0) tof += 86400.0;
            else if (tof > tod + 43200.0) tof -= 86400.0;

            FrameTime = RtcmV3Helper.Utc2Gps(RtcmV3Helper.GetFromGps(week, tow + tof));
            var toe = tb * 900.0 - 10800.0; /* lt->utc */
            
            if (toe < tod - 43200.0) toe += 86400.0;
            else if (toe > tod + 43200.0) toe -= 86400.0;
            EphemerisEpoch = RtcmV3Helper.Utc2Gps(RtcmV3Helper.GetFromGps(week, tow + toe)); /* utc->gpst */

            SatelliteCode = RtcmV3Helper.Sat2Code(sat, (int)prn);

            return bitIndex - offsetBits;
        }

        /// <summary>
        /// satellite number
        /// </summary>
        public int SatelliteNumber { get; set; }

        public string SatelliteCode { get; set; }
        /// <summary>
        /// IODE (0-6 bit of tb field)
        /// </summary>
        public int Iode { get; set; }
        /// <summary>
        /// satellite frequency number
        /// </summary>
        public int FrequencyNumber { get; set; }
        /// <summary>
        /// satellite health of operation
        /// </summary>
        public int OperationHealth { get; set; }
        /// <summary>
        /// satellite accuracy of operation
        /// </summary>
        public int OperationAccuracy { get; set; }
        /// <summary>
        /// satellite age of operation
        /// </summary>
        public int OperationAge { get; set; }
        /// <summary>
        /// epoch of epherides (gpst)
        /// </summary>
        public DateTime EphemerisEpoch { get; set; }
        /// <summary>
        /// message frame time (gpst)
        /// </summary>
        public DateTime FrameTime { get; set; }

        /// <summary>
        /// satellite position (ecef) (m)
        /// </summary>
        public EcefPoint Position { get; } = new EcefPoint();

        /// <summary>
        /// satellite velocity (ecef) (m/s)
        /// </summary>
        public EcefPoint Velocity { get; } = new EcefPoint();

        /// <summary>
        /// satellite acceleration (ecef) (m/s^2)
        /// </summary>
        public EcefPoint Acceleration { get; } = new EcefPoint();

        /// <summary>
        /// SV clock bias (s)
        /// </summary>
        public double SvClockBias { get; set; }
        /// <summary>
        /// relative freq bias
        /// </summary>
        public double RelativeFreqBias { get; set; }
        /// <summary>
        /// delay between L1 and L2 (s)
        /// </summary>
        public double DelayL1ToL2 { get; set; }

        public override ushort MessageId => RtcmMessageId;
    }
}