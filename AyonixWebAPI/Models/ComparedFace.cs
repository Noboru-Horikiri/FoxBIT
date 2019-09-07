namespace FoxBIT.Ayonix.Models
{
    /// <summary>
    /// 顔の比較結果クラス
    /// </summary>
    public class ComparedFace
    {
        public int No { get; set; } = 0;
        public double Score { get; set; } = 0.0;
        public string ID { get; set; } = "";
        public string SubID { get; set; } = "";
        public Location MugLocation { get; set; }
    }
}