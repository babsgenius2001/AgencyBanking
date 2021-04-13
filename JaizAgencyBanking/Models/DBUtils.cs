using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;

namespace JaizAgencyBanking.Models
{
    public class DBUtils
    {
        public static OracleConnection GetDBConnection()
        {
           return OracleUtils.GetDBConnection();
        }
    }
}