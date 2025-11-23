namespace RCS.Common.Protocols
{
    public static class ProtocolConstants
    {
        // Hub Methods (Tên hàm mà Client/Server invoke)
        public const string RegisterAgent = "RegisterAgent";
        public const string ReceiveCommand = "ReceiveCommand";
        public const string SendResponse = "SendResponse";
        public const string SendUpdate = "SendUpdate";
        public const string SendBinaryStream = "SendBinaryStream";

        // Command Actions (Tên lệnh trong JSON)
        public const string ActionAppList = "app_list";
        public const string ActionAppStart = "app_start";
        public const string ActionAppStop = "app_stop";
        
        public const string ActionProcessList = "process_list";
        public const string ActionProcessStart = "process_start";
        public const string ActionProcessStop = "process_stop";

        public const string ActionScreenshot = "screenshot";
        
        public const string ActionKeyloggerStart = "keylogger_start";
        public const string ActionKeyloggerStop = "keylogger_stop";
        
        public const string ActionWebcamOn = "webcam_on";
        public const string ActionWebcamOff = "webcam_off";

        public const string ActionShutdown = "shutdown";
        public const string ActionRestart = "restart";
    }
}