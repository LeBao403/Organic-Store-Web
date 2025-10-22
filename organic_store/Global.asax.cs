//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;
//using System.Web.Optimization;
//using System.Web.Routing;

//namespace organic_store
//{
//    public class MvcApplication : System.Web.HttpApplication
//    {
//        protected void Application_Start()
//        {
//            AreaRegistration.RegisterAllAreas();
//            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
//            RouteConfig.RegisterRoutes(RouteTable.Routes);
//            BundleConfig.RegisterBundles(BundleTable.Bundles);
//        }
//    }
//}
using System.Configuration; // Để đọc Web.config
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Neo4j.Driver; // Cần thiết

namespace organic_store
{
    public class MvcApplication : HttpApplication
    {
        // 1. Biến tĩnh để giữ Driver (Singleton)
        public static IDriver Neo4jDriver { get; private set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // 2. Thiết lập Neo4j Driver
            var uri = ConfigurationManager.AppSettings["Neo4jUri"];
            var user = ConfigurationManager.AppSettings["Neo4jUser"];
            var password = ConfigurationManager.AppSettings["Neo4jPassword"];

            // Khởi tạo Driver
            Neo4jDriver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));

            // Tùy chọn: Kiểm tra kết nối
            // Neo4jDriver.VerifyConnectivityAsync().Wait(); 
        }

        protected void Application_End()
        {
            // Quan trọng: Đảm bảo đóng Driver khi ứng dụng dừng
            if (Neo4jDriver != null)
            {
                Neo4jDriver.Dispose();
            }
        }
    }
}