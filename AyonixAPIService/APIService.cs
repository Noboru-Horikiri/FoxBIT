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
        private static TraceSource _logTraceSource = new TraceSource("ServiceLog");

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
            _logTraceSource.TraceEvent(TraceEventType.Information, Process.GetCurrentProcess().Id, $"OnStart - Start");

            // IPCサーバー初期化
            try
            {
                IPCServer = new Server<AyonixSharedClass>()
                {
                    PortName = IPCConst.PORT_NAME,
                    URI = IPCConst.URI,
                    AuthorizedGroup = IPCConst.AUTHORIZED_GROUP
                };
                IPCServer.Create();
                IPCServer.SharedObj.Initialize();
            }
            catch (System.Exception err)
            {
                _logTraceSource.TraceEvent(TraceEventType.Error, Process.GetCurrentProcess().Id, $"OnStart - {err.Message}");
                throw err;
            }

            // AFIDをDBから読込み
            try
            {
                IPCServer.SharedObj.LoadFaces();
            }
            catch (System.Exception err)
            {
                _logTraceSource.TraceEvent(TraceEventType.Error, Process.GetCurrentProcess().Id, $"OnStart - {err.Message}");
                throw err;
            }

            _logTraceSource.TraceEvent(TraceEventType.Information, Process.GetCurrentProcess().Id, $"OnStart - End AFID[{IPCServer.SharedObj.CountAFID()}]");
        }

        /// <summary>
        /// サービス コントロール マネージャー (SCM) からのストップコマンド受信イベント処理
        /// </summary>
        protected override void OnStop()
        {
            _logTraceSource.TraceEvent(TraceEventType.Information, Process.GetCurrentProcess().Id, $"OnStop - Start");
            
            _logTraceSource.TraceEvent(TraceEventType.Information, Process.GetCurrentProcess().Id, $"OnStop - End Result[CountAFID:{IPCServer.SharedObj.CountAFID()}]");
        }
    }
}
