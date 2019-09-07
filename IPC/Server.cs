using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace FoxBIT.IPC
{
    /// <summary>
    /// IPCサーバークラス
    /// </summary>
    /// <typeparam name="T">共有オブジェクト</typeparam>
    public class Server<T> where T : SharedClass, new()
    {
        /// <summary>
        /// 共有オブジェクト
        /// </summary>
        public T SharedObj { get; set; } = new T();

        /// <summary>
        /// ポート名
        /// </summary>
        public string PortName { get; set; }

        /// <summary>
        /// リモートオブジェクトURI
        /// </summary>
        public string URI { get; set; }

        /// <summary>
        /// 認証グループ
        /// </summary>
        public string AuthorizedGroup { get; set; }

        /// <summary>
        /// サーバー初期化
        /// </summary>
        public void Create()
        {
            // サーバサイドのチャンネルを生成
            var properties = new Hashtable
            {
                { Const.SERVER_CHANNEL_PORT_NAME_KEY, PortName },
                { Const.SERVER_CHANNEL_AUTHORIZED_GROUP_KEY, AuthorizedGroup }
            };
            var channel = new IpcServerChannel(properties, null);

            //チャンネルを登録
            ChannelServices.RegisterChannel(channel, true);

            // URI設定
            RemotingServices.Marshal(SharedObj, URI);
        }
    }
}
