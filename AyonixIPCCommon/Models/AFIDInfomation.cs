namespace FoxBIT.Ayonix.IPCCommon.Models
{
    /// <summary>
    /// AFID情報クラス
    /// </summary>
    public class AFIDInfomation
    {
        public string FaceID { get; set; }
        public string FaceSubID { get; set; }
        public string AFIDString { get; set; }
        public byte[] AFID { get; set; }
    }
}
