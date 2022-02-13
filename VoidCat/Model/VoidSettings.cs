namespace VoidCat.Model
{
    public class VoidSettings
    {
        public string DataDirectory { get; init; } = "./data";
        
        public TorSettings? TorSettings { get; init; }
    }

    public class TorSettings
    {
        public TorSettings(Uri torControl, string privateKey, string controlPassword)
        {
            TorControl = torControl;
            PrivateKey = privateKey;
            ControlPassword = controlPassword;
        }
        
        public Uri TorControl { get; }
        public string PrivateKey { get; }
        
        public string ControlPassword { get; }
    }
}
