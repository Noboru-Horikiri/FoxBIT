using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace FoxBIT.IPC
{
    /// <summary>
    /// IPCクライアントクラス
    /// </summary>
    /// <typeparam name="T">共有オブジェクト</typeparam>
    public class Client<T> : Object where T : SharedClass, new()
    {
        /// <summary>
        /// 共有オブジェクト
        /// </summary>
        public T SharedObj { get; set; }

        /// <summary>
        /// ポート名
        /// </summary>
        public string PortName { get; set; }

        /// <summary>
        /// リモートオブジェクトURI
        /// </summary>
        public string URI { get; set; }
        
        /// <summary>
        /// クライアント初期化
        /// </summary>
        public void Create()
        {
            //クライアントサイドのチャンネルを生成
            var channel = new IpcClientChannel();

            //チャンネルを登録
            ChannelServices.RegisterChannel(channel, true);

            //URIからリモートオブジェクトを取得
            SharedObj = Activator.GetObject(typeof(T), $@"ipc://{PortName}/{URI}") as T;
        }
    }
}
