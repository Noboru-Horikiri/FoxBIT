//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはテンプレートから生成されました。
//
//     このファイルを手動で変更すると、アプリケーションで予期しない動作が発生する可能性があります。
//     このファイルに対する手動の変更は、コードが再生成されると上書きされます。
// </auto-generated>
//------------------------------------------------------------------------------

namespace FoxBIT.Ayonix.DB.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class face_sub_ids
    {
        public string id { get; set; }
        public string sub_id { get; set; }
        public string afid { get; set; }
        public bool deleted { get; set; }
        public System.DateTime created_at { get; set; }
        public Nullable<System.DateTime> updated_at { get; set; }
        public Nullable<System.DateTime> deleted_at { get; set; }
    
        public virtual face_ids face_ids { get; set; }
    }
}
