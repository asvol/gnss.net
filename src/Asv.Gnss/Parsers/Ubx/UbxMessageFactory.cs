namespace Asv.Gnss
{
    public static class UbxMessageFactory
    {
        public static UbxBinaryParser RegisterDefaultFrames(this UbxBinaryParser src)
        {
            src.Register(() => new UbxAck());
            src.Register(() => new UbxNak());
            src.Register(() => new UbxNavPvt());
            src.Register(() => new UbxMonitorVersion());
            src.Register(() => new UbxMonitorHardware());
            src.Register(() => new UbxTimeModeConfiguration());
            src.Register(() => new UbxNavSurveyIn());
            src.Register(() => new UbxNavSatellite());
            src.Register(() => new UbxInfWarning());
            src.Register(() => new UbxBackupRestoreStatus());
            return src;
        }
    }
}