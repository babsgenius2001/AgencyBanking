using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace JaizAgencyBanking.Models
{
    public class BVN
    {
        public BVN()
        {
            //URL = ConfigurationManager.AppSettings["BVN2"];
        }
        //public string URL { get; set; }
        //public string URL2 { get; set; }
        public BVNResponse Request(string BVN)
        {
            BillsPaymentService.JaizHelper re = new BillsPaymentService.JaizHelper();
            var retstat = re.GetBVNVersion2(BVN);
            if (retstat != null)
            {
                BVNResponse rs = new BVNResponse();
                rs.base64Image = retstat.base64Image;
                rs.bvn = retstat.bvn;
                rs.dateOfBirth = retstat.dateOfBirth;
                rs.email = retstat.email;
                rs.enrollmentBank = retstat.enrollmentBank;
                rs.enrollmentBranch = retstat.enrollmentBranch;
                rs.firstName = retstat.firstName;
                rs.gender = retstat.gender;
                rs.lastName = retstat.lastName;
                rs.maritalStatus = retstat.maritalStatus;
                rs.phoneNumber1 = retstat.phoneNumber1;
                rs.phoneNumber2 = retstat.phoneNumber2;
                rs.residentialAddress = retstat.residentialAddress;
                rs.title = retstat.title;
                rs.nationality = retstat.nationality;
                rs.responseDescription = retstat.responseDescription;
                rs.responseCode = retstat.responseCode;
                return rs;
            }
            //var request = (HttpWebRequest)WebRequest.Create(URL + "/JaizBVNVersion2/api/FetchSingleBVNDetails");
            //var req = new BVNREQ { bvn = BVN };
            //string json = JsonConvert.SerializeObject(req);
            //request.Method = "POST";
            //var requestStringBytes = Encoding.UTF8.GetBytes(json);
            //request.GetRequestStream().Write(requestStringBytes, 0, requestStringBytes.Length);
            //request.ContentType = "application/json";
            //request.Timeout = 999999999;
            //var response = (HttpWebResponse)request.GetResponse();
            //var responseString = new StreamReader(
            //response.GetResponseStream()).
            //ReadToEnd();
            //var responseString = System.IO.File.ReadAllText(@"C:\inetpub\wwwroot\JaizBankUSSDAPI\bvnresponse.txt");
            ////var responseString = System.IO.File.ReadAllText(@"C:\Users\NA01190\source\repos\Jaiz USSD Live\JaizAgencyBanking\bvnresponse.txt");
            //var Xxc = JsonConvert.DeserializeObject<BVNResponse>(responseString);
            return null;
        }
        public class BVNREQ
        {
            public string bvn { get; set; }
        }
        public class BVNResponse
        {
            public string responseCode { get; set; }
            public string responseDescription { get; set; }
            public string bvn { get; set; }
            public string title { get; set; }
            public string firstName { get; set; }
            public string middleName { get; set; }
            public string lastName { get; set; }
            public string dateOfBirth { get; set; }
            public string maritalStatus { get; set; }
            public string email { get; set; }
            public string gender { get; set; }
            public string nationality { get; set; }
            public string residentialAddress { get; set; }
            public string phoneNumber1 { get; set; }
            public string phoneNumber2 { get; set; }
            public string stateOfOrigin { get; set; }
            public string stateOfResidence { get; set; }
            public object localGovernmentOfOrigin { get; set; }
            public object localGovernmentOfResidence { get; set; }
            public string registrationDate { get; set; }
            public string enrollmentBank { get; set; }
            public string enrollmentBranch { get; set; }
            public string levelOfAccount { get; set; }
            public object nationalIdentificationNumber { get; set; }
            public string nameOnCard { get; set; }
            public string watchListed { get; set; }
            public string base64Image { get; set; }
        }
    }
}
