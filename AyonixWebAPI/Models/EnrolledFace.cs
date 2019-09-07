using System.Collections.Generic;

namespace FoxBIT.Ayonix.Models
{
    /// <summary>
    /// 登録済み顔情報クラス
    /// </summary>
    public class EnrolledFace
    {
        public string ID { get; set; }
        public List<string> SubID { get; set; }
    }
}