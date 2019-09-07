using System;

namespace FoxBIT.IPC
{
    public class SharedClass : MarshalByRefObject
    {
        /// <summary>
        /// 自動的に切断されるのを回避する
        /// </summary>
        /// <returns>
        /// このインスタンスの有効期間ポリシーを制御する有効期間サービス オブジェクト
        /// </returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
