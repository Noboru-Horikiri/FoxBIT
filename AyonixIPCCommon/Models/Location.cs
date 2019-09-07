using System;

namespace FoxBIT.Ayonix.IPCCommon.Models
{
    /// <summary>
    /// 位置情報
    /// </summary>
    [Serializable]
    public class Location
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int W { get; set; } = 0;
        public int H { get; set; } = 0;
    }
}
