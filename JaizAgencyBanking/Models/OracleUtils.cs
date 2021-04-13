using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;

namespace JaizAgencyBanking.Models
{
    public class OracleUtils
    {
        public static  OracleConnection GetDBConnection()
        {

            //Console.WriteLine("Getting Connection ...");

            // Connection string to connect directly to Oracle.
            string host = ConfigurationManager.AppSettings["host"];
            string port = ConfigurationManager.AppSettings["port"];
            string sid = ConfigurationManager.AppSettings["sid"];
            string user = ConfigurationManager.AppSettings["user"];
            string password= ConfigurationManager.AppSettings["password"];
            string connString = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = "
                 + host + ")(PORT = " + port + "))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = "
                 + sid + ")));Password=" + password + ";User ID=" + user;


            OracleConnection conn = new OracleConnection();

            conn.ConnectionString = connString;

            return conn;
        }
    }
}