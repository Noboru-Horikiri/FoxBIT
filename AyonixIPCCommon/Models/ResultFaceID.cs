using System;
using System.Collections.Generic;

namespace FoxBIT.Ayonix.IPCCommon.Models
{
    /// <summary>
    /// 顔情報取得結果クラス
    /// </summary>
    [Serializable]
    public class ResultFaceID
    {
        public string FaceID { get; set; }
        public List<string> FaceSubID { get; set; }
    }
}
