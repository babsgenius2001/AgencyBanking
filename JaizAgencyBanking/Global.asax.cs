using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace JaizAgencyBanking
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs   
            Exception Ex = Server.GetLastError();
            Server.ClearError();
           // IDP s = new IDP();
            var err2 = new LogUtility.Error()
            {
                ErrorDescription = "Error Accessing the Application: " + Ex.InnerException.ToString()+" Error Message"+Ex.Message,
                ErrorTime = DateTime.Now,
                ModulePointer = "Error",
                StackTrace = Ex.Message
            };
            LogUtility.ActivityLogger.WriteErrorLog(err2);
            //s.ErrorLog(Ex.Message);
        }

    }
}
