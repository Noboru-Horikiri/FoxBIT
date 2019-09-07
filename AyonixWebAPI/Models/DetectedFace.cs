namespace FoxBIT.Ayonix.Models
{
    /// <summary>
    /// 顔の検出結果クラス
    /// </summary>
    public class DetectedFace
    {
        public string ID { get; set; } = "";
        public string SubID { get; set; } = "";
        public Location MugLocation { get; set; }
    }
}