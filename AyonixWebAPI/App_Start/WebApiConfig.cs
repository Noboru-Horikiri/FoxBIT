﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace FoxBIT.Ayonix
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API の設定およびサービス

            // Web API ルート
            config.MapHttpAttributeRoutes();
        }
    }
}
