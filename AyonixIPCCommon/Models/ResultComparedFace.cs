using System;

namespace FoxBIT.Ayonix.IPCCommon.Models
{
    /// <summary>
    /// 顔情報比較結果クラス
    /// </summary>
    [Serializable]
    public class ResultComparedFace : ResultDetectedFace
    {
        public int No { get; set; }
        public double Score { get; set; }
    }
}
