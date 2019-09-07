using System;

namespace FoxBIT.Ayonix.IPCCommon.Models
{
    /// <summary>
    /// 顔情報登録結果クラス
    /// </summary>
    [Serializable]
    public class ResultDetectedFace
    {
        public string FaceID { get; set; }
        public string FaceSubID { get; set; }
        public string AFID { get; set; }
        public Location MugLocation { get; set; }
    }
}
