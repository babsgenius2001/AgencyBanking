using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace JaizAgencyBanking.Models
{
    public class AccountOpeningAPI
    {
        public AcctResp CreateAccount(AcctReq Reference)
        {
            try
            {
                string url = ConfigurationManager.AppSettings["AccountOpeningAPI"];
                var request = (HttpWebRequest)WebRequest.Create(url + "/api/CustomerAPI/AccountCreationNew");

                string json = JsonConvert.SerializeObject(Reference);
                request.Method = "POST";
                var requestStringBytes = Encoding.UTF8.GetBytes(json);
                request.GetRequestStream().Write(requestStringBytes, 0, requestStringBytes.Length);
                request.ContentType = "application/json";
                request.Timeout = 999999999;
                string key = @"2023a9404bae8456004fb0162e5c1e9d1a2d45cc5eb5dbcb4f2e2c422bdb264981018f308c0a9abc622ed8a147a94b661c9925333c8af2ce8d34ef9c0644781b";
                request.Headers.Add("Authorization", key);
                //var response = (HttpWebResponse)request.GetResponse();
                //var responseString = new StreamReader(
                //response.GetResponseStream()).
                //ReadToEnd();

                string responseMassage = "";
                string statusCode = "";
                try
                {
                    // Make the call
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        responseMassage = LogResponse(response);
                        statusCode = response.StatusCode.ToString();
                    }
                }
                catch (WebException e)
                {
                    var assd = e;
                    if (e.Response is HttpWebResponse)
                    {
                        HttpWebResponse response = (HttpWebResponse)e.Response;
                        responseMassage = LogResponse(response);
                        statusCode = response.StatusCode.ToString();
                    }
                    var rsp = new AcctResp { accountNo = "", cif = "", responseCode = "400", responseMessage = responseMassage };
                    return rsp;
                }
                var Xxc = JsonConvert.DeserializeObject<AcctResp>(responseMassage);
                return Xxc;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private string LogResponse(HttpWebResponse response)
        {
            string responseBody;

            using (var reader = new StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
            {
                responseBody = reader.ReadToEnd();
            }
            //Sterling.MSSQL.ErrorLog lo = new Sterling.MSSQL.ErrorLog("<LogResponse>: AccountOpening : " + responseBody);
            return responseBody;
        }
    }
    
    public class AcctResp
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountNo { get; set; }
    }
    public class AcctReq
    {
        public string firstname { get; set; }
        public string secondname { get; set; }
        public string lastname { get; set; }
        public string sex { get; set; }
        public string address { get; set; }
        public string dob { get; set; }
        public string telephone { get; set; }
        public string accountName { get; set; }
        public string branchcode { get; set; }
        public string curencycode { get; set; }
        public string glcode { get; set; }
        public string cif { get; set; }
        public int title { get; set; }
        public int idtype { get; set; }
        public string idno { get; set; }
        public string marital { get; set; }
        public string division { get; set; }
        public string dept { get; set; }
        public string ecosector { get; set; }
        public string addref { get; set; }
        public string bvn { get; set; }
        public string Image { get; set; }
        public string SignatureImage { get; set; }
        public string marketedbyid { get; set; }
    }
}