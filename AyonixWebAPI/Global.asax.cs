using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.IO;
using FoxBIT.IPC;
using FoxBIT.Ayonix.IPCCommon;

namespace FoxBIT.Ayonix
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        /// <summary>
        /// IPCクライアント
        /// </summary>
        static public Client<AyonixSharedClass> IPCClient { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            // レスポンス形式をJsonに限定する
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();
            
            // IPCクライアント初期化
            IPCClient = new Client<AyonixSharedClass>()
            {
                PortName = IPCConst.PORT_NAME,
                URI = IPCConst.URI
            };
            IPCClient.Create();

#if DEBUG
            // TODO:テスト用なのでリリース時には消すこと
            string path = Path.Combine(HttpContext.Current.Server.MapPath("~/"), $@"App_Data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
#endif
        }
    }
}
