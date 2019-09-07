using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using FoxBIT.Ayonix.IPCCommon;
using FoxBIT.IPC;

namespace FoxBIT.Ayonix
{
    /// <summary>
    /// AyonixAPIサービスクラス
    /// </summary>
    public partial class APIService : ServiceBase
    {
        /// <summary>
        /// IPCサーバー
        /// </summary>
        private Server<AyonixSharedClass> IPCServer{ get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public APIService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// サービス コントロール マネージャー (SCM) からのスタートコマンド受信イベント処理
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            Trace.WriteLine("OnStartしました。");
            
            // IPCサーバー初期化
            IPCServer = new Server<AyonixSharedClass>()
            {
                PortName = IPCConst.PORT_NAME,
                URI = IPCConst.URI,
                AuthorizedGroup = IPCConst.AUTHORIZED_GROUP
            };
            IPCServer.Create();
            IPCServer.SharedObj.Initialize();

            // AFIDをDBから読込み
            try
            {
                IPCServer.SharedObj.LoadFaces();
            }
            catch (System.Exception err)
            {
                Trace.WriteLine(err.Message);
                throw err;
            }

            Trace.WriteLine($"{IPCServer.SharedObj.CountAFID()}件");
        }

        /// <summary>
        /// サービス コントロール マネージャー (SCM) からのストップコマンド受信イベント処理
        /// </summary>
        protected override void OnStop()
        {
            Trace.WriteLine("OnStopしました。");

            Trace.WriteLine($"{IPCServer.SharedObj.CountAFID()}件");
        }
    }
}
