using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using JaizAgencyBanking.Models;
using Newtonsoft.Json;
using System.Web;
using System.Data.Entity.Validation;
using System.IO; 
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using System.Data.Objects;
//using System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Data;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
using System.Net.Security;
using System.Threading.Tasks;
using System.Data.SqlClient;
using RestSharp;
using NLog;

namespace JaizAgencyBanking.Controllers
{

    public class CustomerController : ApiController
    {
        private OracleConnection oracleConnection;
        private OracleConnectionStringBuilder stringBuilder;
        // Exceptionhandling exc = new Exceptionhandling();

        protected static Logger _jobLogger = LogManager.GetCurrentClassLogger();

        public long logRequest(string ip,string xml)
        {
            long logID = 0;
            try
            {
                using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                {
                    log l = new log();

                    l.logIP = ip;
                    l.logXML = xml;
                    l.logDate = DateTime.Now;

                    db.logs.Add(l);
                    db.SaveChanges();

                    logID=l.logID;
                }
            }
            catch(Exception ve){
                 var err2 = new LogUtility.Error()
                        {
                            ErrorDescription = "Error Updating Log request: "+ve.InnerException.ToString(),
                            ErrorTime = DateTime.Now,
                            ModulePointer = "Update request",
                            StackTrace = ve.Message
                        };
                        LogUtility.ActivityLogger.WriteErrorLog(err2);
            }
            return logID;
        }

        //get clients ip address
        public static string ClientIP
        {
            get
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
        }
        public long updateRequest(long ID, string response)
        {
            try
            {
                using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                {
                    var details = db.logs.FirstOrDefault(a => a.logID == ID);
                    details.logXMLOut = response;
                    
                    details.logXMLOutDate = DateTime.Now;
                    db.SaveChanges();

                    return details.logID;




                }
            }
            catch (Exception ve)
            {
                      var err2 = new LogUtility.Error()
                        {
                            ErrorDescription = "Error Updating Log request: "+ve.InnerException.ToString(),
                            ErrorTime = DateTime.Now,
                            ModulePointer = "Update request",
                            StackTrace = ve.Message
                        };
                        LogUtility.ActivityLogger.WriteErrorLog(err2);
                    
                

                return 0;
            }
        }

        public long logRequestResponse(string ip, string xml, DateTime loginTime, string respXML, string nameOfmethod)
        {
            long logID = 0;
            try
            {
                using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                {
                    log l = new log();

                    l.logIP = ip;
                    l.logXML = xml;
                    l.logDate = loginTime;
                    l.logXMLOut = respXML;
                    l.logXMLOutDate = DateTime.Now;
                    l.methodName = nameOfmethod;                    

                    db.logs.Add(l);
                    db.SaveChanges();

                }
            }
            catch (Exception ve)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = "Error Inserting into Log for Request: " + ve.InnerException.ToString(),
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Insert to Log Table Request",
                    StackTrace = ve.Message
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
            }
            return logID;
        }




        public static String GetIP()
        {
            String ip =
                HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ip;
        }
       
        [HttpPost]
        [Route("api/CustomerAPI/TokenValidation")]
        public HttpResponseMessage TokenValidation([FromBody] Token t)
        {
            var ip = GetIP();

            var url = ConfigurationManager.AppSettings["staffidurl"];
            staffIDRequest ac = new staffIDRequest();
            ac.email = t.TokenID;
            string rs = JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            result = client.PostAsync(url, content).Result;
            var rrrr = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var rp = JsonConvert.DeserializeObject<staffUserIDResponse>(rrrr.ToString());
            var newUserID = rp.username;
            DateTime loginTime = DateTime.Now;

            string req = JsonConvert.SerializeObject(t);
            //long logID = logRequest(ip, req);
            responseclass resp = new responseclass();

            try
            {
                InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                InternetBankingAPI.TokenResponse O = new InternetBankingAPI.TokenResponse();
                InternetBankingAPI.TokenObject Z = new InternetBankingAPI.TokenObject();

                Z.TokenID = newUserID;
                Z.OTP = t.OTP;
                bool T = true;
                S.ValidateToken(Z, out O, out T);                

                if (O.ToString() == "SUCCESSFUL")
                {
                    resp.responseCode = "00";
                    resp.responseMessage = O.ToString();
                }
                else
                {
                    resp.responseCode = "66";
                    resp.responseMessage = O.ToString();
                }

                //update log 
                string r = JsonConvert.SerializeObject(resp);

                logRequestResponse(ip, req, loginTime, r, "TokenValidation");

            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "96";
                resp.responseMessage = "Error Logging Message";

                string r = JsonConvert.SerializeObject(resp);

                logRequestResponse(ip, req, loginTime, ex.Message + " Response from server:" + r, "TokenValidation");

            }
            string output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

       
        //public int CreateAccount(){
        //     OracleConnection conn = DBUtils.GetDBConnection();
        //    conn.Open();
        //    try
        //    {

        //        // Create a Command object to call Get_Employee_Info procedure.
        //        OracleCommand cmd = new OracleCommand("P_API_ACCOUNT_CREATION", conn);

        //        // Command Type is StoredProcedure
        //        cmd.CommandType = CommandType.StoredProcedure;

        //        // Add parameter @p_Emp_Id and set value = 100
        //        cmd.Parameters.Add("@p_Emp_Id", OracleDbType.Int32).Value = 100;

        //        // Add parameter @v_Emp_No type of Varchar(20).
        //        cmd.Parameters.Add(new OracleParameter("@v_Emp_No", OracleDbType.Varchar2, 20));
        //        cmd.Parameters.Add(new OracleParameter("@v_First_Name", OracleDbType.Varchar2, 50));
        //        cmd.Parameters.Add(new OracleParameter("@v_Last_Name", OracleDbType.Varchar2, 50));
        //        cmd.Parameters.Add(new OracleParameter("@v_Hire_Date", OracleDbType.Date));

        //        // Register parameter @v_Emp_No is OUTPUT.
        //        cmd.Parameters["@v_Emp_No"].Direction = ParameterDirection.Output;
        //        cmd.Parameters["@v_First_Name"].Direction = ParameterDirection.Output;
        //        cmd.Parameters["@v_Last_Name"].Direction = ParameterDirection.Output;
        //        cmd.Parameters["@v_Hire_Date"].Direction = ParameterDirection.Output;

        //        // Execute procedure.
        //        cmd.ExecuteNonQuery();

        //        // Get output values.
        //        string empNo = cmd.Parameters["@v_Emp_No"].Value.ToString();
        //        string firstName = cmd.Parameters["@v_First_Name"].Value.ToString();
        //        string lastName = cmd.Parameters["@v_Last_Name"].Value.ToString();
        //        object hireDateObj = cmd.Parameters["@v_Hire_Date"].Value;
        //    }
        //}
        public internalbvnvalidation InternalBVNValidation(string bvnno)
        {
           
            var url = ConfigurationManager.AppSettings["bvnurl"].ToString();
            internalbvnvalidation resp = new internalbvnvalidation();
            try
            {
                
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.Accept = "application/json; charset=utf-8";
                using (var streamWriter = new

                StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        bvn = bvnno,

                    });

                    streamWriter.Write(json);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    result = result.Replace("\\\"", "'");
                    result = result.Replace("\"", "");
                    result = result.Replace("\"", string.Empty);
                     XmlDocument xmlDoc = new XmlDocument();
                     xmlDoc.LoadXml(result);
                     XmlNodeList xnList = xmlDoc.SelectNodes("/SearchResult");
                     XmlNodeList titles = xmlDoc.GetElementsByTagName("Bvn");
                     XmlNodeList FirstName = xmlDoc.GetElementsByTagName("FirstName");
                     XmlNodeList MiddleName = xmlDoc.GetElementsByTagName("MiddleName");
                     XmlNodeList LastName = xmlDoc.GetElementsByTagName("LastName");
                     XmlNodeList Dob = xmlDoc.GetElementsByTagName("DateOfBirth");
                     XmlNode n2 = xnList[0];
                     var newxml = n2["BvnSearchResult"].InnerXml;
                     string bvn = titles[0].InnerText;
                     
                     resp.firstname = FirstName[0].InnerText;
                     resp.middlename = MiddleName[0].InnerText;
                     resp.lastname = LastName[0].InnerText;

                     return resp;

                }             

            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "BVN Validation - BVN Validation Error",
                    StackTrace = ex.StackTrace
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                
              
                //HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, ex.Message.ToString());
                return resp;
            }

        }
        [HttpPost]
        [Route("api/CustomerAPI/BVNValidation")]
        public HttpResponseMessage BVNValidation([FromBody] bvnrequest req)
        {
            var ip = GetIP();
            string rq = JsonConvert.SerializeObject(req);
            DateTime loginTime = DateTime.Now;

            var url = ConfigurationManager.AppSettings["bvnurl"].ToString();
            try
            {
                //var content = new StringContent(rq, Encoding.UTF8, "text/json");
                //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //var client = new HttpClient();

                //HttpResponseMessage result = null;
                
                //result = client.PostAsync(url, content).Result;
                ////var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                //var resp = JsonConvert.DeserializeObject<Rootobject>(r.ToString());
               // output = JsonConvert.SerializeObject(ree);



                //HttpResponseMessage res = Request.CreateResponse(HttpStatusCode.OK, newrr.ToString());
                //return res;
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                //var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://172.21.19.30:8183/JaizBankAPI/api/CustomerAPI/BVNValidation");
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.Accept = "application/json; charset=utf-8";
                using (var streamWriter = new

                StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        bvn = req.bvn,

                    });

                    streamWriter.Write(json);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    //result = new Regex("\\<\\?xml.*\\?>").Replace(result, string.Empty);
                    //result = Regex.Replace(result, "[@]", string.Empty);
                    result = result.Replace("\\\"", "'");
                    result = result.Replace("\"", "");
                    result = result.Replace("\"", string.Empty);
                   /* XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result);
                    XmlNodeList xnList = xmlDoc.SelectNodes("/SearchResult");
                    XmlNodeList titles = xmlDoc.GetElementsByTagName("Bvn");
                    XmlNodeList FirstName = xmlDoc.GetElementsByTagName("FirstName");
                    XmlNodeList MiddleName = xmlDoc.GetElementsByTagName("MiddleName");
                    XmlNodeList LastName = xmlDoc.GetElementsByTagName("LastName");
                    XmlNodeList Dob = xmlDoc.GetElementsByTagName("DateOfBirth");
                    XmlNode n2 = xnList[0];
                    var newxml = n2["BvnSearchResult"].InnerXml;
                    string bvn = titles[0].InnerText;*/
                }

                logRequestResponse(ip, rq, loginTime, result.ToString(), "BVNValidation");

                return new HttpResponseMessage()
                {
                    Content = new StringContent(result.ToString(), System.Text.Encoding.UTF8, "application/xml")
                };

            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "BVN Validation - BVN Validation Error",
                    StackTrace = ex.StackTrace
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                logRequestResponse(ip, rq, loginTime, ex.Message.ToString(), "BVNValidation");

                HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, ex.Message.ToString());
                return resp;
            }

        }
        
        [HttpPost]
        [Route("api/CustomerAPI/BVNValidation2")]
        public HttpResponseMessage BVNValidation2([FromBody] bvnrequest req)
        {
            var ip = GetIP();
            string rq = JsonConvert.SerializeObject(req);
            long logID = logRequest(ip, rq);
            try
            {
                //var doc = new XmlDocument();
                //doc.Load(req.Content.ReadAsStreamAsync().Result);
                //var r = doc.DocumentElement.OuterXml;



                ////log request


                //// var r = GetString(req);
                ////requestID = LogRequest(r);
                //XmlDocument xml = new XmlDocument();
                //xml.LoadXml(r);

                //var bvn = "";

                //XmlNodeList xnList = xml.SelectNodes("BVNValidation");
                //foreach (XmlNode xn in xnList)
                //{
                //    bvn = xn["bvn"].InnerText;

                //}
                //encrypt data first
                var BBVN = EncryptRequest(req.bvn);
                var svc = new NIBSSBVN.BVNValidation();
                var result = svc.verifySingleBVN(BBVN, "301");
                var res = DecryptRequest(result);

                XmlDocument xml2 = new XmlDocument();
                xml2.LoadXml(res);

                XmlNodeList xnList2 = xml2.SelectNodes("BvnSearchResult");

                /*
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><SearchResult><ResultStatus>00</ResultStatus><BvnSearchResult><Bvn>22152678621</Bvn><FirstName>MORADEUN</FirstName><MiddleName>ADESOLA</MiddleName><LastName>OLABISI</LastName><DateOfBirth>04-MAR-73</DateOfBirth><PhoneNumber>08036868348</PhoneNumber><RegistrationDate>16-NOV-14</RegistrationDate><EnrollmentBank>033</EnrollmentBank><EnrollmentBranch>IJEBU ODE</EnrollmentBranch></BvnSearchResult></SearchResult>

        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><SearchResult><ResultStatus>00</ResultStatus><BvnSearchResult><Bvn>22152678579</Bvn><FirstName>TAOFEEK</FirstName><MiddleName>OLANRENWAJU</MiddleName><LastName>ADESINA</LastName><DateOfBirth>02-JUL-64</DateOfBirth><PhoneNumber>08023371842</PhoneNumber><RegistrationDate>16-NOV-14</RegistrationDate><EnrollmentBank>011</EnrollmentBank><EnrollmentBranch>Shomolu</EnrollmentBranch><ImageBase64></ImageBase64></BvnSearchResult></SearchResult>
        */

                var rs = "";
                foreach (XmlNode xn in xnList2)
                {
                    string bvnno = xn["Bvn"].InnerText;
                    string FirstName = xn["FirstName"].InnerText;
                    string MiddleName = xn["MiddleName"].InnerText;
                    string LastName = xn["LastName"].InnerText;
                    DateTime dob = Convert.ToDateTime(xn["DateOfBirth"].InnerText);
                    string PhoneNumber = xn["PhoneNumber"].InnerText;
                    DateTime RegDate = Convert.ToDateTime(xn["RegistrationDate"].InnerText);

                    XmlDocument xml22 = new XmlDocument();
                    XmlElement root = xml22.CreateElement("BVNValidationResponse");
                    xml22.AppendChild(root);

                    XmlElement BVNNno = xml22.CreateElement("BVN");
                    XmlElement fName = xml22.CreateElement("FirstName");
                    XmlElement mName = xml22.CreateElement("MiddleName");
                    XmlElement lName = xml22.CreateElement("LastName");
                    XmlElement DateOBirth = xml22.CreateElement("DOB");
                    XmlElement pNumber = xml22.CreateElement("PhoneNumber");
                    XmlElement rDate = xml22.CreateElement("RegistrationDate");


                    BVNNno.InnerText = bvnno;
                    fName.InnerText = FirstName;
                    mName.InnerText = MiddleName;
                    lName.InnerText = LastName;
                    DateOBirth.InnerText = dob.ToString();
                    pNumber.InnerText = PhoneNumber;
                    rDate.InnerText = RegDate.ToString();


                    root.AppendChild(BVNNno);
                    root.AppendChild(fName);
                    root.AppendChild(mName);
                    root.AppendChild(lName);
                    root.AppendChild(DateOBirth);
                    root.AppendChild(pNumber);
                    root.AppendChild(rDate);



                    rs = xml22.OuterXml;
                }

                //updateLog(requestID, res.ToString());
                updateRequest(logID, res.ToString());
                HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, res.ToString());
                return resp;


            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "BVN Validation - BVN Validation Error",
                    StackTrace = ex.StackTrace
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                //updateLog(requestID, ex.Message.ToString());
                updateRequest(logID, ex.Message.ToString());
                HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, ex.Message.ToString());
                return resp;
            }



        }
        public Image LoadImage(string img)
        {
            //data:image/gif;base64,
            //this image is a single pixel (black)
            byte[] bytes = Convert.FromBase64String(img);

            Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
            }

            return image;
        }
        [HttpPost]
        [Route("api/CustomerAPI/AccountCreation")]
        public HttpResponseMessage AccountCreation([FromBody] AccountOpening account)
        {
             var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {               
                auth = authenticateUsers(username, password);                
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            AccountOpeningResponse ree = new AccountOpeningResponse();

            string req = JsonConvert.SerializeObject(account);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
            string output = "";
            var rs = "";
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                BVN bs = new BVN();
                var resp = bs.Request(account.bvn);//req.bvn
                if (resp.responseDescription != null && !resp.responseCode.Contains("00"))
                {
                    ree.responseCode = "x02";
                    ree.responseMessage = "please verify BVN details and try again";
                    string r2O = JsonConvert.SerializeObject(ree);
                    updateRequest(logID, r2O);
                    output = JsonConvert.SerializeObject(ree);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(r2O.ToString(), System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (resp.firstName == null)
                {
                    ree.responseCode = "x03";
                    ree.responseMessage = "Cant not get BVN details, try again";
                    string r2O = JsonConvert.SerializeObject(ree);
                    updateRequest(logID, r2O);
                    output = JsonConvert.SerializeObject(ree);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(r2O.ToString(), System.Text.Encoding.UTF8, "application/json")
                    };
                }
                Random nx = new Random();
                DateTime inputDate = Convert.ToDateTime(resp.dateOfBirth);
                string InDate = inputDate.ToString("yyyy-MM-dd");//== string.Empty ? "" : x.bvn,
                AcctReq accountReq = new AcctReq()
                {
                    accountName = account.firstname +" "+ resp.middleName+" " + account.lastname,//account.accountName,
                    addref = account.address,
                    address = account.address,
                    branchcode = Convert.ToString(account.branchcode),
                    bvn = account.bvn,
                    cif = account.cif,
                    curencycode = "566",
                    dept = "223",
                    division = "22",
                    dob = InDate,
                    ecosector = "8",
                    firstname = account.firstname == string.Empty ? "" : account.firstname,
                    glcode = "210808",
                    idno = Convert.ToString(nx.Next(10000008,99999999)),
                    idtype = Convert.ToInt32(account.idtype),
                    Image = account.Image, 
                    secondname = resp.middleName,
                    lastname = account.lastname == string.Empty ? "" : account.lastname,
                    marital = account.marital,
                    sex = account.sex,
                    SignatureImage = account.SignatureImage == string.Empty ? "" : account.SignatureImage,
                    telephone = account.telephone == string.Empty ? "" : account.telephone,
                    title = Convert.ToInt32(account.title),
                    marketedbyid = "99999001"//Agency Banking Advansio
                };
                AccountOpeningAPI kio = new AccountOpeningAPI();
                var acct = kio.CreateAccount(accountReq);
                var cifNo = "";
                var acctNo = "";
                var msg = "";
                if (acct == null ||  acct.responseCode != "00")
                {
                    msg = "Failed " + acct.responseMessage;
                }

                if (acct != null && acct.responseCode == "00")
                {
                    acctNo = acct.accountNo;
                    cifNo = acct.cif;
                    msg = "Account Successfully Created";
                }else
                {
                    msg = "Failed "+ acct.responseMessage;
                }

                //upload image
                var bytes = Convert.FromBase64String(account.Image);
                var bytes2 = Convert.FromBase64String(account.SignatureImage);
                // string filedir = Path.Combine(Directory.GetCurrentDirectory(), "~/Uploads");
                // Debug.WriteLine(filedir);
                //Debug.WriteLine(Directory.Exists(filedir));
                string filedir = System.Web.Hosting.HostingEnvironment.MapPath("~/Uploads/");

                if (!Directory.Exists(filedir))
                { //check if the folder exists;
                    Directory.CreateDirectory(filedir);
                }
                string fileName = "Passport"+acctNo + ".jpg";
                string fileName2 = "Signature" + acctNo + ".jpg";
                string file = Path.Combine(filedir, fileName);
                string file2 = Path.Combine(filedir, fileName2);
                if (bytes.Length > 0)
                {
                    using (var stream = new FileStream(file, FileMode.Create))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();
                    }
                }
                if (bytes2.Length > 0)
                {
                    using (var stream = new FileStream(file2, FileMode.Create))
                    {
                        stream.Write(bytes2, 0, bytes2.Length);
                        stream.Flush();
                    }
                }
                //var httpRequest = HttpContext.Current.Request;
                //updateRequest(logID, req);
                ree.accountNo = acctNo;                
                ree.cif = cifNo;
                if(string.IsNullOrEmpty(ree.accountNo))
                    ree.responseCode = "x02";
                else
                    ree.responseCode = "00";
                ree.responseMessage = msg;
                string r2 = JsonConvert.SerializeObject(ree);
                updateRequest(logID, r2);
                output = JsonConvert.SerializeObject(ree);

                //if bvn was provided, do the linking automatically
                //if (!string.IsNullOrEmpty(account.bvn))
                //{
                //    internalbvnvalidation r = InternalBVNValidation(account.bvn);
                //    var bvnfirstname = r.firstname;
                //    var bvnmiddlename = r.middlename;
                //    var bvnlastname = r.lastname;
                //    //compare details
                //    if ((account.firstname.ToLower() == bvnfirstname.ToLower()) && (account.lastname.ToLower() == bvnlastname.ToLower()))
                //    {
                //        //go for linking
                //        try
                //        {
                //            linkbvnresponse re2 = LinkBVNByProxy(acctNo, account.bvn, cifNo);
                //            //resp.responseCode = re.responseCode;
                //            //resp.responseMessage = re.responseMessage;
                //        }
                //        catch (Exception ex)
                //        {
                //            var err2 = new LogUtility.Error()
                //            {
                //                ErrorDescription = ex.Message + ex.InnerException,
                //                ErrorTime = DateTime.Now,
                //                ModulePointer = "Error Linking BVN on account creation",
                //                StackTrace = ex.StackTrace
                //            };
                //            LogUtility.ActivityLogger.WriteErrorLog(err2);
                //        }
                //    }
                //}
                //else
                //{
                //    //put account on PND
                //    //ActivatePND(acctNo);
                //}
                //HttpResponseMessage res = Request.CreateResponse(HttpStatusCode.OK, newrr.ToString());
                //return res;
                return new HttpResponseMessage()
                {
                    Content = new StringContent(r2.ToString(), System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r2 = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r2);
                output = JsonConvert.SerializeObject(resp);

                XmlDocument xml3 = new XmlDocument();
                XmlElement root = xml3.CreateElement("AccountOpeningResponse");
                xml3.AppendChild(root);

                XmlElement ResponseCode = xml3.CreateElement("ResponseCode");
                XmlElement ResponseMessage = xml3.CreateElement("lastname");
                ResponseCode.InnerText = "99";
                ResponseMessage.InnerText = "Invalid Username and Password";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);

                var rss = xml3.OuterXml;

                HttpResponseMessage res = Request.CreateResponse(HttpStatusCode.OK, rss.ToString());
                return res;
            }
        }

        //[HttpPost]
        //[Route("api/CustomerAPI/AccountCreationNew")]
        //public HttpResponseMessage AccountCreationNew([FromBody] AccountOpening account)
        //{
        //    var username = "";
        //    var password = "";
        //    int auth = 0;

        //    var re = Request;
        //    var headers = re.Headers;

        //    if (headers.Contains("Username"))
        //    {
        //        username = headers.GetValues("Username").First();
        //    }
        //    if (headers.Contains("Password"))
        //    {
        //        password = headers.GetValues("Password").First();
        //    }
        //    try
        //    {

        //        auth = authenticateUsers(username, password);

        //    }
        //    catch (Exception ex)
        //    {
        //        auth = 0;
        //    }
        //    var ip = GetIP();

        //    //string req = JsonConvert.SerializeObject(name);


        //    string req = JsonConvert.SerializeObject(account);
        //    long logID = logRequest(ip, req);
        //    //check if call is from permitted ip
        //    // var permittedip = ConfigurationManager.AppSettings["permittedip"];
        //    string output = "";
        //    var rs = "";
        //    bool permittedip = CheckPermittedIP(ip);
        //    if (auth > 0 && (permittedip))

        //    {
        //        NewAccountOpeningRequest rx = new NewAccountOpeningRequest();
        //        rx.firstname = account.firstname;
        //        rx.secondname = "";
        //        rx.lastname = account.lastname;
        //        rx.sex = account.sex;
        //        rx.address = account.address;
        //        rx.dob = "2000-01-01";
        //        rx.telephone = account.telephone;
        //        rx.accountName = account.accountName;
        //        rx.branchcode = account.branchcode.ToString();
        //        rx.curencycode = account.curencycode.ToString();
        //        rx.glcode = account.glcode;
        //        rx.cif = account.cif;
        //        rx.title = account.title.ToString();
        //        rx.idtype = account.idtype.ToString();
        //        rx.idno = account.idno;
        //        rx.marital = account.marital;
        //        rx.division = "22";
        //        rx.dept = "223";
        //        rx.ecosector = "23";
        //        rx.addref = account.addref;
        //        rx.Image = account.Image;
        //        rx.SignatureImage = account.SignatureImage;

        //        var acctNa = "";
        //        var cifNo = "";
        //        var acctNo = "";
        //        var msg = "";
        //        var rss = JsonConvert.SerializeObject(rx);
        //        var content = new StringContent(rss, Encoding.UTF8, "text/json");
        //        var client = new HttpClient();

        //        HttpResponseMessage result = null;
        //        var url = ConfigurationManager.AppSettings["newaccountopeningurl"];
        //        result = client.PostAsync(url, content).Result;

        //        var rxx = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
        //        var rp = JsonConvert.DeserializeObject<NewAccountOpeningResponse>(rxx.ToString());

        //        cifNo = rp.cif;
        //        acctNo = rp.accountNo;
        //        msg = rp.responseMessage;


        //        //update the response
        //        //var doc2 = new XmlDocument();
        //        //doc2.Load(result.Content.ReadAsStreamAsync().Result);
        //        //var rr = doc2.DocumentElement.OuterXml;

        //        //XmlDocument xml = new XmlDocument();
        //        //var newrr = WebUtility.HtmlDecode(rr);
        //        //xml.LoadXml(newrr);

        //        //var acctNa = "";
        //        //var cifNo = "";
        //        //var acctNo = "";
        //        //var msg = "";
        //        //XmlDocument xmlx = new XmlDocument();
        //        //xmlx.LoadXml(rr);

        //        //XmlNodeList xnListx = xml.SelectNodes("AccountOpeningResponse");

        //        //foreach (XmlNode xn in xnListx)
        //        //{
        //        //    // cutebankerno = xn["cutebankerno"].InnerText;
        //        //    //oficode = xn["oficode"].InnerText;
        //        //    cifNo = xn["CIF"].InnerText;
        //        //    acctNo = xn["AccountNumber"].InnerText;
        //        //    msg = xn["Message"].InnerText;

        //        //}



        //        //XmlNodeList xnList = xml.SelectNodes("AccountOpeningResponse");
        //        //"<AccountOpeningResponse><CIF>13001037</CIF><AccountNumber>0004206011</AccountNumber><Message>Account Successfully Created</Message></AccountOpeningResponse>"
        //        AccountOpeningResponse ree = new AccountOpeningResponse();

        //        //XmlDocument xmlx = new XmlDocument();
        //        //xmlx.LoadXml(newrr);
        //        //XmlNode parentNode = xmlx.GetElementsByTagName("AccountOpeningResponse").Item(0);
        //        //upload image
        //        var bytes = Convert.FromBase64String(account.Image);
        //        var bytes2 = Convert.FromBase64String(account.SignatureImage);
        //        // string filedir = Path.Combine(Directory.GetCurrentDirectory(), "~/Uploads");
        //        // Debug.WriteLine(filedir);
        //        //Debug.WriteLine(Directory.Exists(filedir));
        //        string filedir = System.Web.Hosting.HostingEnvironment.MapPath("~/Uploads/");

        //        if (!Directory.Exists(filedir))
        //        { //check if the folder exists;
        //            Directory.CreateDirectory(filedir);
        //        }
        //        string fileName = "Passport" + acctNo + ".jpg";
        //        string fileName2 = "Signature" + acctNo + ".jpg";
        //        string file = Path.Combine(filedir, fileName);
        //        string file2 = Path.Combine(filedir, fileName2);
        //        if (bytes.Length > 0)
        //        {
        //            using (var stream = new FileStream(file, FileMode.Create))
        //            {
        //                stream.Write(bytes, 0, bytes.Length);
        //                stream.Flush();
        //            }
        //        }
        //        if (bytes2.Length > 0)
        //        {
        //            using (var stream = new FileStream(file2, FileMode.Create))
        //            {
        //                stream.Write(bytes2, 0, bytes2.Length);
        //                stream.Flush();
        //            }
        //        }



        //        var httpRequest = HttpContext.Current.Request;


        //        //updateRequest(logID, newrr);
        //        updateRequest(logID, rxx);

        //        //foreach (XmlNode xn in xnList)
        //        //{


        //        //}

        //        ree.accountNo = acctNo;

        //        ree.cif = cifNo;
        //        //ree.accountNo = xn["AccountNumber"].InnerText;
        //        //ree.cif = xn["CIF"].InnerText;
        //        ree.responseCode = "00";
        //        ree.responseMessage = msg;
        //        string r2 = JsonConvert.SerializeObject(ree);
        //        updateRequest(logID, r2);
        //        output = JsonConvert.SerializeObject(ree);

        //        //if bvn was provided, do the linking automatically
        //        if (!string.IsNullOrEmpty(account.bvn))
        //        {
        //            internalbvnvalidation r = InternalBVNValidation(account.bvn);
        //            var bvnfirstname = r.firstname;
        //            var bvnmiddlename = r.middlename;
        //            var bvnlastname = r.lastname;

        //            //compare details
        //            if ((account.firstname.ToLower() == bvnfirstname.ToLower()) && (account.lastname.ToLower() == bvnlastname.ToLower()))
        //            {
        //                //go for linking
        //                try
        //                {
        //                    linkbvnresponse re2 = LinkBVNByProxy(acctNo, account.bvn, cifNo);
        //                    //resp.responseCode = re.responseCode;
        //                    //resp.responseMessage = re.responseMessage;
        //                }
        //                catch (Exception ex)
        //                {
        //                    var err2 = new LogUtility.Error()
        //                    {
        //                        ErrorDescription = ex.Message + ex.InnerException,
        //                        ErrorTime = DateTime.Now,
        //                        ModulePointer = "Error Linking BVN on account creation",
        //                        StackTrace = ex.StackTrace
        //                    };
        //                    LogUtility.ActivityLogger.WriteErrorLog(err2);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //put account on PND
        //            ActivatePND(acctNo);
        //        }

        //        //HttpResponseMessage res = Request.CreateResponse(HttpStatusCode.OK, newrr.ToString());
        //        //return res;
        //        return new HttpResponseMessage()
        //        {
        //            Content = new StringContent(r2.ToString(), System.Text.Encoding.UTF8, "application/json")
        //        };
        //    }
        //    else
        //    {
        //        responseclass resp = new responseclass();
        //        resp.responseCode = "99";
        //        resp.responseMessage = "Invalid Username or Password";

        //        string r2 = JsonConvert.SerializeObject(resp);
        //        updateRequest(logID, r2);
        //        output = JsonConvert.SerializeObject(resp);

        //        XmlDocument xml3 = new XmlDocument();
        //        XmlElement root = xml3.CreateElement("AccountOpeningResponse");
        //        xml3.AppendChild(root);

        //        XmlElement ResponseCode = xml3.CreateElement("ResponseCode");
        //        XmlElement ResponseMessage = xml3.CreateElement("lastname");
        //        ResponseCode.InnerText = "99";
        //        ResponseMessage.InnerText = "Invalid Username and Password";

        //        root.AppendChild(ResponseCode);
        //        root.AppendChild(ResponseMessage);

        //        var rss = xml3.OuterXml;

        //        HttpResponseMessage res = Request.CreateResponse(HttpStatusCode.OK, rss.ToString());
        //        return res;
        //    }
        //}

        public async void ActivatePND(string accountNo)
        {
            //BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();
            //var ps = bs.PNDWithReason(accountNo, "ACC", req.Reason);

            //var client = new HttpClient();
            //var pndurl = ConfigurationManager.AppSettings["pndurl"].ToString();
            //var activatepndurl = pndurl + accountNo + "/91";
            //HttpResponseMessage resp = await client.GetAsync(activatepndurl);
        }
        //[HttpPost]
        //[Route("api/CustomerAPI/AccountOpening")]
        //public HttpResponseMessage AccountOpening(HttpRequestMessage req)
        //{
        //    //var username = "";
        //    //var password = "";
        //    //int auth = 0;

        //    //var re = Request;
        //    //var headers = re.Headers;

        //    //if (headers.Contains("Username"))
        //    //{
        //    //    username = headers.GetValues("Username").First();
        //    //}
        //    //if (headers.Contains("Password"))
        //    //{
        //    //    password = headers.GetValues("Password").First();
        //    //}
        //    //try
        //    //{
        //    //    auth = authenticateUsers(username, password);
        //    //    //auth = 1;
        //    //}
        //    //catch(Exception ex)
        //    //{
        //    //    auth = 0;
        //    //}
        //    //if (auth > 0)
        //    //{ 
        //    try
        //    {
        //        //var cont = req.Content;
        //        //var req1 = cont.ReadAsStringAsync().Result;

        //        var doc = new XmlDocument();
        //        doc.Load(req.Content.ReadAsStreamAsync().Result);
        //        var r = doc.DocumentElement.OuterXml;



        //        //log request


        //        // var r = GetString(req);
        //        //long requestID = LogRequest(r);
        //        //long requestID = 1000;
        //        XmlDocument xml = new XmlDocument();
        //        xml.LoadXml(r);

        //        XmlNodeList xnList = xml.SelectNodes("AccountOpeningRequest");

        //        var firstname = "";
        //        var lastname = "";
        //        var sex = "";
        //        var address = "";
        //        var telephone = "";
        //        var accountname = "";
        //        var tellerid = "231";
        //        var cif = "";
        //        var glcode = "";
        //        var countrycode = "1";
        //        var currencycode = "";
        //        var ciftype = "2";
        //        var cifidtype = "";
        //        var compcode = "1";
        //        var accountstatus = "A";
        //        var branchcode = "";
        //        var residence = "1";
        //        var civilcode = "";
        //        var createdby = "API";
        //        var dept = "223";
        //        var division = "22";
        //        var ecosector = "1";
        //        var idno = "";
        //        var marital = "";
        //        var addref = "";
        //        var cutebankerno = "5555";
        //        var oficode = "999";





        //        foreach (XmlNode xn in xnList)
        //        {
        //            // cutebankerno = xn["cutebankerno"].InnerText;
        //            //oficode = xn["oficode"].InnerText;
        //            firstname = xn["firstname"].InnerText;
        //            lastname = xn["lastname"].InnerText;
        //            sex = xn["sex"].InnerText;
        //            address = xn["address"].InnerText;
        //            telephone = xn["telephone"].InnerText;
        //            accountname = xn["accountname"].InnerText;
        //            // tellerid = xn["tellerid"].InnerText;
        //            cif = xn["cif"].InnerText;
        //            glcode = xn["glcode"].InnerText;
        //            //countrycode = xn["countrycode"].InnerText;
        //            currencycode = xn["currencycode"].InnerText;
        //            // ciftype = xn["ciftype"].InnerText;
        //            cifidtype = xn["idtype"].InnerText;
        //            //compcode = xn["compcode"].InnerText;
        //            // accountstatus = xn["accountstatus"].InnerText;
        //            branchcode = xn["branchcode"].InnerText;
        //            //residence = xn["resident"].InnerText;
        //            civilcode = xn["title"].InnerText;
        //            //createdby = xn["createdby"].InnerText;
        //            //dept = xn["dept"].InnerText;
        //            //division = xn["division"].InnerText;
        //            //ecosector = xn["ecosector"].InnerText;
        //            idno = xn["idno"].InnerText;
        //            marital = xn["marital"].InnerText;
        //            addref = xn["addref"].InnerText;




        //        }
        //        //authenticate user calls



        //        var rs = "";


        //        Decimal AL_CHANNEL_ID = 2;
        //        string AS_USER_ID = "JZNIP01";
        //        string AS_MACHINE_NAME = "172.13.21.63";
        //        Decimal AL_API_CODE = 105;
        //        DateTime ADT_DATE =DateTime.Now;
        //        Decimal AL_COMP_CODE = Convert.ToDecimal(compcode);
        //        Decimal AL_BRANCH_CODE = Convert.ToDecimal(branchcode);
        //        string AS_CURRENCY_CODE = currencycode;
        //        Decimal AL_GL_CODE = Convert.ToDecimal(glcode);
        //        Decimal? AL_CIF_SUB_NO = (Decimal?)null;
        //        if (string.IsNullOrEmpty(cif))
        //        {
        //            //call the generate cif method
        //            var newcif = CreateCIF(compcode, branchcode, currencycode, lastname, firstname, accountname, idno, countrycode,
        //               civilcode, createdby, dept, division, ecosector, telephone, address, marital, sex, addref);

        //            AL_CIF_SUB_NO = Convert.ToDecimal(newcif);
        //        }
        //        else
        //        {
        //            AL_CIF_SUB_NO = Convert.ToDecimal(cif);
        //        }

        //        string AS_TELLER_ID = tellerid;
        //        string AS_RENEW = "N";
        //        Decimal? AL_TRF_BR = (Decimal?)null;
        //        Decimal? AL_TRF_CY = (Decimal?)null;
        //        Decimal? AL_TRF_GL = (Decimal?)null;
        //        Decimal? AL_TRF_CIF = (Decimal?)null;
        //        Decimal? AL_TRF_SL = (Decimal?)null;
        //        string AS_TRF_ADD_REF = "YES";
        //        string AS_PFT_POST_TO = "1";
        //        Decimal? AL_PROFIT_BR = (Decimal?)null;
        //        Decimal? AL_PROFIT_CY = (Decimal?)null;
        //        Decimal? AL_PROFIT_GL = (Decimal?)null;
        //        Decimal? AL_PROFIT_CIF = (Decimal?)null;
        //        Decimal? AL_PROFIT_SL = (Decimal?)null;
        //        string AS_PROFIT_ADD_REF = "";
        //        Decimal? AL_MATURITY_GL = (Decimal?)null;
        //        string AS_EXT_TRF = "";
        //        Decimal? AL_OFF_BR = (Decimal?)null;
        //        Decimal? AL_OFF_CY = (Decimal?)null;
        //        Decimal? AL_OFF_GL = (Decimal?)null;
        //        Decimal? AL_OFF_CIF = (Decimal?)null;
        //        Decimal? AL_OFF_SL = (Decimal?)null;
        //        string AS_OFF_ADD_REF = "";
        //        Decimal? AL_TRANSFER_AM = (Decimal?)null;
        //        Decimal? AL_DEBIT_BRANCH = (Decimal?)null;
        //        Decimal? AL_DEBIT_CURRENCY = (Decimal?)null;
        //        Decimal? AL_DEBIT_GL_CODE = (Decimal?)null;
        //        Decimal? AL_DEBIT_CIF_SUB_NO = (Decimal?)null;
        //        Decimal? AL_DEBIT_SL = (Decimal?)null;
        //        string AS_DEBIT_ADD_REF = "";
        //        string AS_REMARKS = "";
        //        Decimal AL_DIV_CODE = Convert.ToDecimal(division);
        //        Decimal AL_DEPT_CODE = Convert.ToDecimal(dept);
        //        string AS_REFERENCE = "";
        //        Decimal AL_POS = 0;
        //        string AS_INST1 = "";
        //        string AS_INST2 = "";
        //        Decimal AL_TRXTYPE = 0;
        //        string AS_STATUS = "I";
        //        string AS_CREATED_TRX = "";


        //        ObjectParameter OS_ADD_REF = new ObjectParameter("OS_ADD_REF", typeof(string));
        //        ObjectParameter OL_TRS_NO = new ObjectParameter("OL_TRS_NO", typeof(Decimal));
        //        ObjectParameter OL_POINT_RATE = new ObjectParameter("OL_POINT_RATE", typeof(Decimal));
        //        ObjectParameter ODT_MATE_DATE = new ObjectParameter("ODT_MATE_DATE", typeof(DateTime));
        //        ObjectParameter OL_NEW_BAL = new ObjectParameter("OL_NEW_BAL", typeof(Decimal));
        //        ObjectParameter OS_ACC_NAME = new ObjectParameter("OS_ACC_NAME", typeof(string));
        //        ObjectParameter OL_ERROR_CODE = new ObjectParameter("OL_ERROR_CODE", typeof(Decimal));
        //        ObjectParameter OS_ERROR_DESC = new ObjectParameter("OS_ERROR_DESC", typeof(string));








        //        //attempt posting
        //        int response = 999;
        //        try
        //        {
        //            // adt_date=DateTime.Now;
        //            //int response = im.P_API_ACCOUNT_CREATION(al_channel_id=2, as_user_id="UA08170", as_machine_name='JAIZ', al_api_code=100, adt_date, al_comp_code=0001, al_branch_code=2, as_currency_code="566", al_gl_code=103114, al_cif_sub_no=10928374, as_teller_id="UA08170", as_renew="Y", "", "", "", "", "", "", as_pft_post_to="4", "", "", "", "", "", "", "", "", "", "", "", "", "", "", al_transfer_am=0, 1, 1, 1, 1, 1, "add ref", "remarks", al_div_code, al_dept_code, as_reference, al_position=1, "test","test",300,"O","0","out",2,2,"2018-06-30",0,"AJAGBE UTH","09","out");
        //            //response=im.P_API_ACCOUNT_CREATION ( AL_CHANNEL_ID, AS_USER_ID, AS_MACHINE_NAME, AL_API_CODE, ADT_DATE, AL_COMP_CODE, AL_BRANCH_CODE, AS_CURRENCY_CODE, AL_GL_CODE, AL_CIF_SUB_NO, AS_TELLER_ID, AS_RENEW, AL_TRF_BR, AL_TRF_CY, AL_TRF_GL, AL_TRF_CIF, AL_TRF_SL, AS_TRF_ADD_REF, AS_PFT_POST_TO, AL_PROFIT_BR, AL_PROFIT_CY, AL_PROFIT_GL, AL_PROFIT_CIF, AL_PROFIT_SL, AS_PROFIT_ADD_REF, AL_MATURITY_GL, AS_EXT_TRF, AL_OFF_BR, AL_OFF_CY, AL_OFF_GL, AL_OFF_CIF, AL_OFF_SL, AS_OFF_ADD_REF, AL_TRANSFER_AM, AL_DEBIT_BRANCH, AL_DEBIT_CURRENCY, AL_DEBIT_GL_CODE, AL_DEBIT_CIF_SUB_NO, AL_DEBIT_SL, AS_DEBIT_ADD_REF, AS_REMARKS, AL_DIV_CODE, AL_DEPT_CODE, AS_REFERENCE, AL_POS, AS_INST1, AS_INST2, AL_TRXTYPE, AS_STATUS, AS_CREATED_TRX, OS_ADD_REF, OL_TRS_NO, OL_POINT_RATE, ODT_MATE_DATE, OL_NEW_BAL, OS_ACC_NAME, OL_ERROR_CODE, OS_ERROR_DESC);

        //            /**** BEGIN PROCEDURE ****/
        //            OracleConnection conn = DBUtils.GetDBConnection();
        //            conn.Open();
        //            try
        //            {
        //                ////Nullable<System.DateTime> aDT_DATE;
        //                //// Create a Command object to call Get_Employee_Info procedure.

        //                //OracleCommand cmd = new OracleCommand("P_API_ACCOUNT_CREATION", conn);

        //                //// Command Type is StoredProcedure
        //                //cmd.CommandType = CommandType.StoredProcedure;

        //                //// Add parameter @p_Emp_Id and set value = 100
        //                //cmd.Parameters.Add("@AL_CHANNEL_ID", OracleDbType.Decimal).Value = AL_CHANNEL_ID;
        //                //cmd.Parameters.Add("@AS_USER_ID", OracleDbType.NVarchar2).Value = AS_USER_ID;
        //                //cmd.Parameters.Add("@AS_MACHINE_NAME", OracleDbType.NVarchar2).Value = AS_MACHINE_NAME;
        //                //cmd.Parameters.Add("@AL_API_CODE", OracleDbType.Decimal).Value = AL_API_CODE;
        //                //cmd.Parameters.Add("@ADT_DATE", OracleDbType.Date).Value = ADT_DATE;
        //                //cmd.Parameters.Add("@AL_COMP_CODE", OracleDbType.Decimal).Value = AL_COMP_CODE;
        //                //cmd.Parameters.Add("@AL_BRANCH_CODE", OracleDbType.Decimal).Value = AL_BRANCH_CODE;
        //                //cmd.Parameters.Add("@AS_CURRENCY_CODE", OracleDbType.NVarchar2).Value = AS_CURRENCY_CODE;
        //                //cmd.Parameters.Add("@AL_GL_CODE", OracleDbType.Decimal).Value = AL_GL_CODE;
        //                //cmd.Parameters.Add("@AL_CIF_SUB_NO", OracleDbType.Decimal).Value = AL_CIF_SUB_NO;
        //                //cmd.Parameters.Add("@AS_TELLER_ID", OracleDbType.NVarchar2).Value = AS_TELLER_ID;
        //                //cmd.Parameters.Add("@AS_RENEW", OracleDbType.NVarchar2).Value = AS_RENEW;
        //                //cmd.Parameters.Add("@AL_TRF_BR", OracleDbType.Decimal).Value = AL_TRF_BR;
        //                //cmd.Parameters.Add("@AL_TRF_CY", OracleDbType.Decimal).Value = AL_TRF_CY;
        //                //cmd.Parameters.Add("@AL_TRF_GL", OracleDbType.Decimal).Value = AL_TRF_GL;
        //                //cmd.Parameters.Add("@AL_TRF_CIF", OracleDbType.Decimal).Value = AL_TRF_CIF;
        //                //cmd.Parameters.Add("@AL_TRF_SL", OracleDbType.Decimal).Value = AL_TRF_SL;
        //                //cmd.Parameters.Add("@AS_TRF_ADD_REF", OracleDbType.NVarchar2).Value = AS_TRF_ADD_REF;
        //                //cmd.Parameters.Add("@AS_PFT_POST_TO", OracleDbType.NVarchar2).Value = AS_PFT_POST_TO;
        //                //cmd.Parameters.Add("@AL_PROFIT_BR", OracleDbType.Decimal).Value = AL_PROFIT_BR;
        //                //cmd.Parameters.Add("@AL_PROFIT_CY", OracleDbType.Decimal).Value = AL_PROFIT_CY;
        //                //cmd.Parameters.Add("@AL_PROFIT_GL", OracleDbType.Decimal).Value = AL_PROFIT_GL;
        //                //cmd.Parameters.Add("@AL_PROFIT_CIF", OracleDbType.Decimal).Value = AL_PROFIT_CIF;
        //                //cmd.Parameters.Add("@AL_PROFIT_SL", OracleDbType.Decimal).Value = AL_PROFIT_SL;
        //                //cmd.Parameters.Add("@AS_PROFIT_ADD_REF", OracleDbType.NVarchar2).Value = AS_PROFIT_ADD_REF;
        //                //cmd.Parameters.Add("@AL_MATURITY_GL", OracleDbType.Decimal).Value = AL_MATURITY_GL;
        //                //cmd.Parameters.Add("@AS_EXT_TRF", OracleDbType.NVarchar2).Value = AS_EXT_TRF;
        //                //cmd.Parameters.Add("@AL_OFF_BR", OracleDbType.Decimal).Value = AL_OFF_BR;
        //                //cmd.Parameters.Add("@AL_OFF_CY", OracleDbType.Decimal).Value = AL_OFF_CY;
        //                //cmd.Parameters.Add("@AL_OFF_GL", OracleDbType.Decimal).Value = AL_OFF_GL;
        //                //cmd.Parameters.Add("@AL_OFF_CIF", OracleDbType.Decimal).Value = AL_OFF_CIF;
        //                //cmd.Parameters.Add("@AL_OFF_SL", OracleDbType.Decimal).Value = AL_OFF_SL;
        //                //cmd.Parameters.Add("@AS_OFF_ADD_REF", OracleDbType.NVarchar2).Value = AS_OFF_ADD_REF;
        //                //cmd.Parameters.Add("@AL_TRANSFER_AM", OracleDbType.Decimal).Value = AL_TRANSFER_AM;
        //                //cmd.Parameters.Add("@AL_DEBIT_BRANCH", OracleDbType.Decimal).Value = AL_DEBIT_BRANCH;
        //                //cmd.Parameters.Add("@AL_DEBIT_CURRENCY", OracleDbType.Decimal).Value = AL_DEBIT_CURRENCY;
        //                //cmd.Parameters.Add("@AL_DEBIT_GL_CODE", OracleDbType.Decimal).Value = AL_DEBIT_GL_CODE;
        //                //cmd.Parameters.Add("@AL_DEBIT_CIF_SUB_NO", OracleDbType.Decimal).Value = AL_DEBIT_CIF_SUB_NO;
        //                //cmd.Parameters.Add("@AL_DEBIT_SL", OracleDbType.Decimal).Value = AL_DEBIT_SL;
        //                //cmd.Parameters.Add("@AS_DEBIT_ADD_REF", OracleDbType.NVarchar2).Value = AS_DEBIT_ADD_REF;
        //                //cmd.Parameters.Add("@AS_REMARKS", OracleDbType.NVarchar2).Value = AS_REMARKS;
        //                //cmd.Parameters.Add("@AL_DIV_CODE", OracleDbType.Decimal).Value = AL_DIV_CODE;
        //                //cmd.Parameters.Add("@AL_DEPT_CODE", OracleDbType.Decimal).Value = AL_DEPT_CODE;
        //                //cmd.Parameters.Add("@AS_REFERENCE", OracleDbType.NVarchar2).Value = AS_REFERENCE;
        //                //cmd.Parameters.Add("@AL_POS", OracleDbType.Decimal).Value = AL_POS;
        //                //cmd.Parameters.Add("@AS_INST1", OracleDbType.NVarchar2).Value = AS_INST1;
        //                //cmd.Parameters.Add("@AS_INST2", OracleDbType.NVarchar2).Value = AS_INST2;
        //                //cmd.Parameters.Add("@AL_TRXTYPE", OracleDbType.Decimal).Value = AL_TRXTYPE;
        //                //cmd.Parameters.Add("@AS_STATUS", OracleDbType.NVarchar2).Value = AS_STATUS;
        //                //cmd.Parameters.Add("@AS_CREATED_TRX", OracleDbType.Decimal).Value = AS_CREATED_TRX;




        //                //cmd.Parameters.Add(new OracleParameter("@OS_ACC_NAME", OracleDbType.Varchar2, 50));
        //                //cmd.Parameters.Add(new OracleParameter("@OL_ERROR_CODE", OracleDbType.Varchar2, 50));
        //                //cmd.Parameters.Add(new OracleParameter("@OS_ERROR_DESC", OracleDbType.Varchar2, 50));
        //                //cmd.Parameters.Add(new OracleParameter("@OS_ADD_REF", OracleDbType.Varchar2)); 

        //                //// Register parameter @v_Emp_No is OUTPUT.
        //                //cmd.Parameters["@OS_ACC_NAME"].Direction = ParameterDirection.Output;
        //                //cmd.Parameters["@OL_ERROR_CODE"].Direction = ParameterDirection.Output;
        //                //cmd.Parameters["@OS_ERROR_DESC"].Direction = ParameterDirection.Output;
        //                //cmd.Parameters["@OS_ADD_REF"].Direction = ParameterDirection.Output;

        //                //// Execute procedure.

        //                //    cmd.ExecuteNonQuery();



        //                //// Get output values.


        //                //string AccountName = cmd.Parameters["@OS_ACC_NAME"].Value.ToString();
        //                //string errorCode = cmd.Parameters["@OL_ERROR_CODE"].Value.ToString();
        //                //string error = cmd.Parameters["@OS_ERROR_DESC"].Value.ToString();
        //                //string AcctNumber = cmd.Parameters["@OS_ADD_REF"].Value.ToString();




        //                /**** END ****/
        //                using (ImalEntities im = new ImalEntities())
        //                {
        //                    response = im.P_API_ACCOUNT_CREATION(
        //                        AL_CHANNEL_ID,
        //                        AS_USER_ID,
        //                        AS_MACHINE_NAME,
        //                        AL_API_CODE,
        //                        ADT_DATE,
        //                        AL_COMP_CODE,
        //                        AL_BRANCH_CODE,
        //                        AS_CURRENCY_CODE,
        //                        AL_GL_CODE,
        //                        AL_CIF_SUB_NO,
        //                        AS_TELLER_ID,
        //                        AS_RENEW,
        //                        AL_TRF_BR,
        //                        AL_TRF_CY,
        //                        AL_TRF_GL,
        //                        AL_TRF_CIF,
        //                        AL_TRF_SL,
        //                        AS_TRF_ADD_REF,
        //                        AS_PFT_POST_TO,
        //                        AL_PROFIT_BR,
        //                        AL_PROFIT_CY,
        //                        AL_PROFIT_GL,
        //                        AL_PROFIT_CIF,
        //                        AL_PROFIT_SL,
        //                        AS_PROFIT_ADD_REF,
        //                        AL_MATURITY_GL,
        //                        AS_EXT_TRF,
        //                        AL_OFF_BR,
        //                        AL_OFF_CY,
        //                        AL_OFF_GL,
        //                        AL_OFF_CIF,
        //                        AL_OFF_SL,
        //                        AS_OFF_ADD_REF,
        //                        AL_TRANSFER_AM,
        //                        AL_DEBIT_BRANCH,
        //                        AL_DEBIT_CURRENCY,
        //                        AL_DEBIT_GL_CODE,
        //                        AL_DEBIT_CIF_SUB_NO,
        //                        AL_DEBIT_SL,
        //                        AS_DEBIT_ADD_REF,
        //                        AS_REMARKS,
        //                        AL_DIV_CODE,
        //                        AL_DEPT_CODE,
        //                        AS_REFERENCE,
        //                        AL_POS,
        //                        AS_INST1,
        //                        AS_INST2,
        //                        AL_TRXTYPE,
        //                        AS_STATUS,
        //                        AS_CREATED_TRX,
        //                        OS_ADD_REF,
        //                        OL_TRS_NO,
        //                        OL_POINT_RATE,
        //                        ODT_MATE_DATE,
        //                        OL_NEW_BAL,
        //                        OS_ACC_NAME,
        //                        OL_ERROR_CODE,
        //                        OS_ERROR_DESC);

        //                    // ObjectParameter Output = new ObjectParameter("ID", typeof(Int32));

        //                    var error = OS_ERROR_DESC.Value;
        //                    XmlDocument xml2 = new XmlDocument();
        //                    XmlElement root = xml2.CreateElement("AccountOpeningResponse");
        //                    xml2.AppendChild(root);

        //                    //XmlElement ErrorCode = xml2.CreateElement("ErrorCode");
        //                    //XmlElement ErrorDesc = xml2.CreateElement("ErrorDesc");
        //                    XmlElement ResponseCode = xml2.CreateElement("ResponseCode");
        //                    XmlElement CIF = xml2.CreateElement("CIF");
        //                    XmlElement AccountNumber = xml2.CreateElement("AccountNumber");
        //                    XmlElement Message = xml2.CreateElement("Message");


        //                    CIF.InnerText = AL_CIF_SUB_NO.Value.ToString();
        //                    ResponseCode.InnerText = "00";
        //                    //AccountNumber.InnerText = OS_ADD_REF.Value.ToString();
        //                    AccountNumber.InnerText = getAccountNo(AL_CIF_SUB_NO.Value.ToString());
        //                    //Message.InnerText = OS_ERROR_DESC.Value.ToString();
        //                    Message.InnerText = "Account Successfully Created";

        //                    root.AppendChild(CIF);
        //                    root.AppendChild(AccountNumber);
        //                    root.AppendChild(Message);


        //                    //if (AL_CIF_SUB_NO.Value.ToString() !=null)
        //                    //{
        //                    //    //log into the accountNo table
        //                    //    //    int ll=logAccountNo(requestID, DateTime.Now, cutebankerno, oficode, getAccountNo(AL_CIF_SUB_NO.Value.ToString()), AL_CIF_SUB_NO.Value.ToString());
        //                    //    //}

        //                    //    rs = xml2.OuterXml;


        //                    //    //send welcome sms
        //                    //    var acctNo = getAccountNo(AL_CIF_SUB_NO.Value.ToString());
        //                    //    string msg = "Welcome to Jaiz Bank Plc, Your account no is: " + acctNo + ".For more enquiries pls call +2347080635500,+2347080635555";




        //                    //}

        //                    //XmlDocument xml2 = new XmlDocument();
        //                    //XmlElement root = xml2.CreateElement("AccountOpeningResponse");
        //                    //xml2.AppendChild(root);

        //                    ////XmlElement ErrorCode = xml2.CreateElement("ErrorCode");
        //                    ////XmlElement ErrorDesc = xml2.CreateElement("ErrorDesc");
        //                    //XmlElement ResponseCode = xml2.CreateElement("ResponseCode");
        //                    //XmlElement CIF = xml2.CreateElement("CIF");
        //                    //XmlElement AccountNumber = xml2.CreateElement("AccountNumber");
        //                    //XmlElement Message = xml2.CreateElement("Message");


        //                    //CIF.InnerText = AL_CIF_SUB_NO.Value.ToString();
        //                    //ResponseCode.InnerText = "00";
        //                    //AccountNumber.InnerText = AcctNumber;
        //                    ////AccountNumber.InnerText = getAccountNo(AL_CIF_SUB_NO.Value.ToString());
        //                    ////Message.InnerText = OS_ERROR_DESC.Value.ToString();
        //                    //Message.InnerText = "Account Successfully Created";

        //                    //root.AppendChild(CIF);
        //                    //root.AppendChild(AccountNumber);
        //                    //root.AppendChild(Message);




        //                    //rs = xml2.OuterXml;

        //                    //// updateLog(requestID, rs);
        //                    ////send welcome sms
        //                    //var acctNo = getAccountNo(AL_CIF_SUB_NO.Value.ToString());
        //                    //string msg = "Welcome to Jaiz Bank Plc, Your account no is: " + acctNo + ".For more enquiries pls call +2347080635500,+2347080635555";

        //                    //var smsRespCode = SendSMS(telephone, msg);



        //                    //rs = "";
        //                }
        //            }
        //            catch (Exception ex)
        //            {


        //                int linenum = 0;
        //                linenum = Convert.ToInt32(ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
        //                var err2 = new LogUtility.Error()
        //                {


        //                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //                    ErrorTime = DateTime.Now,
        //                    ModulePointer = "Account Opening- connecting to imal, stored procedure",
        //                    StackTrace = ex.StackTrace
        //                };
        //                LogUtility.ActivityLogger.WriteErrorLog(err2);
        //            }
        //            // updateLog(requestID, rs.ToString());

        //            HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, rs.ToString());
        //            return resp;
        //        }
        //        catch (Exception ex)
        //        {
        //            int linenum = 0;
        //            linenum = Convert.ToInt32(ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
        //            var err2 = new LogUtility.Error()
        //            {

        //                ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //                ErrorTime = DateTime.Now,
        //                ModulePointer = "Account Opening- connecting to imal",
        //                StackTrace = ex.StackTrace
        //            };
        //            LogUtility.ActivityLogger.WriteErrorLog(err2);


        //            HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, ex.Message);
        //            return resp;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        int linenum = 0;
        //        linenum = Convert.ToInt32(ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
        //        var err2 = new LogUtility.Error()
        //        {
        //            ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //            ErrorTime = DateTime.Now,
        //            ModulePointer = "Account Opening- Error",
        //            StackTrace = ex.StackTrace
        //        };
        //        LogUtility.ActivityLogger.WriteErrorLog(err2);
        //        HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, ex.Message);
        //        return resp;
        //    }
        //}
        
        public string getAccountNo(string cif)
        {
            using (ImalEntities im = new ImalEntities())
            {
                int c = Convert.ToInt32(cif);
                var d = im.AMFs.Where(a => a.CIF_SUB_NO == c).OrderByDescending(a => a.DATE_OPND).First();
                return d.ADDITIONAL_REFERENCE;
            }
            

        }

        [HttpPost]
        [Route("api/AccountOpening/CreateCIF")]
        public string CreateCIF(string compcode, string branchcode, string currencycode, string lastname, string firstname, string accountname, string idno, string nationcode, string civilcode, string createdby, string dept, string division, string ecosector, string phone, string address, string marital, string sex, string addref)
        {
            //using (ImalEntities2 imal = new ImalEntities2())
            //{
            var cifNo = "";
            Decimal AL_CHANNEL_ID = 2;
            string AS_USER_ID = "JZNIP01";
            string AS_MACHINE_NAME = "172.13.21.63";
            Decimal AL_API_CODE = 105;
            DateTime? ADT_DATE = null;
            Decimal AL_COMP_CODE = 1;
            Decimal AL_BRANCH_CODE = Convert.ToDecimal(branchcode);


            Decimal? AL_DEBIT_BRANCH = (Decimal?)null;
            Decimal? AL_DEBIT_CURRENCY = (Decimal?)null;
            Decimal? AL_DEBIT_GL_CODE = (Decimal?)null;
            Decimal? AL_DEBIT_CIF_SUB_NO = (Decimal?)null;
            Decimal? AL_DEBIT_SL = (Decimal?)null;

            /**BEGIN OTHER VALUES **/
            Decimal AL_CIF_TYPE = 1;
            Decimal AL_ID_TYPE = 1;
            DateTime? ADT_ESTAB_DATE = null;
            string AS_SHORT_NAME_ENG = firstname;
            string AS_SHORT_NAME_ARAB = firstname;
            string AS_LONG_NAME_ENG = accountname;
            string AS_LONG_NAME_ARAB = accountname;
            string AS_ID_NO = idno;
            string AS_LANGUAGE = "E";
            Decimal AL_NATION_CODE = Convert.ToDecimal(nationcode);
            Decimal AL_COUNTRY_CODE = Convert.ToDecimal(nationcode);
            Decimal AL_PRIORITY_CODE = 1;
            string AS_RESIDENT = "";
            Decimal AL_CIVIL_CODE = Convert.ToDecimal(civilcode);
            string AS_CREATED_BY = createdby;
            Decimal AL_DEPT = Convert.ToDecimal(dept);
            Decimal AL_DIVISION = Convert.ToDecimal(division);
            Decimal AL_ECO_SECTOR = Convert.ToDecimal(ecosector);
            string AS_FIRST_NAME_ENG = firstname;
            string AS_LAST_NAME_ENG = lastname;
            string AS_TEL = phone;
            string AS_FIRST_NAME_ARAB = "";
            string AS_SEC_NAME_ARAB = "";
            string AS_LAST_NAME_ARAB = "";
            string AS_ADDRESS1_ENG = address;
            string AS_ADDRESS2_ENG = "";
            string AS_ADDRESS3_ENG = "";
            string AS_AUTH_ID = "";
            string AS_AUTH_NAME = "";
            string AS_SEC_NAME_ENG = "";
            string AS_REL_OFFICER = "";
            //DateTime ADT_DATE_CREATED;
            //DateTime ADT_DATE_MODIFIED;
            string AS_TYPE = "V";
            string AS_KYC_COMPLETED = "Y";
            string AS_MARITAL_STATUS = marital;
            string AS_PC_IND = "P";
            string AS_POPULATED = "";
            string AS_SHOW_SECRET_NO = "1";
            Decimal? AL_REL_OFF_ID = (Decimal?)null; ;
            Decimal? AL_MONTHLY_SALARY = (Decimal?)null; ;
            Decimal? AL_SUB_ECO_SECTOR = (Decimal?)null;
            string AS_SEXE = sex;
            string AS_ADD_REF = addref;
            string AS_BILL_FLAG = "0";
            string AS_IND = "I";
            Decimal? AL_TRX_TYPE = (Decimal?)null;
            Decimal? AL_CY = (Decimal?)null;
            Decimal? AL_ACC_BR = (Decimal?)null;
            Decimal? AL_ACC_CY = (Decimal?)null;
            Decimal? AL_ACC_GL = (Decimal?)null;
            Decimal? AL_ACC_CIF = (Decimal?)null;
            Decimal? AL_ACC_SL = (Decimal?)null;

            
            DateTime? ADT_ADD_DATE1 = null; DateTime? ADT_ADD_DATE2 = null; DateTime? ADT_ADD_DATE3 = null; DateTime? ADT_ADD_DATE4 = null; DateTime? ADT_ADD_DATE5 = null;

            Decimal? AL_ADD_NUMBER1 = (Decimal?)null; Decimal? AL_ADD_NUMBER2 = (Decimal?)null; Decimal? AL_ADD_NUMBER3 = (Decimal?)null; Decimal? AL_ADD_NUMBER4 = (Decimal?)null; Decimal? AL_ADD_NUMBER5 = (Decimal?)null;

            string AS_ADD_STRING1 = ""; string AS_ADD_STRING2 = ""; string AS_ADD_STRING3 = ""; string AS_ADD_STRING4 = ""; string AS_ADD_STRING5 = ""; string AS_ADD_STRING6 = ""; string AS_ADD_STRING7 = ""; string AS_ADD_STRING8 = ""; string AS_ADD_STRING9 = ""; string AS_ADD_STRING10 = ""; string AS_ADD_STRING11 = ""; string AS_ADD_STRING12 = ""; string AS_ADD_STRING13 = ""; string AS_ADD_STRING14 = ""; string AS_ADD_STRING15 = "";

            DateTime? ADT_DATE_CREATED = null;
            DateTime? ADT_DATE_MODIFIED = null;


            /**END OTHER VALUES **/

            ObjectParameter OL_CIF_NO = new ObjectParameter("OL_CIF_NO", typeof(string));
            ObjectParameter OL_TRS_NO = new ObjectParameter("OL_TRS_NO", typeof(Decimal));
            ObjectParameter OL_POINT_RATE = new ObjectParameter("OL_POINT_RATE", typeof(Decimal));
            ObjectParameter ODT_MATE_DATE = new ObjectParameter("ODT_MATE_DATE", typeof(DateTime));
            ObjectParameter OL_NEW_BAL = new ObjectParameter("OL_NEW_BAL", typeof(Decimal));
            ObjectParameter OS_ACC_NAME = new ObjectParameter("OS_ACC_NAME", typeof(string));
            ObjectParameter OL_ERROR_CODE = new ObjectParameter("OL_ERROR_CODE", typeof(Decimal));
            ObjectParameter OS_ERROR_DESC = new ObjectParameter("OS_ERROR_DESC", typeof(string));

            string AS_STATUS = "A";
            using (ImalEntities im = new ImalEntities())
            {
                try
                {
                    int response = im.P_API_CREATE_CIF(AL_CHANNEL_ID, AS_USER_ID, AS_MACHINE_NAME, AL_API_CODE, AL_COMP_CODE, AL_BRANCH_CODE, AL_CIF_TYPE, AL_ID_TYPE, ADT_ESTAB_DATE, AS_SHORT_NAME_ENG, AS_SHORT_NAME_ARAB, AS_LONG_NAME_ENG, AS_LONG_NAME_ARAB, AS_ID_NO, AS_LANGUAGE, AL_NATION_CODE, AL_COUNTRY_CODE, AL_PRIORITY_CODE, AS_RESIDENT, AL_CIVIL_CODE, AS_CREATED_BY, AL_DEPT, AL_DIVISION, AL_ECO_SECTOR, AS_FIRST_NAME_ENG, AS_LAST_NAME_ENG, AS_TEL, AS_FIRST_NAME_ARAB, AS_SEC_NAME_ARAB, AS_LAST_NAME_ARAB, AS_ADDRESS1_ENG, AS_ADDRESS2_ENG, AS_ADDRESS3_ENG, AS_AUTH_ID, AS_AUTH_NAME, AS_SEC_NAME_ENG, AS_REL_OFFICER, ADT_DATE_CREATED, ADT_DATE_MODIFIED, AS_STATUS, AS_TYPE, AS_KYC_COMPLETED, AS_MARITAL_STATUS, AS_PC_IND, AS_POPULATED, AS_SHOW_SECRET_NO, AL_REL_OFF_ID, AL_MONTHLY_SALARY, AL_SUB_ECO_SECTOR, AS_SEXE, AS_ADD_REF, AS_BILL_FLAG, AS_IND, AL_TRX_TYPE, AL_CY, AL_ACC_BR, AL_ACC_CY, AL_ACC_GL, AL_ACC_CIF, AL_ACC_SL, ADT_ADD_DATE1, ADT_ADD_DATE2, ADT_ADD_DATE3, ADT_ADD_DATE4, ADT_ADD_DATE5, AL_ADD_NUMBER1, AL_ADD_NUMBER2, AL_ADD_NUMBER3, AL_ADD_NUMBER4, AL_ADD_NUMBER5, AS_ADD_STRING1, AS_ADD_STRING2, AS_ADD_STRING3, AS_ADD_STRING4, AS_ADD_STRING5, AS_ADD_STRING6, AS_ADD_STRING7, AS_ADD_STRING8, AS_ADD_STRING9, AS_ADD_STRING10, AS_ADD_STRING11, AS_ADD_STRING12, AS_ADD_STRING13, AS_ADD_STRING14, AS_ADD_STRING15, OL_CIF_NO, OL_ERROR_CODE, OS_ERROR_DESC);
                    
                    cifNo = OL_CIF_NO.Value.ToString();

                    return cifNo;


                }

                catch (Exception ex)
                
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message  + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "CIF Creation Error: Line Number:",
                        StackTrace = ex.StackTrace
                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);

                    cifNo = "";
                    return cifNo;
                }






            }

        }

        [HttpPost]
        [Route("api/CustomerAPI/SendSMS")]
        public HttpResponseMessage SendSMS([FromBody] SMSSendingClass sms)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string output = "";
            DateTime loginTime = DateTime.Now;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);               
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(sms);
          
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                responseclass resp = new responseclass();

                try
                {
                    InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                    InternetBankingAPI.SmsObject O = new InternetBankingAPI.SmsObject();
                    InternetBankingAPI.SmsResponse R = new InternetBankingAPI.SmsResponse();

                    O.MobileNo = sms.phoneNo;
                    O.SmsContent = sms.message;
                    O.SenderId = "INTBK";
                    bool T = true;
                    S.SendSmsViaHelper(O, out R, out T);
                    resp.responseCode = "00";
                    resp.responseMessage = "Message Successfully logged";

                    output = JsonConvert.SerializeObject(resp);

                    logRequestResponse2(ip, req, "SendSMS", loginTime, output);


                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };

                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = ex.Message + Environment.NewLine + ex.InnerException;

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, req, "SendSMS", loginTime, output);
                }
                output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, req, "SendSMS", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        public static string Decrypt(string cipherText, string passPhrase)
        {
            int Keysize = 256;

            int DerivationIterations = 1000;
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        public int authenticateUsers(string username, string password)
        {
            int result = 0;
            var cipher = ConfigurationManager.AppSettings["cipher"];
            string decryptedPassword = Decrypt(password, cipher);
            using (JaizOpenDigitalBankingEntities db=new JaizOpenDigitalBankingEntities())
            {
                result = db.users.Count(a => a.username == username && a.password == decryptedPassword);
                return result;
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/SendEmail")]
        public HttpResponseMessage SendEmail([FromBody] EmailSendingClass email)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
               // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(email);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
           // if (auth > 0 && (permittedip==ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

               
                
                responseclass resp = new responseclass();

                try
                {
                    InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                    InternetBankingAPI.EmailObject E = new InternetBankingAPI.EmailObject();
                    InternetBankingAPI.EmailResponse R = new InternetBankingAPI.EmailResponse();

                    E.EmailAddress = email.to;
                    E.EmailContent = email.message;
                    E.FromAddress = email.from;
                    E.Subject = email.subject;
                    E.SenderId = "INTBK";


                    bool T = true;
                    S.SendEmailViaHelper(E, out R, out T);

                    resp.responseCode = "00";
                    resp.responseMessage = "Message Successfully logged";

                    //update log 
                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Logging Message";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);
                
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/AccountBalance2")]
        public HttpResponseMessage AccountBalance2([FromBody] BalanceEnquiry bal)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(bal);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            // var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            BalanceEnquiryResponse resp = new BalanceEnquiryResponse();
            if (auth > 0 && (permittedip))
            {

                

                try
                {
                    InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();

                    //var custInfo = S.GetCustomerInfoByAccountNo(bal.accountNo);
                    AccountDetailsResponse custInfo = AccountDetails(bal.accountNo);
                    //AccountDetailsResponse custInfo = AccountDetailsByProxy(bal.accountNo);

                    if (!string.IsNullOrEmpty(custInfo.name))
                    {
                        resp.balance = custInfo.balance.ToString();
                        resp.accountName = custInfo.name;
                        resp.responseCode = "00";
                        resp.responseMessage = "Successful";
                        resp.phoneNo = custInfo.phone;
                    }
                    else
                    {
                        resp.responseCode = "09";
                        resp.responseMessage = "Invalid Account Number";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
            }
            else
            {

            }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            
            
        }
        [HttpPost]
        [Route("api/CustomerAPI/AccountBalance")]
        public HttpResponseMessage AccountBalance([FromBody] BalanceEnquiry bal)
        {
            try
            {
                var username = "";
                var password = "";
                int auth = 0;

                var re = Request;
                var headers = re.Headers;

                if (headers.Contains("Username"))
                {
                    username = headers.GetValues("Username").First();
                }
                if (headers.Contains("Password"))
                {
                    password = headers.GetValues("Password").First();
                }
                try
                {
                    //decrypt the password sent
                    // var decryptedpswd = Decrypt(password, "h28wi47");
                    auth = authenticateUsers(username, password);
                    //auth = 1;
                }
                catch (Exception ex)
                {
                    auth = 0;
                }
                var ip = GetIP();
                string req = JsonConvert.SerializeObject(bal);
                long logID = logRequest(ip, req);
                //check if call is from permitted ip
                // var permittedip = ConfigurationManager.AppSettings["permittedip"];
                //if (auth > 0 && (permittedip == ip))
                bool permittedip = CheckPermittedIP(ip);
                if (auth > 0 && (permittedip))
                {

                    BalanceEnquiryResponse resp = new BalanceEnquiryResponse();

                    try
                    {
                        InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();

                        //var custInfo = S.GetCustomerInfoByAccountNo(bal.accountNo);
                        //AccountDetailsResponse custInfo = AccountDetails(bal.accountNo);
                        AccountDetailsResponse custInfo = AccountDetailsByProxy(bal.accountNo, username, password);

                        if (!string.IsNullOrEmpty(custInfo.name))
                        {
                            resp.balance = custInfo.balance.ToString();
                            resp.accountName = custInfo.name;
                            resp.responseCode = "00";
                            resp.responseMessage = "Successful";
                            resp.phoneNo = custInfo.phone;
                        }
                        else
                        {
                            resp.responseCode = "09";
                            resp.responseMessage = "Invalid Account Number";
                        }

                        //update log 
                        string r = JsonConvert.SerializeObject(resp);
                        updateRequest(logID, r);




                    }
                    catch (Exception ex)
                    {
                        var err2 = new LogUtility.Error()
                        {
                            ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                            ErrorTime = DateTime.Now,
                            ModulePointer = "Error",
                            StackTrace = ex.StackTrace

                        };
                        LogUtility.ActivityLogger.WriteErrorLog(err2);
                        resp.responseCode = "96";
                        resp.responseMessage = "Error Retrieving Account Balance";

                        string r = JsonConvert.SerializeObject(resp);
                        updateRequest(logID, r);
                    }
                    string output = JsonConvert.SerializeObject(resp);
                    // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                    //return r;
                }
                else
                {
                    responseclass resp = new responseclass();
                    resp.responseCode = "99";
                    resp.responseMessage = "Invalid Username or Password";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                    string output = JsonConvert.SerializeObject(resp);

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }catch(Exception ex){
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error Accessing The Method",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                string output = "";

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
       
        public string EncryptRequest(string Req)
        {
            var result = string.Empty;
            try
            {
                var e = new NIBSS.processmessage();
               // var e = new EncryptService.processmessageSoapClient();
                result = e.SSMEncryptData("BVN" + Req);
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Customer API - Encrypt Request",
                    StackTrace = ex.StackTrace
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
            }

            return result;
        }
        
        public string DecryptRequest(string Req)
        {
            var result = string.Empty;
            try
            {
                var e = new NIBSS.processmessage();
                result = e.SSMDecryptData("BVN"+Req);
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error Decrypting Request",
                    StackTrace = ex.StackTrace
                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
            }

            return result;
        }

        public int InsertBiometrics(string bvn,string accountID)
        {
            string BankCode = "301";
            string Branch = "HEAD OFFICE BRANCH";
            Decimal cif  = Convert.ToDecimal(accountID);
            string EnrollmentIP = GetIP();
            string EnrollmentOfficer = "AGENCY BANKING";
            DateTime EntryDate = DateTime.Now;
            string EntryMode = "WEB API";
            string UserID = "WEBAPI";
            string PhoneNo = "080";
            DateTime RegDate = DateTime.Now;
            int sn = Convert.ToInt32(GenerateRndNumber(4));
            try
            {
                OracleConnection conn = DBUtils.GetDBConnection();
               // conn.Open();

                //string sql = "Insert into BIODATA (BANKCODE, BIOMETRICID, BRANCH,CUSTOMERID,ENROLLMENTIP,ENROLLMENTOFFICER,ENTRYDATE,ENTRYMODE) "
                //                                     + " values (@BankCode, @BioMetricID, @Branch,@CustomerID,@EnrollmentIP,@EnrollmentOfficer,@EntryDate,@EntryMode) ";

                //OracleCommand cmd = conn.CreateCommand();
                //cmd.CommandText = sql;

                var commandText = "insert into BIODATA (SN,BANKCODE, BIOMETRICID, BRANCH,CUSTOMERID,ENROLLMENTIP,ENROLLMENTOFFICER,ENTRYDATE,ENTRYMODE,USERID,PHONENO,REGISTRATIONDATE) values(:SN,:BankCode, :BioMetricID, :Branch,:CustomerID,:EnrollmentIP,:EnrollmentOfficer,:EntryDate,:EntryMode,:UserID,:PhoneNo,:RegDate)";

                
                using (OracleCommand command = new OracleCommand(commandText, conn))
                {
                    
                    command.Parameters.Add("SN", sn);
                    command.Parameters.Add("BankCode", BankCode);
                    command.Parameters.Add("BioMetricID", bvn);
                    command.Parameters.Add("Branch", Branch);
                    command.Parameters.Add("CustomerID", cif);
                    command.Parameters.Add("EnrollmentIP", EnrollmentIP);
                    command.Parameters.Add("EnrollmentOfficer", EnrollmentOfficer);
                    command.Parameters.Add("EntryDate", EntryDate);
                    command.Parameters.Add("EntryMode", EntryMode);
                    command.Parameters.Add("UserID", UserID);
                    command.Parameters.Add("PhoneNo", PhoneNo);
                    command.Parameters.Add("RegDate", RegDate);
                    command.Connection.Open();
                    int rowCount= command.ExecuteNonQuery();
                    command.Connection.Close();

                    return rowCount;
                }

                // Create Parameter.
                //OracleParameter BankCodeParam = new OracleParameter("@BankCode", OracleDbType.NVarchar2);
                //BankCodeParam.Value = BankCode;
                //cmd.Parameters.Add(BankCodeParam);

                //// Add parameter @highSalary (Write shorter)
                //OracleParameter BioMetricIDParam = cmd.Parameters.Add("@BioMetricID", OracleDbType.NVarchar2);
                //BioMetricIDParam.Value = bvn;

                // Add parameter @lowSalary (more shorter).
                //cmd.Parameters.Add("@BankCode", OracleDbType.NVarchar2).Value = BankCode;
                //cmd.Parameters.Add("@BioMetricID", OracleDbType.NVarchar2).Value = bvn;
                //cmd.Parameters.Add("@Branch", OracleDbType.NVarchar2).Value = Branch;
                //cmd.Parameters.Add("@CustomerID", OracleDbType.Decimal).Value = cif;
                //cmd.Parameters.Add("@EnrollmentIP", OracleDbType.NVarchar2).Value = EnrollmentIP;
                //cmd.Parameters.Add("@EnrollmentOfficer", OracleDbType.NVarchar2).Value = EnrollmentOfficer;
                //cmd.Parameters.Add("@EntryDate", OracleDbType.Date).Value = EntryDate;
                //cmd.Parameters.Add("@EntryMode", OracleDbType.NVarchar2).Value = EntryMode;

                //// Execute Command (for Delete,Insert or Update).
                //int rowCount = cmd.ExecuteNonQuery();
                
            }
            catch (OracleException ex) {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error Inserting Biometerics",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                int rowCount = 0;
                return rowCount;
            }
        }
       
        public int CountBioDataInfo(decimal cif, string BVN)
        {
            //using (ImalEntities db = new ImalEntities())
            //{
            //    int cnt = db.BIODATAs.Count(a => a.BIOMETRICID == BVN && a.CUSTOMERID == cif);
            //    return cnt;
            //}

            OracleConnection conn = DBUtils.GetDBConnection();
            conn.Open();
            //string sql = "Select * FROM BIODATA WHERE BIOMETRICID=@BVN AND CUSTOMERID=@cif";
            string sql = "Select * FROM BIODATA WHERE BIOMETRICID=:BVN AND CUSTOMERID=:cif";

            // Create command.
            OracleCommand cmd = new OracleCommand();

            // Set connection for command.
            cmd.Connection = conn;
            cmd.CommandText = sql;
            //cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

            OracleParameter BVNValue = new OracleParameter("BVN", OracleDbType.NVarchar2, 40);
            BVNValue.Direction = ParameterDirection.Input;
            BVNValue.Value = BVN;
            //
            OracleParameter cifValue = new OracleParameter("cif", OracleDbType.NVarchar2, 40);
            cifValue.Direction = ParameterDirection.Input;
            cifValue.Value = cif;
            cmd.Parameters.Add(BVNValue);
            cmd.Parameters.Add(cifValue);
            cmd.Connection = conn;
            int theCount = 0;
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                string BIOMETRICID = "";
                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                      BIOMETRICID=reader["BIOMETRICID"].ToString();
                    }
                }
                if (string.IsNullOrEmpty(BIOMETRICID))
                {
                    theCount = 0;
                }
                else
                {
                    theCount = 1;
                }
            }
            
            
            
            
            
            //int theCount = (int)cmd.ExecuteScalar();

            return theCount;
        }
        public linkbvnresponse LinkBVNByProxy(string AccountNo, string bvn,string cif)
        {
            var url = ConfigurationManager.AppSettings["linkbvnurl"];

            linkbvnrequest ac = new linkbvnrequest();
            ac.accountNo = AccountNo;
            ac.bvn = bvn;
            //get cif of customer
            ac.cif = cif;
            string rs = JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "text/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            //client.DefaultRequestHeaders.Add("Username", username);
            //client.DefaultRequestHeaders.Add("Password", password);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<LinkingObject>(r.ToString());


            linkbvnresponse re = new linkbvnresponse();
            re.responseCode = resp.responseCode;
            re.responseMessage = resp.responseMessage;
            


            return re;
        }

        public AccountDetailsResponse AccountDetailsByProxy22(string AccountNo)
        {
            AccountDetailsResponse re = new AccountDetailsResponse();
            try
            {
                var url = ConfigurationManager.AppSettings["localnameenquiryurl2"];

                account ac = new account();
                ac.accountNo = AccountNo;
                string rs = JsonConvert.SerializeObject(ac);

                var content = new StringContent(rs, Encoding.UTF8, "application/json");
                var client = new HttpClient();

                HttpResponseMessage result = null;
                //client.DefaultRequestHeaders.Add("Username", username);
                //client.DefaultRequestHeaders.Add("Password", password);
                result = client.PostAsync(url, content).Result;
                //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                var resp = JsonConvert.DeserializeObject<Rootobject>(r.ToString());


               
                re.balance = resp.balance;
                re.name = resp.accountName;
                re.phone = resp.phoneNo;
                re.cif = resp.cif;
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error, Accessing ",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
            }

            // string response = await result.Content.ReadAsStringAsync();


            return re;
        }

        [HttpPost]
        [Route("api/CustomerAPI/LinkBVN")]
        public HttpResponseMessage LinkBVN([FromBody] bvnlinking details)
        {
            bvnlinkingresponse resp = new bvnlinkingresponse();
            string output = "";
            DateTime loginTime = DateTime.Now;

            var ip = GetIP();
            string req = JsonConvert.SerializeObject(details);

            try
            {
               //get account details and compare
                InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                //var custInfo = S.GetCustomerInfoByAccountNo(details.accountNo);
                AccountDetailsResponse custInfo=AccountDetailsByProxy22(details.accountNo);
                string data = custInfo.name;
                // Split string on spaces (this will separate all the words).
                
                string fName = "";
                string mName = "";
                string lName = "";
              
                string[] broken_str = data.Split(' ');

                for (int i = 0; i < broken_str.Length; i++)
                {
                    if (broken_str.Length == 2)
                    {
                        fName = broken_str[0];
                        mName = "";
                        lName = broken_str[1];
                    }
                    else
                    {
                        fName = broken_str[0];
                        mName = broken_str[1];
                        lName = broken_str[2];
                    }
                  
                }
                var firstname = fName;
                var middlename = mName;
                var lastname = lName;

                //get bvn validation details
                internalbvnvalidation r= InternalBVNValidation(details.bvn);
                var bvnfirstname = r.firstname;
                var bvnmiddlename = r.middlename;
                var bvnlastname = r.lastname;

                //compare details
                if ((firstname.ToLower() == bvnfirstname.ToLower() && lastname.ToLower() == bvnlastname.ToLower()) || (firstname.ToLower() == bvnlastname.ToLower() && lastname.ToLower() == bvnfirstname.ToLower()))
                {
                    //go for linking
                    linkbvnresponse re= LinkBVNByProxy(details.accountNo, details.bvn,custInfo.cif);
                    resp.responseCode = re.responseCode;
                    resp.responseMessage = re.responseMessage;

                    output = JsonConvert.SerializeObject(resp);

                    logRequestResponse(ip, req, loginTime, output, "LinkBVN");
                }
                else
                {
                    //return names do not match
                    resp.responseCode = "89";
                    resp.responseMessage = "Account Details does not match BVN details";

                    output = JsonConvert.SerializeObject(resp);

                    logRequestResponse(ip, req, loginTime, output, "LinkBVN");
                }
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                resp.responseCode = "ZZ";
                resp.responseMessage = "Error Accessing BVN Linking";

                output = JsonConvert.SerializeObject(resp);

                logRequestResponse(ip, req, loginTime, output, "LinkBVN");
            }
            //output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }
        
        [HttpPost]
        [Route("api/CustomerAPI/LinkBVN2")]
        public HttpResponseMessage LinkBVN2([FromBody] bvnlinking details)
        {
            bvnlinkingresponse resp = new bvnlinkingresponse();
            
           // OracleConnection connection = DBUtils.GetDBConnection();
            //connection.Open();
            try
            {
                var ip = GetIP();
                string req = JsonConvert.SerializeObject(details);
                long logID = logRequest(ip, req);
                var FirstName = "";
                var MiddleName = "";
                var LastName = "";
                

                //check if valid account no
                InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
               // NIBSSBVN.BVNValidation nBVN = new NIBSSBVN.BVNValidation();
                var nBVN = new NIBSSBVN.BVNValidation();

                BVNNibss.BVNValidationWebService valService = new BVNNibss.BVNValidationWebService();


                var custInfo = S.GetCustomerInfoByAccountNo(details.accountNo);
                if (!string.IsNullOrEmpty(custInfo.AccountID))
                {
                    //get the cif
                    var cif = custInfo.AccountID;
                    var BBVN = EncryptRequest(details.bvn);
                    var bvnresult = nBVN.verifySingleBVN(BBVN, "301");
                   // var bvnresult = valService.verifySingleBVN(BBVN, "301");
                    //decrypt result
                    var decryptedMsg = DecryptRequest(bvnresult);

                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(decryptedMsg);

                   // XmlNodeList xnList = xml.SelectNodes("BvnSearchResult");

                    XmlNodeList xnList = xml.SelectNodes("BvnSearchResult");


                
                foreach (XmlNode xn in xnList)
                {
                    string bvnno = xn["Bvn"].InnerText;
                    FirstName = xn["FirstName"].InnerText;
                    MiddleName = xn["MiddleName"].InnerText;
                    LastName = xn["LastName"].InnerText;
                }

                    

                    string data = custInfo.CustomerName;
                    // Split string on spaces (this will separate all the words).
                    string[] words = data.Split(null);
                    string fName = "";
                    string mName = "";
                    string lName = "";
                    foreach (string word in words)
                    {
                        fName = word;
                        mName = word;
                        lName = word;

                    }
                    //first check if it exists in the biodata first
                    int cnt = CountBioDataInfo(Convert.ToDecimal(custInfo.AccountID), details.bvn);
                    if (cnt == 0)
                    {
                        //insert into biodata table
                        //if (fName == FirstName && mName == MiddleName && lName == LastName)
                        //if (fName == FirstName && mName == MiddleName && lName == LastName)
                        //{

                        //add to biometrics table

                            //using (ImalEntities dbb = new ImalEntities())
                            //{
                            //    BIODATA b = new BIODATA();
                            //    b.BANKCODE = "301";
                            //    b.BIOMETRICID = details.bvn;
                            //    b.BRANCH = "HEAD OFFICE BRANCH";
                            //    b.CUSTOMERID = Convert.ToDecimal(custInfo.AccountID);
                            //    b.ENROLLMENTIP = GetIP();
                            //    b.ENROLLMENTOFFICER = "AGENCY BANKING";
                            //    b.ENTRYDATE = DateTime.Now;
                            //    b.ENTRYMODE = "WEB API";
                            //    dbb.BIODATAs.Add(b);
                            //    dbb.SaveChanges();
                            //}
                       int insert= InsertBiometrics(details.bvn, custInfo.AccountID);
                       if (insert>0)
                       {
                           resp.responseMessage = "Successfull";
                           resp.responseCode = "00";
                       }
                       else
                       {
                           resp.responseMessage = "Error Linking Card";
                           resp.responseCode = "59";
                       }

                        //}
                        //else
                        //{
                        //    resp.responseMessage = "Name mismatch";
                        //    resp.responseCode = "56";
                        //}
                    }
                    else
                    {
                        resp.responseMessage = "BVN previously linked";
                        resp.responseCode = "58";
                    }





                }
                else
                {
                    resp.responseCode = "96";
                    resp.responseMessage = "Invalid Account Number";
                }

                //get bvn details from nibbs
                //compare details from cba and nibbs
                //check the biodata table if

               
            }
            catch (Exception ex)
            {
                
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "ZZ";
                resp.responseMessage = "Error Accessing BVN Linking";
            }

            string output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/AccountDetailsNew22")]
        public HttpResponseMessage AccountDetailsNew22([FromBody] customerdetails t)
        {
            string output = "";
            var AccountNo = t.accountNo;
            OracleConnection conn = DBUtils.GetDBConnection();
            conn.Open();

            //string sql = "SELECT a.LONG_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance, b.tel as Phone FROM imal_trn.amf a,imal_trn.cif b  WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
            // string sql = "SELECT b.TEL as Phone, b.CIF_NO, b.AUTH_NAME,b.FIRST_NAME_ENG,b.SEC_NAME_ENG,b.LAST_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance,b.description as Email,b.birth_date as dob,c.long_desc_eng as accountType FROM imal_trn.amf a,imal_trn.cif b,imal_trn.gen_ledger c WHERE a.cif_sub_no=b.cif_no AND a.gl_code=c.gl_code AND a.additional_reference=:acctno";
            string sql = "SELECT case when b.TEL is NULL then CA.TEL else b.TEL end as Phone, b.CIF_NO, b.AUTH_NAME,b.FIRST_NAME_ENG,b.SEC_NAME_ENG,b.LAST_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance,b.description as Email,b.birth_date as dob,c.long_desc_eng as accountType FROM imal.amf a,imal.cif b,imal.gen_ledger c,imal.cif_address CA WHERE a.cif_sub_no=b.cif_no AND a.gl_code=c.gl_code AND b.CIF_NO = CA.CIF_NO AND a.additional_reference=:acctno";
            // Create command.
            OracleCommand cmd = new OracleCommand();

            // Set connection for command.
            cmd.Connection = conn;
            cmd.CommandText = sql;
            // cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

            OracleParameter AccNo = new OracleParameter("acctno", OracleDbType.Varchar2, 40);
            AccNo.Direction = ParameterDirection.Input;
            AccNo.Value = AccountNo;

            cmd.Parameters.Add(AccNo);
            cmd.Connection = conn;
            string AccountName = "";
            string firstname = "";
            string secondname = "";
            string lastname = "";
            string Balance = "";
            string Phone = "";
            string bvn = "";
            string email = "";
            string dob = "";
            string cif = "";
            string tel = "";
            string accountType = "";

            customerdetailsresponse resp = new customerdetailsresponse();

            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {



                    if (reader.HasRows)
                    {
                        int sn = 0;

                        while (reader.Read())
                        {
                            sn++;

                            AccountName = reader["AUTH_NAME"].ToString();
                            Balance = reader["CustomerBalance"].ToString();
                            tel = reader["Phone"].ToString();
                            firstname = reader["FIRST_NAME_ENG"].ToString();
                            secondname = reader["SEC_NAME_ENG"].ToString();
                            lastname = reader["LAST_NAME_ENG"].ToString();
                            email = reader["Email"].ToString();
                            dob = reader["dob"].ToString();
                            cif = reader["CIF_NO"].ToString();
                            accountType = reader["accountType"].ToString();



                        }
                    }
                }

                resp.balance = Balance;
                resp.accountname = AccountName;
                resp.firstname = firstname;
                resp.secondname = secondname;
                resp.lastname = lastname;
                resp.phone = tel;
                resp.bvn = GetBVN(cif);
                resp.dob = dob;
                resp.email = email;
                resp.cif = cif;
                resp.accountType = accountType;

                output = JsonConvert.SerializeObject(resp);
                
            }
            catch (OracleException ex)
            {
                //return null;
                responseclass r = new responseclass();
                r.responseCode = "99";
                r.responseMessage = "Error";

                output = JsonConvert.SerializeObject(resp);
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        public customerdetailsresponse AccountDetailsNew(string AccountNo)
        {

            OracleConnection conn = DBUtils.GetDBConnection();
            conn.Open();

            //string sql = "SELECT a.LONG_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance, b.tel as Phone FROM imal_trn.amf a,imal_trn.cif b  WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
            // string sql = "SELECT b.TEL as Phone, b.CIF_NO, b.AUTH_NAME,b.FIRST_NAME_ENG,b.SEC_NAME_ENG,b.LAST_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance,b.description as Email,b.birth_date as dob,c.long_desc_eng as accountType FROM imal_trn.amf a,imal_trn.cif b,imal_trn.gen_ledger c WHERE a.cif_sub_no=b.cif_no AND a.gl_code=c.gl_code AND a.additional_reference=:acctno";
            string sql = "SELECT case when b.TEL is NULL then CA.TEL else b.TEL end as Phone, b.CIF_NO, b.AUTH_NAME,b.FIRST_NAME_ENG,b.SEC_NAME_ENG,b.LAST_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance,b.description as Email,b.birth_date as dob,c.long_desc_eng as accountType FROM imal.amf a,imal.cif b,imal.gen_ledger c,imal.cif_address CA WHERE a.cif_sub_no=b.cif_no AND a.gl_code=c.gl_code AND b.CIF_NO = CA.CIF_NO AND a.additional_reference=:acctno";
            // Create command.
            OracleCommand cmd = new OracleCommand();

            // Set connection for command.
            cmd.Connection = conn;
            cmd.CommandText = sql;
            // cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

            OracleParameter AccNo = new OracleParameter("acctno", OracleDbType.Varchar2, 40);
            AccNo.Direction = ParameterDirection.Input;
            AccNo.Value = AccountNo;

            cmd.Parameters.Add(AccNo);
            cmd.Connection = conn;
            string AccountName = "";
            string firstname = "";
            string secondname = "";
            string lastname = "";
            string Balance = "";
            string Phone = "";
            string bvn = "";
            string email = "";
            string dob = "";
            string cif = "";
            string tel = "";
            string accountType = "";

            customerdetailsresponse resp = new customerdetailsresponse();

            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {



                    if (reader.HasRows)
                    {
                        int sn = 0;

                        while (reader.Read())
                        {
                            sn++;

                            AccountName = reader["AUTH_NAME"].ToString();
                            Balance = reader["CustomerBalance"].ToString();
                            tel = reader["Phone"].ToString();
                            firstname = reader["FIRST_NAME_ENG"].ToString();
                            secondname = reader["SEC_NAME_ENG"].ToString();
                            lastname = reader["LAST_NAME_ENG"].ToString();
                            email = reader["Email"].ToString();
                            dob = reader["dob"].ToString();
                            cif = reader["CIF_NO"].ToString();
                            accountType = reader["accountType"].ToString();



                        }
                    }
                }
                
                resp.balance = Balance;
                resp.accountname = AccountName;
                resp.firstname = firstname;
                resp.secondname = secondname;
                resp.lastname = lastname;
                resp.phone = tel;
                resp.bvn = GetBVN(cif);
                resp.dob = dob;
                resp.email = email;
                resp.cif = cif;
                resp.accountType = accountType;

                return resp;
            }
            catch (OracleException ex)
            {
                return null;
            }

        }

        [HttpPost]
        [Route("api/CustomerAPI/GenerateOTP")]
        public HttpResponseMessage GenerateOTP([FromBody] genotp t)
        {
            responseclass resp = new responseclass();
            var output="";
            var ip = GetIP();
            _jobLogger.Info("|GenerateOTP| ---- Inside the GenerateOTP Method--------");
            DateTime loginTime = DateTime.Now;
            string req = JsonConvert.SerializeObject(t);
            _jobLogger.Info("|GenerateOTP| ---- Request Body Is--------" + req);
            //long logID = logRequest(ip, req);
            try
            {
            var otp = GenerateRndNumber(6);

            _jobLogger.Info("|GenerateOTP| ---- OTP generated Is--------" + otp);
            int otpID = 0;
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                otprequest o = new otprequest();
                o.otp = otp;
                o.otpAccountNo = "0000000000";
                o.otpDate = DateTime.Now;
                o.otpPhone = t.phoneNo;
                o.otpExpiry = DateTime.Now.AddMinutes(5.0);
                db.otprequests.Add(o);
                db.SaveChanges();
                otpID = o.otpRequestID;
            }
            if (otpID > 0)
            {
                //send sms
                InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                InternetBankingAPI.SmsObject O = new InternetBankingAPI.SmsObject();
                InternetBankingAPI.SmsResponse R = new InternetBankingAPI.SmsResponse();

               _jobLogger.Info("|GenerateOTP| ---- About sending mail for successful OTP generation--------");

                O.MobileNo = t.phoneNo;
                O.SmsContent = t.msg+": "+otp;
                O.SenderId = "INTBK";
                bool T = true;
                S.SendSmsViaHelper(O, out R, out T);

                _jobLogger.Info("|GenerateOTP| ---- Mail sent successfully on OTP generation--------");

                resp.responseCode = "00";
                resp.responseMessage = "Successful";

                output = JsonConvert.SerializeObject(resp);

               logRequestResponse2(ip, req, "GenerateOTP", loginTime, output);
               _jobLogger.Info("|GenerateOTP| ---- Successful Response Body--------" + output);
            }
            else
            {
                resp.responseCode = "96";
                resp.responseMessage = "Error generating otp";

               output = JsonConvert.SerializeObject(resp);
               logRequestResponse2(ip, req, "GenerateOTP", loginTime, output);

              _jobLogger.Info("|GenerateOTP| ---- Unsuccessful Response Body--------" + output);

           }
           }catch(Exception ex){

            var err2 = new LogUtility.Error()
            {
                ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                ErrorTime = DateTime.Now,
                ModulePointer = "Error",
                StackTrace = ex.StackTrace

            };
            LogUtility.ActivityLogger.WriteErrorLog(err2);

                resp.responseCode = "96";
                resp.responseMessage = ex.Message;

                 output = JsonConvert.SerializeObject(resp);

                 logRequestResponse2(ip, req, "GenerateOTP", loginTime, output);

                _jobLogger.Info("|GenerateOTP| ---- Unsuccessful Response Body Exception--------" + output);

            }
            output = JsonConvert.SerializeObject(resp);

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/ConfirmOTP")]
        public HttpResponseMessage ConfirmOTP([FromBody] newotprequest t)
        {
            string output = "";
            var username = "";
            var password = "";
            int auth = 0;
            int checkOTP = 0;

            DateTime loginTime = DateTime.Now;
            responseclass resp = new responseclass();
            customerdetailsrequest rr = new customerdetailsrequest();

            var ip = GetIP();
            string req = JsonConvert.SerializeObject(t);

            //long logID = logRequest(ip, req);

            _jobLogger.Info("|ConfirmOTP| ---- Inside the ConfirmOTP method--------");

            _jobLogger.Info("|ConfirmOTP| ---- Request Body is--------" + req);


            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                {

                    checkOTP = db.otprequests.Count(a => a.otp == t.otp && a.otpExpiry > DateTime.Now && a.otpPhone==t.phoneNo);
                }
                if (checkOTP > 0)
                {
                //still within the accepted time and 

                _jobLogger.Info("|ConfirmOTP| ---- checkOTP value is--------" + checkOTP);
                resp.responseCode = "00";
                    resp.responseMessage = "Successful";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, req, "ConfirmOTP", loginTime, output);
                }
                else
                {
                    resp.responseCode = "96";
                    resp.responseMessage = "Invalid OTP or OTP expired";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, req, "ConfirmOTP", loginTime, output);
                }

                output = JsonConvert.SerializeObject(resp);

            _jobLogger.Info("|ConfirmOTP| ---- response out is--------" + output);

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }


        [HttpPost]
        [Route("api/CustomerAPI/ValidateOTP")]
        public HttpResponseMessage ValidateOTP([FromBody] otpvalidationrequest t)
        {
            string output = "";
             var username = "";
            var password = "";
            int auth = 0;
            int checkOTP = 0;
            responseclass resp = new responseclass();
            customerdetailsrequest rr = new customerdetailsrequest();

            _jobLogger.Info("|ValidateOTP| ---- Inside the ValidateOTP method--------");         

            DateTime loginTime = DateTime.Now;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {              
                auth = authenticateUsers(username, password);                
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(t.accountNo);
            _jobLogger.Info("|ValidateOTP| ---- Request Body is--------" + req);


            //long logID = logRequest(ip, req);
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities()) {
                
                    checkOTP = db.otprequests.Count(a => a.otp == t.otp && a.otpAccountNo == t.accountNo && a.otpExpiry > DateTime.Now);
                }
                if (checkOTP > 0)
                {
                    //still within the accepted time and 
                    _jobLogger.Info("|ValidateOTP| ---- checkOTP is--------" + checkOTP);
                    resp.responseCode = "00";
                    resp.responseMessage = "Successful";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, req, "ValidateOTP", loginTime, output);
                }
                else
                {
                    resp.responseCode = "96";
                    resp.responseMessage = "Invalid OTP or OTP expired";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, req, "ValidateOTP", loginTime, output);
                }

                output = JsonConvert.SerializeObject(resp);

                _jobLogger.Info("|ValidateOTP| ---- Response Body is--------" + output);
            }
            else
            {
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, req, "ValidateOTP", loginTime, output);

            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }
        [HttpPost]
        [Route("api/CustomerAPI/DataVending")]
        public async Task<HttpResponseMessage> DataVending([FromBody] DataVendingRequest req)
        {

            DataVendingRequest d = new DataVendingRequest();
            d.loginId = ConfigurationManager.AppSettings["insurtechID"];
            d.key = ConfigurationManager.AppSettings["insurtechKey"];
            d.recipient = req.recipient;
            d.date = req.date;
            d.amount = req.amount;
            //d.serviceId = ConfigurationManager.AppSettings["dataserviceID"];
            d.serviceId = req.serviceId;
            d.requestId = GenerateRndNumber(8);

            string s = JsonConvert.SerializeObject(d);
            var ip = GetIP();
            long logID = logRequest(ip, s);

            string rs = JsonConvert.SerializeObject(d);
            var url = ConfigurationManager.AppSettings["datavendingurl"];

            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            string output = "";

            HttpResponseMessage result = null;

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new
RemoteCertificateValidationCallback(delegate { return true; });
                result = client.PostAsync(url, content).Result;

                var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                
                var resp = JsonConvert.DeserializeObject<dVendingResponse>(r.ToString());
                //DataVendResp av = new DataVendResp();
                //if (resp.statusCode == "00")
                //{

                //    av.response = true;
                //}
                //else
                //{
                //    av.response = false;
                //}

                output = JsonConvert.SerializeObject(resp);
                updateRequest(logID, output);
            }
            catch (Exception ex)
            {
                output = "";
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/AirtimeVending")]
        public HttpResponseMessage AirTime([FromBody] AirTimeVendingRequest req)
        {
            AirTimeVendingRequest d = new AirTimeVendingRequest();
            responseclass response = new responseclass();
            d.loginId = ConfigurationManager.AppSettings["insurtechID"];
            d.key = ConfigurationManager.AppSettings["insurtechKey"];
            d.recipient = req.recipient;
            d.date = req.date;
            d.amount = req.amount;
            //d.serviceId = ConfigurationManager.AppSettings["airtimeserviceID"]; 
            d.serviceId = req.serviceId;
            d.requestId = GenerateRndNumber(8);


            string rs = JsonConvert.SerializeObject(d);
            var url = ConfigurationManager.AppSettings["airtimevendingurl"];
            //string o = "{'loginId':'48447','key':'0644378ec7fe233','requestId':'11318','serviceId':A01E','amount':'100','recipient':'08030001111', 'date':'25-Jun-2019 12:51 GMT'}";
            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            string output = "";

            HttpResponseMessage result = null;
            //client.DefaultRequestHeaders.Add("Username", username);
            //client.DefaultRequestHeaders.Add("Password", password);
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new
RemoteCertificateValidationCallback(delegate { return true; });
                result = client.PostAsync(url, content).Result;
                //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                var resp = JsonConvert.DeserializeObject<aVendingResponse>(r.ToString());
                AirTimeVendResp av = new AirTimeVendResp();
                if (resp.responseCode == "00")
                {
                    response.responseCode = "00";
                    response.responseMessage = "Successful";
                    //av.response = true;
                }
                else
                {
                    response.responseCode = resp.responseCode;
                    response.responseMessage = resp.responseMessage;
                    //av.response = false;
                }

                output = JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                output = "";
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };

        }
        [HttpPost]
        [Route("api/CustomerAPI/DataBundle")]
        public async Task<HttpResponseMessage> DataBundle([FromBody] DataBundleRequest req)
        {
            databundlerequest db = new databundlerequest();
            db.loginId = ConfigurationManager.AppSettings["insurtechID"];
            db.key = ConfigurationManager.AppSettings["insurtechKey"];
            //db.serviceId = ConfigurationManager.AppSettings["databundleserviceID"];
            db.serviceId = req.serviceId;
            string s = JsonConvert.SerializeObject(db);
            var ip = GetIP();
            long logID = logRequest(ip, s);
            string rs = JsonConvert.SerializeObject(db);
            var url = ConfigurationManager.AppSettings["databundleurl"];

            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            string output = "";

            HttpResponseMessage result = null;
            var r = "";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new
RemoteCertificateValidationCallback(delegate { return true; });
                result = client.PostAsync(url, content).Result;

                r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");

                updateRequest(logID, r);



                //output = JsonConvert.SerializeObject(av);
            }
            catch (Exception ex)
            {
                output = "";
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(r, System.Text.Encoding.UTF8, "application/json")
            };

        }
        [HttpPost]
        [Route("api/CustomerAPI/Vend")]
        public HttpResponseMessage Vend([FromBody] vendrequest t)
        {
            string output = "";
            var username = "";
            var password = "";
            int auth = 0;
            responseclass resp = new responseclass();
            customerdetailsrequest rr = new customerdetailsrequest();

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {

                auth = authenticateUsers(username, password);

            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(t.catID);
            vendrequest v = new vendrequest();
            vendresponse vr = new vendresponse();
            JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities();


            long logID = logRequest(ip, req);
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                List<string> l = new List<string>();
                var ll = db.vends.Where(a=>a.catID==t.catID).ToList();
                output = JsonConvert.SerializeObject(ll);
            }
            else
            {
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                output = JsonConvert.SerializeObject(resp);
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/VendingCat")]
        public HttpResponseMessage VendingCat([FromBody] VendingCatRequest type)
        {
            string output = "";
             var username = "";
            var password = "";
            int auth = 0;
            responseclass resp = new responseclass();
            customerdetailsrequest rr = new customerdetailsrequest();

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
              
                auth = authenticateUsers(username, password);
                
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            vendcatrequest v = new vendcatrequest();
            vendcatresponse vr = new vendcatresponse();
            JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities();
            v.method = "Vending Request";
            string req = JsonConvert.SerializeObject(v);

           

            long logID = logRequest(ip, req);
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                List<string> l = new List<string>();
                //var ll=db.vendingcats.ToList();
                var ll = db.vendingcats.Where(a => a.vendType == type.typeID);
                output=JsonConvert.SerializeObject(ll);
                //l.Add(ll);
                
            }
            else
            {
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                output = JsonConvert.SerializeObject(resp);
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/ADValidation")]
        public HttpResponseMessage ADValidation([FromBody] ValidateADUser user)
        {
            var ip = GetIP();

            //get the domain id of staff
            var url = ConfigurationManager.AppSettings["staffidurl"];
            staffIDRequest ac = new staffIDRequest();
            ac.email = user.UserID;
            string rs = JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            result = client.PostAsync(url, content).Result;
            var rrrr = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var rp = JsonConvert.DeserializeObject<staffUserIDResponse>(rrrr.ToString());
            var newUserID = rp.username;



            string req = JsonConvert.SerializeObject(user);
            long logID = logRequest(ip, req);
            responseclass resp = new responseclass();

            try
            {
                InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                InternetBankingAPI.LoginResponse O = new InternetBankingAPI.LoginResponse();
                InternetBankingAPI.LoginObject Z = new InternetBankingAPI.LoginObject();



                Z.UserID = newUserID;
                Z.Password = user.Password;
                bool T = true;
                S.ValidateADUser(Z, out O, out T);

                if (O.ToString() == "SUCCESSFUL")
                {
                    resp.responseCode = "00";
                    resp.responseMessage = O.ToString();
                }
                else
                {
                    resp.responseCode = "66";
                    resp.responseMessage = O.ToString();
                }


                //update log 
                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);




            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "96";
                resp.responseMessage = "Error Logging Message";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
            }
            string output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
            //return r;

        }
        [HttpPost]
        [Route("api/CustomerAPI/StaffDetails")]
        public async Task<HttpResponseMessage> StaffDetails([FromBody] staffDetailsRequest req)
        {

            var url = ConfigurationManager.AppSettings["staffdetailsurl"];

            staffDetailsRequest ac = new staffDetailsRequest();
            ac.domainID = req.domainID;
            string rs = JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "application/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            //client.DefaultRequestHeaders.Add("Username", username);
            //client.DefaultRequestHeaders.Add("Password", password);
            //  ServicePointManager.Expect100Continue = true;
            //  ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //  ServicePointManager.ServerCertificateValidationCallback = new
            //RemoteCertificateValidationCallback
            //(
            //   delegate { return true; }
            //);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<staffDetailsResponse>(r.ToString());





            // string response = await result.Content.ReadAsStringAsync();



            //staffDetailsResponse r = GetStaffDetail(req.domainID);
            string output = JsonConvert.SerializeObject(resp);

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }



        [HttpPost]
        [Route("api/CustomerAPI/ValidateCustomer")]
        public HttpResponseMessage ValidateCustomer([FromBody] customerdetailsrequest t)
        {
            string output = "";
             var username = "";
            var password = "";
            int auth = 0;
            responseclass resp = new responseclass();
            customerdetailsrequest rr = new customerdetailsrequest();

            _jobLogger.Info("|ValidateCustomer| ----Inside the ValidateCustomer method -----------");

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
              
                //auth = authenticateUsers(username, password);
                auth = 1;

            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(t.accountNo);

            _jobLogger.Info("|ValidateCustomer| ----Request Body-----------" +req);

            long logID = logRequest(ip, req);
            bool permittedip = CheckPermittedIP(ip);
            
            if (auth > 0 && (permittedip))
            {
                try
                {
                    //now check if the account number is a customer
                    //var details = AccountDetailsNew(t.accountNo);
                    /****** Connect to Jaiz Middleware ****/
                    var url = ConfigurationManager.AppSettings["accountdetailsnew"];

                    _jobLogger.Info("|ValidateCustomer| ----accountdetailsnew URL-----------" + url);

                    rr.accountNo = t.accountNo;

                    _jobLogger.Info("|ValidateCustomer| ---- rr.accountNo -----------" + rr.accountNo);


                    string rs = JsonConvert.SerializeObject(rr);

                    var content = new StringContent(rs, Encoding.UTF8, "text/json");
                    var client = new HttpClient();

                    _jobLogger.Info("|ValidateCustomer| ---- making HTTPClient Call -----------");

                    HttpResponseMessage result = null;

                    result = client.PostAsync(url, content).Result;
                    
                    //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                    var rrr = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                    _jobLogger.Info("|ValidateCustomer| ---- Response from HTTPClient Call -----------" + rrr);
                    var details = JsonConvert.DeserializeObject<customerdetailsresponse>(rrr.ToString());

                    _jobLogger.Info("|ValidateCustomer| ---- Response(details) from HTTPClient Call -----------" + details.ToString());

                    /**** End connection to Jaiz Middleware **/

                    if (details != null)
                    {
                        //extract phone no and connect to otprequest
                        var phone = details.phone;
                        var accountNo = t.accountNo;
                        var phoneNo = "+234" + phone.Substring(1, phone.Length - 1);
                        var newOTP = GenerateRndNumber(6);
                        string msg = "Your OTP to complete Agency Banking Registration is: " + newOTP;
                        DateTime expTime = DateTime.Now;
                        DateTime expiry = expTime.AddMinutes(5.00);

                        //insert into otprequest table
                        using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                        {
                            otprequest r = new otprequest();
                            r.otp = newOTP;
                            r.otpAccountNo = t.accountNo;
                            r.otpDate = DateTime.Now;
                            r.otpExpiry = DateTime.Now.AddMinutes(5.00);
                            r.otpPhone = phone;
                            db.otprequests.Add(r);
                            db.SaveChanges();

                            _jobLogger.Info("|ValidateCustomer| ---- Saved inside otprequest table-----------");
                        }
                        /* InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                         InternetBankingAPI.SmsObject O = new InternetBankingAPI.SmsObject();
                         InternetBankingAPI.SmsResponse R = new InternetBankingAPI.SmsResponse();

                         O.MobileNo = phoneNo;
                         O.SmsContent = msg;
                         O.SenderId = "INTBK";
                         bool T = true;
                         S.SendSmsViaHelper(O, out R, out T);*/
                        var clt = new HttpClient();
                        sendsmsrequest s = new sendsmsrequest();
                        _jobLogger.Info("|ValidateCustomer| ---- About sending SMS-----------");                        
                        s.phoneNo = phone;
                        s.message = msg;
                        var send = JsonConvert.SerializeObject(s);

                        _jobLogger.Info("|ValidateCustomer| ---- Details to be sent on SMS is:::" + send);
                        var cont = new StringContent(send, Encoding.UTF8, "application/json");


                        HttpResponseMessage rst = null;
                        var u = ConfigurationManager.AppSettings["smslink"];
                        _jobLogger.Info("|ValidateCustomer| ---- sms url to call is:::" + u);
                        clt.DefaultRequestHeaders.Add("Username", username);
                        clt.DefaultRequestHeaders.Add("Password", password);
                        rst = clt.PostAsync(u, cont).Result;
                       
                        //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                        var rrrr = rst.Content.ReadAsStringAsync().Result.Replace("\\", "");
                        var answer = JsonConvert.DeserializeObject<responseclass>(rrrr.ToString());
                        _jobLogger.Info("|ValidateCustomer| ---- response for sms call is:::" + rrrr.ToString());

                        customerdetailsresponse z = new customerdetailsresponse();
                        z.accountname = details.accountname;
                        z.accountType = details.accountType;
                        z.address = details.address;
                        z.balance = details.balance;
                        z.bvn = details.bvn;
                        z.cif = details.cif;
                        z.dob = details.dob;
                        z.email = details.email;
                        z.firstname = details.firstname;
                        z.lastname = details.lastname;
                        z.phone = details.phone;
                        z.responseCode = "00";
                        z.responseMessage = "Successful";
                        z.secondname = details.secondname;
                        z.sex = details.sex;

                        //resp.responseCode = "00";
                        //resp.responseMessage = "Successful";

                        output = JsonConvert.SerializeObject(z);

                        _jobLogger.Info("|ValidateCustomer| ---- Successful response sent outside is:::" + output);

                        // var smsRespCode = SendSMS(phoneNo, msg);
                    }
                    else
                    {
                        //return invalid account

                        resp.responseCode = "99";
                        resp.responseMessage = "Invalid Account No";
                        string r = JsonConvert.SerializeObject(resp);
                        updateRequest(logID, r);
                        output = JsonConvert.SerializeObject(resp);

                        _jobLogger.Info("|ValidateCustomer| ---- Failed response sent outside is:::" + output);
                    }
                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Validating Customer";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);

                    _jobLogger.Info("|ValidateCustomer| ---- Failed response sent outside Exception is:::" + r);
                }
            }
        
            else
            {
                //responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                output = JsonConvert.SerializeObject(resp);

                
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };

        }

        [HttpPost]
        [Route("api/CustomerAPI/LocalNameEnquiry")]
        public HttpResponseMessage LocalNameEnquiry([FromBody] NameEnquiry name)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(name);

           

            long logID = logRequest(ip, req);
            //check if call is from permitted ip
          //  var permittedip = ConfigurationManager.AppSettings["permittedip"];
           // if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                NameEnquiryResponse resp = new NameEnquiryResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                    InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();

                   // var custInfo = S.GetCustomerInfoByAccountNo(name.accountNo);
                    AccountDetailsResponse custInfo = AccountDetailsByProxy2(name.accountNo,username,password);
                    
                    //if (!string.IsNullOrEmpty(custInfo.CustomerName))
                    //{
                    //    resp.accountName = custInfo.CustomerName;
                    //    resp.responseCode = "00";
                    //    resp.responseMessage = "Successful";
                    //    resp.phoneNo = custInfo.PhoneNo;
                    //}
                    if (!string.IsNullOrEmpty(custInfo.name))
                    {
                        resp.accountName = custInfo.name;
                        resp.responseCode = "00";
                        resp.responseMessage = "Successful";
                        resp.phoneNo = custInfo.phone;
                    }
                    else
                    {
                        resp.responseCode = "09";
                        resp.responseMessage = "Invalid Account Number";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        public string generatesessionid(string bankcode)
        {
            var senderscode = "301";
            var destcode = bankcode;
            var date = DateTime.Now.ToString("yyyymmddHHmmss");

            Random rnd = new Random();
            int r = rnd.Next(100000, 900000);
            int r2 = rnd.Next(500000, 800000);
            var rand = r.ToString() + r2.ToString();

            var sessID = senderscode + destcode + date + rand;
            return sessID;
        }


        [HttpPost]
        [Route("api/CustomerAPI/InterBankNameEnquiry")]
        public HttpResponseMessage InterBankNameEnquiry(HttpRequestMessage req)
        {
            
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            
            //string req = JsonConvert.SerializeObject(name);
            var doc = new XmlDocument();
            doc.Load(req.Content.ReadAsStreamAsync().Result);
            var r = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"+doc.InnerXml;
            
            //var r = doc.DocumentElement.OuterXml;
            
            long logID = logRequest(ip, r);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {


                var response = "";


                InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                   // NewNIP.processmessage S = new NewNIP.processmessage();
                    JaizNIBSSOthers.processmessage S = new JaizNIBSSOthers.processmessage();
                    
                    



                    var rs = "";

                    

                   // response = S.nameenquirysingleitem(r);
                    response = S.newNIPNameEnquiry(r);
                    
                    updateRequest(logID, response);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Retrieving Account Balance";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, response);
                }
                // string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/xml")
                };
                //return r;
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("NameEnquiryResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");
                

                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";
                
                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);



                var rs = "";

                rs = xml2.OuterXml;
                updateRequest(logID, rs);
                

                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/TransHistory")]
        public HttpResponseMessage TransHistory([FromBody] transhistory r)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            long logID = logRequest(ip, r.ToString());
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                transhistoryresponse resp = new transhistoryresponse();
                transdetails det = new transdetails();
                List<transdetails> Trans = new List<transdetails>();
                var svc = new InternetBankingAPI.JaizHelper();
                var transhistory = svc.GenerateStatement(r.accountNo, Convert.ToDateTime(r.startDate), true, Convert.ToDateTime(r.endDate),true);
                
                foreach (var item in transhistory)
                {
                    transdetails history = new transdetails();
                    det.Amount = Convert.ToDecimal(item.Amount);
                    det.Narration = item.Narration;
                    det.TransDate = Convert.ToDateTime(item.TransDate);
                    

                   //Trans.Add(det.ToString());
                    history=new transdetails { Amount = Convert.ToDecimal(item.Amount), Narration = item.Narration, TransDate = Convert.ToDateTime(item.TransDate),TransType=item.CrDr };
                    Trans.Add(history);
                    
                }
                               // resp.history = Trans;

                                string output = JsonConvert.SerializeObject(Trans);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");


                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);



                var rs = "";

                rs = xml2.OuterXml;
                updateRequest(logID, rs);


                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }


        }

        [HttpPost]
        [Route("api/CustomerAPI/LogReversal")]
        public HttpResponseMessage LogReversal([FromBody] logreversal r)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            //var doc = new XmlDocument();
            //doc.Load(req.Content.ReadAsStreamAsync().Result);
            //var r = doc.DocumentElement.OuterXml;
            long logID = logRequest(ip, r.ToString());
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {


                var response = "";


                InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                    // NewNIP.processmessage S = new NewNIP.processmessage();
                    JaizInternal.processmessage S = new JaizInternal.processmessage();



                    var rs = "";



                    response = S.FundTransferReversal(r.sessionID);

                    updateRequest(logID, response);
                    resp.responseCode = "00";
                    resp.responseMessage = "Successful";





                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, response);
                }
                // string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");


                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);



                var rs = "";

                rs = xml2.OuterXml;
                updateRequest(logID, rs);


                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/FundsTransferReversal")]
        public HttpResponseMessage FundsTransferReversal([FromBody] logreversal r)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            //var doc = new XmlDocument();
            //doc.Load(req.Content.ReadAsStreamAsync().Result);
            //var r = doc.DocumentElement.OuterXml;
            long logID = logRequest(ip, r.ToString());
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
           // if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {


                var response = "";


                InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                   // NewNIP.processmessage S = new NewNIP.processmessage();
                    JaizInternal.processmessage S = new JaizInternal.processmessage();



                    var rs = "";



                    response = S.FundTransferReversal(r.sessionID);

                    updateRequest(logID, response);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, response);
                }
                // string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");


                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);



                var rs = "";

                rs = xml2.OuterXml;
                updateRequest(logID, rs);


                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/NIPFundsTransfer")]
        public HttpResponseMessage NIPFundsTransfer(HttpRequestMessage req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            DateTime loginTime = DateTime.Now;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            var doc = new XmlDocument();
            doc.Load(req.Content.ReadAsStreamAsync().Result);
            var r = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"+doc.InnerXml;
           // var r = doc.DocumentElement.OuterXml;
            
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {


                var response = "";


                InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                    //NewNIP.processmessage S = new NewNIP.processmessage();
                    JaizNIBSSOthers.processmessage S = new JaizNIBSSOthers.processmessage();



                    var rs = "";


                    //response = S.FundTransfer(r);
                    response = S.newNIPFundTransfer(r);

                    
                    logRequestResponse2(ip, r, "NIPFundsTransfer", loginTime, response);


                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    string output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, r, "NIPFundsTransfer", loginTime, output);

                }
                // string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/xml")
                };
                //return r;
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");


                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);

                var rs = "";

                rs = xml2.OuterXml;

                logRequestResponse2(ip, r, "NIPFundsTransfer", loginTime, rs);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/GetBillItems")]
        public HttpResponseMessage GetBillerItems([FromBody] Items it)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(it);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                Itemsresponse resp = new Itemsresponse();

                try
                {

                    //BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();
                    //var ps = bs.QTPaymentItems(it.itemID);
                   
                    //List<Item> bll = new List<Item>();
                    //foreach (var i in ps)
                    //{

                    //    Item bl = new Item();
                    //    bl.Name = i.paymentitemname;
                    //    bl.PaymentCode = i.paymentCode;
                    //    bl.Amount = i.amount.ToString();
                    //    bl.ConvertedAmount = Convert.ToDecimal(i.amount);
                    //    bl.BillID = Convert.ToInt16(i.billerid);
                        

                        
                    //    bll.Add(bl);

                    //}
                    //convert this to billID
                    int bID = 0;
                    using (JaizOpenDigitalBankingEntities d = new JaizOpenDigitalBankingEntities())
                    {
                        var bDetails = d.Bills.FirstOrDefault(y => y.ID == it.itemID);
                        bID = Convert.ToInt16(bDetails.billerID);
                    }

                    List<Item> bl = null;
                    using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    {


                        bl = db.Items.Where(a => a.BillID == bID).ToList();
                    }

                    if (bl != null)
                    {
                        resp.Items= bl;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.Items = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription = "Error Retrieving Biller Items";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        public long insertItem(Decimal amount,string name,string consumerfield,string code,Decimal ConvertedAmount,int billID,string paymentCode)
        {
            long id = 0;
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                Items2 I = new Items2();
                I.Amount = amount.ToString();
                I.Name = name;
                I.ConsumerIdField = consumerfield;
                I.Code = code;
                I.ConvertedAmount = ConvertedAmount;
                I.BillID = billID;
                I.PaymentCode = paymentCode;
                I.PictureId = 1;
                I.IsVisible = true;
                db.Items2.Add(I);
                db.SaveChanges();
                id = I.ID;

            }
            return id;
        }
        [HttpPost]
        [Route("api/CustomerAPI/SpoolBillItems")]
        public HttpResponseMessage SpoolBillItems([FromBody] Items it)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(it);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            //if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                Itemsresponse resp = new Itemsresponse();

                try
                {

                    BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();
                    using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    {
                        var bbs = db.Bills2.ToList();
                        foreach (var x in bbs)
                        {
                            
                            var pss = bs.QTPaymentItems(Convert.ToInt16(x.billerID));
                            foreach (var xx in pss)
                            {

                                
                                //insert into items2
                                
                                insertItem(Convert.ToDecimal(xx.amount), xx.paymentitemname, xx.paymentitemname, xx.paymentCode, Convert.ToDecimal(xx.amount), Convert.ToInt16(xx.billerid), xx.paymentCode);
                            }
                        }
                    }
                    var ps = bs.QTPaymentItems(it.itemID);

                    List<Item> bll = new List<Item>();
                    foreach (var i in ps)
                    {

                        Item bl = new Item();
                        bl.Name = i.paymentitemname;
                        bl.PaymentCode = i.paymentCode;
                        bl.Amount = i.amount.ToString();
                        bl.ConvertedAmount = Convert.ToDecimal(i.amount);
                        bl.BillID = Convert.ToInt16(i.billerid);



                        bll.Add(bl);

                    }


                    //List<Item> bl = null;
                    //using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    //{


                    //    bl = db.Items.Where(a => a.BillID== it.itemID ).ToList();
                    //}

                    if (bll != null)
                    {
                        resp.Items = bll;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.Items = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription = "Error Retrieving Biller Items";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }


        public string GenerateRndNumber(int cnt)
        {
            string[] key2 = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            Random rand1 = new Random();
            string txt = "";
            for (int j = 0; j < cnt; j++)
                txt += key2[rand1.Next(0, 9)];
            return txt;
        }
        public decimal GetDecimal(object val, decimal otherwise = 0)
        {
            decimal k = otherwise;
            try
            {
                k = Convert.ToDecimal(val);
            }
            catch { }
            return k;
        }

        public string GetString(object val, string otherwise = "")
        {
            string k = otherwise;
            try
            {
                k = Convert.ToString(val);
            }
            catch { }
            return k;
        }

        [HttpPost]
        [Route("api/CustomerAPI/CustomerDetails")]
        public HttpResponseMessage CustomerDetails([FromBody] customerdetails req) 
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string request = JsonConvert.SerializeObject(req);



            long logID = logRequest(ip, request);
            //check if call is from permitted ip
            //  var permittedip = ConfigurationManager.AppSettings["permittedip"];
            // if (auth > 0 && (permittedip == ip))
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                customerdetailsresponse resp = new customerdetailsresponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                    InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();

                    // var custInfo = S.GetCustomerInfoByAccountNo(name.accountNo);
                    customerdetailsresponse custInfo = AccountDetailsNew(req.accountNo);

                    //if (!string.IsNullOrEmpty(custInfo.CustomerName))
                    //{
                    //    resp.accountName = custInfo.CustomerName;
                    //    resp.responseCode = "00";
                    //    resp.responseMessage = "Successful";
                    //    resp.phoneNo = custInfo.PhoneNo;
                    //}
                    //if (!string.IsNullOrEmpty(custInfo.accountname))
                    if (!string.IsNullOrEmpty(custInfo.accountname)||!string.IsNullOrEmpty(custInfo.firstname) || !string.IsNullOrEmpty(custInfo.secondname) || !string.IsNullOrEmpty(custInfo.lastname))
                    {
                        resp.accountname = custInfo.accountname;
                        resp.firstname = custInfo.firstname;
                        resp.secondname = custInfo.secondname;
                        resp.lastname = custInfo.lastname;
                        resp.bvn = custInfo.bvn;
                        resp.dob = custInfo.dob;
                        resp.phone = custInfo.phone;
                        resp.email = custInfo.email;
                        resp.balance = custInfo.balance;
                        resp.responseCode = "00";
                        resp.responseMessage = "Successful";
                        resp.cif = custInfo.cif;
                    }
                    else
                    {
                        resp.responseCode = "09";
                        resp.responseMessage = "Invalid Account Number";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/BillsPayment2")]
        public HttpResponseMessage BillsPayment2([FromBody] itembill Req)
        {
         var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(Req);
            long logID = logRequest(ip, req);
            //long logID = 1001;
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];

            //if (auth > 0 && (permittedip == ip))
            //if (auth > 0)
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                string xml = "";
                string pmtPrefix = ConfigurationManager.AppSettings["PamtPrefix"].ToString();
                string TerminalId = ConfigurationManager.AppSettings["TerminalId"].ToString();
                string BillerProfile3 = ConfigurationManager.AppSettings["BillerProfile3"].ToString();
                //prepare the data to be sent to Interswitch

                BillPaymentAdvice pq = new BillPaymentAdvice();
                decimal amt = GetDecimal(decimal.Parse(Req.Amount));
                amt = amt * 100;
                int convamt = Decimal.ToInt32(amt);
                pq.Amount = GetString(convamt);
                pq.PaymentCode = Req.PaymentCode;
                pq.CustomerMobile = Req.Phone;
                pq.CustomerEmail = Req.Email;
                ////check if it is vtu top up and then assign the mobile number into subscriberinfo
                //if (Req.SubscriberInfo1 == "VTUP")
                //{
                //    Req.SubscriberInfo1 = pq.CustomerMobile;
                //}
                pq.CustomerId = Req.CustomerID;
                pq.TerminalId = TerminalId;
                pq.RequestReference = pmtPrefix + BillerProfile3 + GenerateRndNumber(4);
                string Type = Req.PaymentType;
                if (Type == "VTUP")
                    Type = "Airtime";
                else
                    Type = "Bills";
                //Quickteller.QuickTellerServiceClient client = new Quickteller.QuickTellerServiceClient();
                Quickteller.QuickTellerService q = new Quickteller.QuickTellerService();
                //string url = "https://sandbox.interswitchng.com/QuickTellerService/QuickTeller.svc"; //ConfigurationManager.AppSettings["url"].ToString();
                string url = ConfigurationManager.AppSettings["quicktellerurl"].ToString();
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var rq = (HttpWebRequest)WebRequest.Create(url);
                rq.Method = "GET";
                IWebProxy proxyObject = new WebProxy("http://172.21.6.138:3128", true);
                q.Proxy = proxyObject;

                xml = q.SendBillPaymentAdvice(pq.GetMessage());

                xml = xml.Replace("u003c", "<");
                xml = xml.Replace("u003e", ">");
                responseclass resp = new responseclass();
                string output = "";
                if (xml != "")
                {
                    if (xml.ToLower().Contains("successful") || xml.ToLower().Contains("90000") || xml.ToLower().Contains("90009"))
                    {
                        
                        resp.responseCode = "00";
                        resp.responseMessage = "Successful";

                        string r = JsonConvert.SerializeObject(resp);
                        updateRequest(logID, r);
                        output = JsonConvert.SerializeObject(resp);

                       
                    }
                    if (!(xml.ToLower().Contains("90000") || xml.ToLower().Contains("90009") || xml.ToLower().Contains("900a0")))
                    {
                        //do reversal
                        
                        resp.responseCode = "99";
                        resp.responseMessage = "Unsuccessful";

                        string r = JsonConvert.SerializeObject(resp);
                        updateRequest(logID, r);
                        output = JsonConvert.SerializeObject(resp);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                }
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            
            
            }

   
    }
        public billspaymentresponse BillsPaymentByProxy(string username, string password, BillsPaymentRequest bills)
        {
            var url = ConfigurationManager.AppSettings["billspaymenturl"];

           
            string rs = JsonConvert.SerializeObject(bills);

            var content = new StringContent(rs, Encoding.UTF8, "text/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            client.DefaultRequestHeaders.Add("Username", username);
            client.DefaultRequestHeaders.Add("Password", password);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<BillsPaymentObject>(r.ToString());


            billspaymentresponse re = new billspaymentresponse();
            re.responseCode = resp.responseCode;
            re.responseMessage = resp.responseMessage;



            return re;
        }
        [HttpPost]
        [Route("api/CustomerAPI/BillsPayment")]
        public HttpResponseMessage BillsPayment([FromBody] itembill Req)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(Req);
            long logID = logRequest(ip, req);
            //long logID = 1001;
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];

            //if (auth > 0 && (permittedip == ip))
            //if (auth > 0)
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                string xml = "";
                //string pmtPrefix = ConfigurationManager.AppSettings["PamtPrefix"].ToString();
                //string TerminalId = ConfigurationManager.AppSettings["TerminalId"].ToString();
                //string BillerProfile3 = ConfigurationManager.AppSettings["BillerProfile3"].ToString();
                //prepare the data to be sent to Interswitch

                //"Email":"uthman4u2nv@yahoo.com",  "Phone":"08032518766",  "PaymentCode":"10401",  "Amount":"14705",  "CustomerID":"0000000001",  "PaymentType":"Bills"

                //BillPaymentAdvice pq = new BillPaymentAdvice();
                //decimal amt = GetDecimal(decimal.Parse(Req.Amount));
                //amt = amt * 100;
                //int convamt = Decimal.ToInt32(amt);
                //pq.Amount = GetString(convamt);
                //pq.PaymentCode = Req.PaymentCode;
                //pq.CustomerMobile = Req.Phone;
                //pq.CustomerEmail = Req.Email;
                ////check if it is vtu top up and then assign the mobile number into subscriberinfo
                //if (Req.SubscriberInfo1 == "VTUP")
                //{
                //    Req.SubscriberInfo1 = pq.CustomerMobile;
                //}
                //pq.CustomerId = Req.CustomerID;
               // pq.TerminalId = TerminalId;
                //pq.RequestReference = pmtPrefix + BillerProfile3 + GenerateRndNumber(4);

                //"Email":"uthman4u2nv@yahoo.com",  "Phone":"08032518766",  "PaymentCode":"10401",  "Amount":"14705",  "CustomerID":"0000000001",  "PaymentType":"Bills"
                var e = Req.Email;
                var p = Req.Phone;
                var email = "";
                var phone = "";
                if (string.IsNullOrEmpty(e))
                {
                    email = ConfigurationManager.AppSettings["billspaymentemail"];
                }
                else
                {
                    email = Req.Email;
                }
                if (string.IsNullOrEmpty(p))
                {
                    phone = ConfigurationManager.AppSettings["billspaymentphone"]; ;
                }
                else
                {
                    phone = Req.Phone;
                }
                BillsPaymentRequest b=new BillsPaymentRequest();
                b.Email=email;
                b.Phone=phone;
                b.PaymentCode=Req.PaymentCode;
                b.Amount=Req.Amount;
                b.CustomerID=Req.CustomerID;
                b.PaymentType=Req.PaymentType;


                BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();
               // BillsPaymentService.BillPaymentAdviceResponse rr = new BillsPaymentService.BillPaymentAdviceResponse();
                

                var ls = bs.QTSendBillPaymentAdvice("", Req.PaymentCode, Req.CustomerID, Req.Phone, Req.Email, Req.Amount);
                responseclass resp = new responseclass();
                var output = "";
                var response = ls.responseCode;
                //if ((ResponseCode.Contains("90000") || ResponseCode.Contains("90009") || ResponseCode.Contains("900a0"))) 
                if (response.Contains("90000") || response.Contains("90009") || response.Contains("900a0"))
                {

                    resp.responseCode = "00";
                    resp.responseMessage = "Successful";
                }
                else
                {
                    resp.responseCode = "99";
                    resp.responseMessage = "Unsuccessful";
                }
                
                
                
                //billspaymentresponse resp=BillsPaymentByProxy(username,password,b);
                
                output = JsonConvert.SerializeObject(resp);
                //string Type = Req.PaymentType;
                //if (Type == "VTUP")
                //    Type = "Airtime";
                //else
                //    Type = "Bills";
                ////Quickteller.QuickTellerServiceClient client = new Quickteller.QuickTellerServiceClient();
                //Quickteller.QuickTellerService q = new Quickteller.QuickTellerService();
                ////string url = "https://sandbox.interswitchng.com/QuickTellerService/QuickTeller.svc"; //ConfigurationManager.AppSettings["url"].ToString();
                //string url = ConfigurationManager.AppSettings["quicktellerurl"].ToString();
                //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //var rq = (HttpWebRequest)WebRequest.Create(url);
                //rq.Method = "GET";
                //IWebProxy proxyObject = new WebProxy("http://172.21.6.138:3128", true);
                //q.Proxy = proxyObject;

                //xml = q.SendBillPaymentAdvice(pq.GetMessage());

                //xml = xml.Replace("u003c", "<");
                //xml = xml.Replace("u003e", ">");
                //responseclass resp = new responseclass();
                //string output = "";
                //if (xml != "")
                //{
                //    if (xml.ToLower().Contains("successful") || xml.ToLower().Contains("90000") || xml.ToLower().Contains("90009"))
                //    {

                //        resp.responseCode = "00";
                //        resp.responseMessage = "Successful";

                //        string r = JsonConvert.SerializeObject(resp);
                //        updateRequest(logID, r);
                //        output = JsonConvert.SerializeObject(resp);


                //    }
                //    if (!(xml.ToLower().Contains("90000") || xml.ToLower().Contains("90009") || xml.ToLower().Contains("900a0")))
                //    {
                //        //do reversal

                //        resp.responseCode = "99";
                //        resp.responseMessage = "Unsuccessful";

                //        string r = JsonConvert.SerializeObject(resp);
                //        updateRequest(logID, r);
                //        output = JsonConvert.SerializeObject(resp);

                //        return new HttpResponseMessage()
                //        {
                //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                //        };
                //    }
                //}
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };


            }


        }
        private bool CheckPermittedIP(string ip)
        {
            bool flag;
            try
            {
                flag = (!ConfigurationManager.AppSettings["permittedip"].ToString().Contains(ip) ? false : true);
            }
            catch (Exception exception)
            {
                flag = false;
            }
            return flag;
        }

       
        [HttpPost]
        [Route("api/CustomerAPI/GetBillerPaymentItems")]
        public HttpResponseMessage GetBillerPaymentItems([FromBody] billerItems bill)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(bill);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
          //  var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            //if (auth > 0 && (permittedip == ip))
            if (auth > 0 && (permittedip))
            {

                billeritemsresponse resp = new billeritemsresponse();

                try
                {
                    List<Bill> bl=null;
                    using(JaizOpenDigitalBankingEntities db=new JaizOpenDigitalBankingEntities()){


                        bl = db.Bills.Where(a => a.QuickTellerCategoryId == bill.billerID).ToList();
                    }

                    if (bl != null)
                    {
                        resp.BillItems = bl;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.BillItems = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription= "Error Encountered";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/GetBiller")]
        public HttpResponseMessage GetBiller([FromBody] biller bill)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(bill);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip=CheckPermittedIP(ip);
           // if (auth > 0 && (permittedip))
            if (auth > 0 )
            {

                billeritemsresponse resp = new billeritemsresponse();

                try
                {
                    BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();

                     var ls = bs.QTBillersByCategorie(bill.billerID);
                     List<Bill> bll = new List<Bill>();
                    foreach (var i in ls)
                    {
                        
                        Bill bl = new Bill();
                        bl.Name = i.billername;
                        bl.Surcharge = i.surcharge;
                        bl.QuickTellerCategoryId = Convert.ToInt16(i.categorysid);
                        bl.ShortName = i.shortName;
                        bl.billerID = Convert.ToInt16(i.billerid);
                        bl.ID = Convert.ToInt16(i.billerid);
                        bll.Add(bl);
                       
                    }
                   
                    
                    
                    //using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    //{


                    //    bl = db.Bills.Where(a => a.QuickTellerCategoryId == bill.billerID).ToList();
                    //}

                    if (bll != null)
                    {
                        resp.BillItems = bll;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.BillItems = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        public long insertBiller(int billerID,string shortname, int QuickTellerID, string name, string narration, string surcharge, string customerfieldlabel)
        {
            long id = 0;
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                Bills2 b = new Bills2();
                b.ShortName = shortname;
                b.QuickTellerCategoryId = QuickTellerID;
                b.Name = narration;
                b.Narration = narration;
                b.LogoUrl = "";
                b.Surcharge = surcharge;
                b.Visible = true;
                b.CustomerFieldLabel = customerfieldlabel;
                b.billerID = billerID;
                db.Bills2.Add(b);
                db.SaveChanges();
                id = b.ID;
            }
            return id;
        }
        [HttpPost]
        [Route("api/CustomerAPI/SpoolBiller")]
        public HttpResponseMessage SpoolBiller([FromBody] biller bill)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(bill);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            // if (auth > 0 && (permittedip))
            if (auth > 0)
            {

                billeritemsresponse resp = new billeritemsresponse();

                try
                {
                    BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();
                    using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    {
                        var bList = db.QuickTellerCategories2.ToList();
                        foreach (var bb in bList)
                        {
                            //call the quick
                            var fs = bs.QTBillersByCategorie(bb.QuickTellerCategoryId);
                            foreach (var x in fs)
                            {
                                
                                //insert into bills2
                                
                                insertBiller(Convert.ToInt16(x.billerid),x.shortName, Convert.ToInt16(x.categorysid), x.categorydescription, x.billername, x.surcharge, x.categoryname);
                            }
                            
                        }
                        
                    }
                    var ls = bs.QTBillersByCategorie(bill.billerID);
                    List<Bill> bll = new List<Bill>();
                    foreach (var i in ls)
                    {
                        

                        Bill bl = new Bill();
                        bl.Name = i.billername;
                        bl.Surcharge = i.surcharge;
                        bl.QuickTellerCategoryId = Convert.ToInt16(i.categorysid);
                        bl.ShortName = i.shortName;
                        bl.billerID = Convert.ToInt16(i.billerid);
                        bl.ID = Convert.ToInt16(i.billerid);
                        bll.Add(bl);

                    }



                    //using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    //{


                    //    bl = db.Bills.Where(a => a.QuickTellerCategoryId == bill.billerID).ToList();
                    //}

                    if (bll != null)
                    {
                        resp.BillItems = bll;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.BillItems = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        public string GetBVN(string cif)
        {

            OracleConnection conn = DBUtils.GetDBConnection();
            conn.Open();

            //string sql = "SELECT a.LONG_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance, b.tel as Phone FROM imal_trn.amf a,imal_trn.cif b  WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
            string sql = "SELECT biometricid FROM imal.biodata  WHERE customerid=:cif";
            // Create command.
            OracleCommand cmd = new OracleCommand();

            // Set connection for command.
            cmd.Connection = conn;
            cmd.CommandText = sql;
            // cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

            OracleParameter cifNo = new OracleParameter("cif", OracleDbType.Varchar2, 40);
            cifNo.Direction = ParameterDirection.Input;
            cifNo.Value = cif;

            cmd.Parameters.Add(cifNo);
            cmd.Connection = conn;
            string bvn = "";
            
            customerdetailsresponse resp = new customerdetailsresponse();

            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {



                    if (reader.HasRows)
                    {
                        int sn = 0;

                        while (reader.Read())
                        {
                            sn++;

                        
                            bvn = reader["biometricid"].ToString(); ;
                         

                        }
                    }
                }

                

                return bvn;
            }
            catch (OracleException ex)
            {
                return null;
            }

        }
        //public customerdetailsresponse AccountDetailsNew(string AccountNo)
        //{

        //    OracleConnection conn = DBUtils.GetDBConnection();
        //    conn.Open();

        //    //string sql = "SELECT a.LONG_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance, b.tel as Phone FROM imal_trn.amf a,imal_trn.cif b  WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
        //    string sql = "SELECT b.TEL as Phone, b.CIF_NO, b.AUTH_NAME,b.FIRST_NAME_ENG,b.SEC_NAME_ENG,b.LAST_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance,b.description as Email,b.birth_date as dob FROM imal.amf a,imal.cif b WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
        //    // Create command.
        //    OracleCommand cmd = new OracleCommand();

        //    // Set connection for command.
        //    cmd.Connection = conn;
        //    cmd.CommandText = sql;
        //    // cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

        //    OracleParameter AccNo = new OracleParameter("acctno", OracleDbType.Varchar2, 40);
        //    AccNo.Direction = ParameterDirection.Input;
        //    AccNo.Value = AccountNo;

        //    cmd.Parameters.Add(AccNo);
        //    cmd.Connection = conn;
        //    string AccountName = "";
        //    string firstname = "";
        //    string secondname = "";
        //    string lastname = "";
        //    string Balance = "";
        //    string Phone = "";
        //    string bvn = "";
        //    string email = "";
        //    string dob = "";
        //    string cif = "";
        //    string tel = "";

        //    customerdetailsresponse resp = new customerdetailsresponse();

        //    try
        //    {
        //        using (DbDataReader reader = cmd.ExecuteReader())
        //        {



        //            if (reader.HasRows)
        //            {
        //                int sn = 0;

        //                while (reader.Read())
        //                {
        //                    sn++;

        //                    AccountName = reader["AUTH_NAME"].ToString();
        //                    Balance = reader["CustomerBalance"].ToString();
        //                    tel = reader["Phone"].ToString();
        //                    firstname = reader["FIRST_NAME_ENG"].ToString();
        //                    secondname = reader["SEC_NAME_ENG"].ToString(); 
        //                    lastname = reader["LAST_NAME_ENG"].ToString();                             
        //                    email = reader["Email"].ToString(); 
        //                    dob = reader["dob"].ToString();
        //                    cif = reader["CIF_NO"].ToString();
                            


        //                }
        //            }
        //        }

        //        resp.balance = Balance;
        //        resp.accountname = AccountName;
        //        resp.firstname = firstname;
        //        resp.secondname = secondname;
        //        resp.lastname = lastname;
        //        resp.phone = tel;
        //        resp.bvn = GetBVN(cif);
        //        resp.dob = dob;
        //        resp.email = email;
        //        resp.cif = cif;

        //        return resp;
        //    }
        //    catch (OracleException ex)
        //    {
        //        return null;
        //    }

        //}*/
        public AccountDetailsResponse AccountDetailsByProxy2(string AccountNo, string username, string password)
        {
            var url = ConfigurationManager.AppSettings["localnameenquiryurl"];

            account ac = new account();
            ac.accountNo = AccountNo;
            string rs = JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "text/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            client.DefaultRequestHeaders.Add("Username", username);
            client.DefaultRequestHeaders.Add("Password", password);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<Rootobject>(r.ToString());


            AccountDetailsResponse re = new AccountDetailsResponse();
            re.balance = resp.balance;
            re.name = resp.accountName;
            re.phone = resp.phoneNo;


            // string response = await result.Content.ReadAsStringAsync();


            return re;
        }
        [HttpPost]
        [Route("api/CustomerAPI/GetBranches")]
        public HttpResponseMessage GetBranches()
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {

                auth = authenticateUsers(username, password);

            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            branchrequest br = new branchrequest();
            br.branchMethod = "Branches Method Called";
            string req = JsonConvert.SerializeObject(br.ToString());
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                InternetBankingAPI.JaizHelper svc=new InternetBankingAPI.JaizHelper();
                try{
                var l=svc.GetBranchList();
                        string output = JsonConvert.SerializeObject(l);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    response resp = new response();
                    resp.ResponseCode = "96";
                    resp.ResponseDescription = "Error Retrieving Account Lists";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);


                    string output = JsonConvert.SerializeObject(resp);

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
       

        public  accountsbyphonenoresponse CustomerAccounts(string phoneNo, string username, string password)
        {
            var url = ConfigurationManager.AppSettings["GetAccountsByPhoneNoUrl"];

            phone p = new phone();
            p.phoneNo = phoneNo; 
            string rs = JsonConvert.SerializeObject(p);

            var content = new StringContent(rs, Encoding.UTF8, "text/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            client.DefaultRequestHeaders.Add("Username", username);
            client.DefaultRequestHeaders.Add("Password", password);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<ResponseObject>(r.ToString());


            accountsbyphonenoresponse re = new accountsbyphonenoresponse();
            re.AccountNumbers = resp.AccountNumbers;
            re.ResponseCode = resp.responseCode;
            re.ResponseDescription = resp.responseMessage;


            // string response = await result.Content.ReadAsStringAsync();


            return re;
        }
        
        public AccountDetailsResponse AccountDetailsByProxy(string AccountNo,string username,string password)
        {
            var url = ConfigurationManager.AppSettings["accountbalanceurl"];
            
            account ac = new account();
            ac.accountNo = AccountNo;
            string rs= JsonConvert.SerializeObject(ac);

            var content = new StringContent(rs, Encoding.UTF8, "text/json");
            var client = new HttpClient();

            HttpResponseMessage result = null;
            client.DefaultRequestHeaders.Add("Username", username);
            client.DefaultRequestHeaders.Add("Password", password);
            result = client.PostAsync(url, content).Result;
            //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
            var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
            var resp = JsonConvert.DeserializeObject<Rootobject>(r.ToString());

            
            AccountDetailsResponse re = new AccountDetailsResponse();
            re.balance = resp.balance;
            re.name = resp.accountName;
            re.phone = resp.phoneNo;


           // string response = await result.Content.ReadAsStringAsync();


            return re;
        }

        public class LinkingObject
        {
            public string responseCode { get; set; }
            public string responseMessage { get; set; }
        }
        public class BillsPaymentObject
        {
            public string responseCode { get; set; }
            public string responseMessage { get; set; }
        }
        public class ResponseObject
        {
            public List<string> AccountNumbers { get; set; }
            public string responseCode { get; set; }
            public string responseMessage { get; set; }
        }
            
        public class Rootobject
        {
            public string accountName { get; set; }
            public string phoneNo { get; set; }
            public string balance { get; set; }
            public string cif { get; set; }
            public string responseCode { get; set; }
            public string responseMessage { get; set; }
        }

        public AccountDetailsResponse AccountDetails(string AccountNo)
        {
            
             OracleConnection conn = DBUtils.GetDBConnection();
                conn.Open();

       string sql="SELECT b.LONG_NAME_ENG,(-1*a.cv_avail_bal) as CustomerBalance, b.tel as Phone FROM imal.amf a,imal.cif b  WHERE a.cif_sub_no=b.cif_no AND a.additional_reference=:acctno";
            // Create command.
                OracleCommand cmd = new OracleCommand();

                // Set connection for command.
                cmd.Connection = conn;
                cmd.CommandText = sql;
               // cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

                OracleParameter AccNo = new OracleParameter("acctno", OracleDbType.Varchar2, 40);
                AccNo.Direction = ParameterDirection.Input;
                AccNo.Value = AccountNo;

                cmd.Parameters.Add(AccNo);
                cmd.Connection = conn;
                string AccountName="";
                string Balance="";
                string Phone = "";
            AccountDetailsResponse resp=new AccountDetailsResponse();

            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {


                    
                    if (reader.HasRows)
                    {
                        int sn = 0;

                        while (reader.Read())
                        {
                            sn++;

                            AccountName = reader["LONG_NAME_ENG"].ToString();
                            Balance = reader["CustomerBalance"].ToString();
                            Phone = reader["Phone"].ToString();
                        }
                    }
                }
                
                resp.balance = Balance;
                resp.name = AccountName;
                resp.phone = Phone;

                return resp;
            }
            catch (OracleException ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error: Accessing Customer Account Balanace",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                return null;
            }
               
        }
        //public async void GetResult(string phoneNo,string accountNo,string card)
        //{
        //}
        //protected async Task<string> AsyncAwait_GetSomeDataAsync()
        //{
        //    var httpClient = new HttpClient();

        //    var result = await httpClient.GetAsync("http://stackoverflow.com", HttpCompletionOption.ResponseHeadersRead);

        //    return result.Content.Headers.ToString();
        //}
        [HttpPost]
        [Route("api/CustomerAPI/ValidateCard")]
        public HttpResponseMessage ValidateCard([FromBody] validatecustomer details)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(details);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {
                var phoneNo = details.phoneNo;
                var accountNo = details.accountNo;
                var card = details.carddetails;

                var rs = " <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><SearchResult><ResultStatus>00</ResultStatus>";
                var content = new StringContent(rs, Encoding.UTF8, "text/xml");
                var client = new HttpClient();
                try{
                    //HttpResponseMessage result = null;
                   

               // result.EnsureSuccessStatusCode();

                // On success, return sign in results from the server response packet
                //var responseContent = await response.Content.ReadAsStringAsync();

                    //
                    //var result = await httpClient.GetAsync("http://stackoverflow.com", HttpCompletionOption.ResponseHeadersRead);

                    //    return result.Content.Headers.ToString();
                    //System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                    //var url = ConfigurationManager.AppSettings["fepurl"] + phoneNo + "/" + accountNo + "/" + card;

                    //var result = client.GetAsync(url).Result;

                    validatecustomerresponse response = new validatecustomerresponse();
                    if (phoneNo == "08032518766" && accountNo == "0003124107" && card == "267447")
                    {
                        response.responseCode = "00";
                        response.responseMessage = "Successful";
                    }
                    else
                    {
                        response.responseCode = "96";
                        response.responseMessage = "Invalid";
                    }
                    updateRequest(logID, response.ToString());
                    string output = JsonConvert.SerializeObject(response);
                    //XmlDocument xml2 = new XmlDocument();
                    //xml2.LoadXml(result.ToString());

                    //XmlNodeList xnList2 = xml2.SelectNodes("Response");



                    //foreach (XmlNode xn in xnList2)
                    //{
                    //    string ResponseCode = xn["ResponseCode"].InnerText;
                    //    string ResponseMessage = xn["ResponseMessage"].InnerText;
                    //    string MiddleName = xn["MiddleName"].InnerText;
                    //    string LastName = xn["LastName"].InnerText;
                    //    DateTime dob = Convert.ToDateTime(xn["DateOfBirth"].InnerText);
                    //    string PhoneNumber = xn["PhoneNumber"].InnerText;
                    //}


                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    
            }
                catch(Exception ex){
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(ex.Message, System.Text.Encoding.UTF8, "application/xml")
                    };
                }

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/GetAccountsByPhoneNo")]
        public HttpResponseMessage GetAccountsByPhoneNo([FromBody] accountsbyphoneno account)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string req = JsonConvert.SerializeObject(account.phoneNo);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                accountsbyphonenoresponse resp = CustomerAccounts(account.phoneNo,username,password);
                

                        string output = JsonConvert.SerializeObject(resp);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                //OracleConnection conn = DBUtils.GetDBConnection();
                //conn.Open();

                ////string sql = "Select * FROM  WHERE BIOMETRICID=:BVN AND CUSTOMERID=:cif";
                ////string sql = "SELECT b.additional_reference FROM imal_trn.cif_address a,imal_trn.amf b  WHERE a.cif_no=b.cif_sub_no AND b.GL_CODE LIKE '21%' AND a.tel=:phone";
                //string sql = "SELECT b.additional_reference FROM imal.cif a,imal.amf b  WHERE a.cif_no=b.cif_sub_no AND b.GL_CODE LIKE '21%' AND a.tel=:phone";

                //// Create command.
                //OracleCommand cmd = new OracleCommand();

                //// Set connection for command.
                //cmd.Connection = conn;
                //cmd.CommandText = sql;
                ////cmd.CommandText = "SELECT COUNT(*) FROM BIODATA";

                //OracleParameter PhoneNo = new OracleParameter("phone", OracleDbType.NVarchar2, 40);
                //PhoneNo.Direction = ParameterDirection.Input;
                //PhoneNo.Value = account.phoneNo;

                //cmd.Parameters.Add(PhoneNo);
                //cmd.Connection = conn;
                //var accounts = "";
                ////response = "CON Select your Account No \n";
                //List<string> AccountList = new List<string>();
                //// Add items using Add method.
                //accountsbyphonenoresponse resp = new accountsbyphonenoresponse();
                
                //try
                //{
                //    using (DbDataReader reader = cmd.ExecuteReader())
                //    {


                //        //string BIOMETRICID = "";
                //        if (reader.HasRows)
                //        {
                //            int sn = 0;

                //            while (reader.Read())
                //            {
                //                sn++;

                //                accounts = reader["ADDITIONAL_REFERENCE"].ToString();

                //               // accountlist += sn + " " + accounts + "\n";
                //                AccountList.Add(accounts);

                //                //BIOMETRICID = reader["BIOMETRICID"].ToString();
                //            }
                //        }
                //        if(AccountList.Count() > 0){
                //            resp.AccountNumbers = AccountList;
                //        resp.ResponseCode = "00";
                //        resp.ResponseDescription = "Successful";
                //        }else{
                            
                //        resp.ResponseCode = "99";
                //        resp.ResponseDescription = "Invalid PhoneNo or phone record not found";
                //        }
                        
                //        string output = JsonConvert.SerializeObject(resp);

                //        return new HttpResponseMessage()
                //        {
                //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                //        };
                //    }
                //}
                //catch (Exception ex)
                //{
                //    var err2 = new LogUtility.Error()
                //    {
                //        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                //        ErrorTime = DateTime.Now,
                //        ModulePointer = "Error",
                //        StackTrace = ex.StackTrace

                //    };
                //    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    
                //    resp.ResponseCode = "96";
                //    resp.ResponseDescription = "Error Retrieving Account Lists";

                //    string r = JsonConvert.SerializeObject(resp);
                //    updateRequest(logID, r);

                    
                //    string output = JsonConvert.SerializeObject(resp);

                //    return new HttpResponseMessage()
                //    {
                //        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                //    };
                //}

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/TestBVN")]
        public HttpResponseMessage TestBVN()
        {
           bvnvalidationresponse resp= getCustomerBVN("22162611742");
           var lastname = resp.lastname;
           var firstname = resp.firstname;

           string output = JsonConvert.SerializeObject(resp);
           return new HttpResponseMessage()
           {
               Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
           };
            
        }
        
        public bvnvalidationresponse getCustomerBVN(string bvn)
        {
            XmlDocument xml22 = new XmlDocument();
            XmlElement root = xml22.CreateElement("BVNValidation");
            xml22.AppendChild(root);

            XmlElement BVNNno = xml22.CreateElement("bvn");
            

            BVNNno.InnerText = bvn;
            root.AppendChild(BVNNno);
            var rs = xml22.OuterXml;
            rs = xml22.OuterXml;

            var content = new StringContent(rs, Encoding.UTF8, "text/xml");
            HttpResponseMessage result = null;
            var client= new HttpClient();
            var url = "https://activate.jaizbankplc.com:8181/api/CustomerAPI/BVNValidation";
            result = client.PostAsync(url, content).Result;

            var doc2 = new XmlDocument();
            doc2.Load(result.Content.ReadAsStreamAsync().Result);
            var rr = doc2.DocumentElement.OuterXml;

            var firstname = "";
            var lastname = "";

            XmlDocument xm = new XmlDocument();
            xm.LoadXml(rr);
            XmlNodeList xnList2=xm.SelectNodes("BvnSearchResult");
        //     <?xml version="1.0" encoding="UTF-8" standalone="yes"?><SearchResult><ResultStatus>00</ResultStatus><BvnSearchResult><Bvn>22152678621</Bvn><FirstName>MORADEUN</FirstName><MiddleName>ADESOLA</MiddleName><LastName>OLABISI</LastName><DateOfBirth>04-MAR-73</DateOfBirth><PhoneNumber>08036868348</PhoneNumber><RegistrationDate>16-NOV-14</RegistrationDate><EnrollmentBank>033</EnrollmentBank><EnrollmentBranch>IJEBU ODE</EnrollmentBranch></BvnSearchResult></SearchResult>

        //<?xml version="1.0" encoding="UTF-8" standalone="yes"?><SearchResult><ResultStatus>00</ResultStatus><BvnSearchResult><Bvn>22152678579</Bvn><FirstName>TAOFEEK</FirstName><MiddleName>OLANRENWAJU</MiddleName><LastName>ADESINA</LastName><DateOfBirth>02-JUL-64</DateOfBirth><PhoneNumber>08023371842</PhoneNumber><RegistrationDate>16-NOV-14</RegistrationDate><EnrollmentBank>011</EnrollmentBank><EnrollmentBranch>Shomolu</EnrollmentBranch><ImageBase64></ImageBase64></BvnSearchResult></SearchResult>



            foreach (XmlNode xn in xnList2)
            {

                firstname = xn["FirstName"].InnerText;

                lastname = xn["LastName"].InnerText;
            }
            bvnvalidationresponse resp = new bvnvalidationresponse();
            resp.lastname = lastname;
            resp.firstname = firstname;
            return resp;
        }
        [HttpPost]
        [Route("api/CustomerAPI/CategoryBillers")]
        public HttpResponseMessage CategoryBillers()
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            CategoryBiller b = new CategoryBiller();
            b.categorybillerrequest = "Get Categories and Billers";
            string req = JsonConvert.SerializeObject(b);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            //var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                CategoryBillerResponse resp = new CategoryBillerResponse();

                try
                {
                    List<CategoryBill> bl = null;
                    using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    {
                        
                        bl = db.CategoryBills.ToList();
                    }

                    if (bl != null)
                    {
                        resp.Billers = bl;
                        resp.responseCode = "00";
                        resp.responseDescription = "Successful";
                    }
                    else
                    {
                        resp.Billers = null;
                        resp.responseCode = "09";
                        resp.responseDescription = "Successful";
                    }

                    //update log 
                    string r = JsonConvert.SerializeObject(resp.ToString());
                    updateRequest(logID, r);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseDescription = "Error Retrieving Account Balance";

                    string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, r);
                }
                string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
                //return r;
            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r);
                string output = JsonConvert.SerializeObject(resp);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
        [HttpPost]
        [Route("api/CustomerAPI/GetBillerCategory")]
        public HttpResponseMessage GetBillerCategory()
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            bankquery b = new bankquery();
            b.action = "Biller Category Method Called";
            string req = JsonConvert.SerializeObject(b);
            long logID = logRequest(ip, req);
            //check if call is from permitted ip
            var permittedip = ConfigurationManager.AppSettings["permittedip"];
            string output = "";
            //if (auth > 0 && (permittedip == ip))
            if (auth > 0)
            {
                
                try
                {
                    BillsPaymentService.JaizHelper bb = new BillsPaymentService.JaizHelper();
                    var ls=bb.QTBillerCategorie();
                    
                    List<QuickTellerCategory> lss= new List<QuickTellerCategory>();
                    
                    foreach (var i in ls)
                    {
                        QuickTellerCategory l = new QuickTellerCategory();
                        l.Name = i.categoryname;
                        l.QuickTellerCategoryId = Convert.ToInt16(i.categoryid);
                        l.Description = i.categorydescription;
                        lss.Add(l);
                    }

                    //using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    //{
                    //    List<QuickTellerCategory> ls = db.QuickTellerCategories.ToList();


                    //}
                    string rr = JsonConvert.SerializeObject(lss);
                    updateRequest(logID, rr);
                    output = JsonConvert.SerializeObject(lss);


                }
                catch (Exception ex)
                {
                    responseclass resp = new responseclass();
                    var err2 = new LogUtility.Error()
                    {

                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, resp.ToString());

                }

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r2 = JsonConvert.SerializeObject(resp);
                updateRequest(logID, r2);
                output = JsonConvert.SerializeObject(resp);

               
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }
        public long insertCategories(long CatID,string name,string description)
        {
            long id = 0;
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                QuickTellerCategories2 q = new QuickTellerCategories2();
                q.Description = description;
                q.Name = name;
                q.QuickTellerCategoryId = Convert.ToInt32(CatID);
                q.Visible = true;
                q.PictureUrl = "";
                db.QuickTellerCategories2.Add(q);
                db.SaveChanges();
                id = q.ID;
            }
            return id;
        }

        [HttpPost]
        [Route("api/CustomerAPI/SpoolBillerCategory")]
        public HttpResponseMessage SpoolBillerCategory()
        {
            var username = "";
            var password = "";
            int auth = 0;
            DateTime loginTime = DateTime.Now;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            bankquery b = new bankquery();
            b.action = "Biller Category Method Called";
            string req = JsonConvert.SerializeObject(b);
            //long logID = logRequest(ip, req);
            //check if call is from permitted ip
            var permittedip = ConfigurationManager.AppSettings["permittedip"];
            string output = "";
            //if (auth > 0 && (permittedip == ip))
            if (auth > 0)
            {

                try
                {
                    BillsPaymentService.JaizHelper bb = new BillsPaymentService.JaizHelper();
                    var ls = bb.QTBillerCategorie();

                    List<QuickTellerCategory> lss = new List<QuickTellerCategory>();

                    foreach (var i in ls)
                    {
                        QuickTellerCategory l = new QuickTellerCategory();
                        
                        //insert into quicktellercategory2
                        insertCategories(i.categoryid, i.categoryname, i.categorydescription);

                        l.Name = i.categoryname;
                        l.QuickTellerCategoryId = Convert.ToInt16(i.categoryid);
                        l.Description = i.categorydescription;
                        lss.Add(l);
                    }

                    //using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
                    //{
                    //    List<QuickTellerCategory> ls = db.QuickTellerCategories.ToList();


                    //}
                    string rr = JsonConvert.SerializeObject(lss);
                    
                    output = JsonConvert.SerializeObject(lss);

                    logRequestResponse(ip, req, loginTime, output, "SpoolBillerCategory");
                }
                catch (Exception ex)
                {
                    responseclass resp = new responseclass();
                    var err2 = new LogUtility.Error()
                    {

                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    string r = JsonConvert.SerializeObject(resp);
                    logRequestResponse(ip, req, loginTime, r, "SpoolBillerCategory");

                }

            }
            else
            {
                responseclass resp = new responseclass();
                resp.responseCode = "99";
                resp.responseMessage = "Invalid Username or Password";

                string r2 = JsonConvert.SerializeObject(resp);
                output = JsonConvert.SerializeObject(resp);

                logRequestResponse(ip, req, loginTime, output, "SpoolBillerCategory");
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }



        [HttpPost]
        [Route("api/CustomerAPI/Banks")]
        public HttpResponseMessage Banks()
        {
             var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            bankquery b = new bankquery();
            b.action = "Bank Method Called";
            string req = JsonConvert.SerializeObject(b);
            long logID = logRequest(ip,req);
            //check if call is from permitted ip
           // var permittedip = ConfigurationManager.AppSettings["permittedip"];
            bool permittedip = CheckPermittedIP(ip);
            //if (auth > 0 && (permittedip))
            //{
                string output = "";
                string rs = "";
                string url = ConfigurationManager.AppSettings["banklisturl"].ToString();
                var content = new StringContent(rs, Encoding.UTF8, "text/json");
                var client = new HttpClient();

                var r = "";
           
                try
                {
                    //using(JaizODBEntities db=new JaizODBEntities()){
                    //    List<TBFIList> ls = db.TBFILists.ToList();
                    //    string rr = JsonConvert.SerializeObject(ls);
                    //    updateRequest(logID, rr);
                    //    output = JsonConvert.SerializeObject(ls);
                        
                    //}
                    HttpResponseMessage result = null;
                    client.DefaultRequestHeaders.Add("Username", username);
                    client.DefaultRequestHeaders.Add("Password", password);
                    result = client.PostAsync(url, content).Result;
                    //var r = result.Content.ReadAsStringAsync().Result.Replace("\\", "").Trim(new char[1] { '"' });
                     r = result.Content.ReadAsStringAsync().Result.Replace("\\", "");
                    
                    
                }
                catch (Exception ex)
                {
                    responseclass resp = new responseclass();
                    var err2 = new LogUtility.Error()
                    {
                        
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, resp.ToString());

                } 
                return new HttpResponseMessage()
                {
                    Content = new StringContent(r.ToString(), System.Text.Encoding.UTF8, "application/json")
                };
           
        }
        

        [HttpPost]
        [Route("api/CustomerAPI/TSQ")]
        public HttpResponseMessage TSQ(HttpRequestMessage req)
        {
            var username = "";
            var password = "";
            int auth = 0;

            var re = Request;
            var headers = re.Headers;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();

            //string req = JsonConvert.SerializeObject(name);
            var doc = new XmlDocument();
            doc.Load(req.Content.ReadAsStreamAsync().Result);
            var r = doc.DocumentElement.OuterXml;
            long logID = logRequest(ip, r);
            //check if call is from permitted ip
            var permittedip = ConfigurationManager.AppSettings["permittedip"];
            if (auth > 0 && (permittedip == ip))
            {


                var response = "";


                InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

                try
                {
                    //JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                    NewNIP.processmessage S = new NewNIP.processmessage();



                    var rs = "";



                    response = S.newNIPTSQ(r);
                    

                    updateRequest(logID, response);




                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    resp.responseCode = "96";
                    resp.responseMessage = "Error Carrying out operation";

                    //string r = JsonConvert.SerializeObject(resp);
                    updateRequest(logID, response);
                }
                // string output = JsonConvert.SerializeObject(resp);
                // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(response, System.Text.Encoding.UTF8, "application/xml")
                };
                //return r;
            }
            else
            {
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTResponse");
                xml2.AppendChild(root);

                XmlElement ResponseCode = xml2.CreateElement("responseCode");
                XmlElement ResponseMessage = xml2.CreateElement("responseMessage");


                ResponseMessage.InnerText = "Authentication Failed";
                ResponseCode.InnerText = "00";

                root.AppendChild(ResponseCode);
                root.AppendChild(ResponseMessage);



                var rs = "";

                rs = xml2.OuterXml;
                updateRequest(logID, rs);


                return new HttpResponseMessage()
                {
                    Content = new StringContent(rs, System.Text.Encoding.UTF8, "application/xml")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/NIPx")]
        public HttpResponseMessage NIPx(HttpRequestMessage req)
        {
            var ip = GetIP();
            //string req = JsonConvert.SerializeObject(name);
            var doc = new XmlDocument();
            doc.Load(req.Content.ReadAsStreamAsync().Result);
            var r = doc.DocumentElement.OuterXml;
            long logID = logRequest(ip, r);

            var response="";


            InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

            try
            {
                JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                //InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                                               
                response = S.FundTransfer(r);               

                updateRequest(logID, response);
                                             
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "96";
                resp.responseMessage = "Error Connecting to NIP service";

                
                updateRequest(logID, response);
            }
            // string output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(response, System.Text.Encoding.UTF8, "application/xml")
            };
            //return r;
        }

        public string ReturnTransType(decimal amt)
        {
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                var type = db.TransTypes.FirstOrDefault(a => a.typeAmount == amt);
                return type.transType1;
            }
        }
        public string ReturnTransType2(int channelCode)
        {
            using (JaizOpenDigitalBankingEntities db = new JaizOpenDigitalBankingEntities())
            {
                var type = db.TransTypes.FirstOrDefault(a => a.channelCode==channelCode);
                return type.transType1;
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/LocalFT")]
        public HttpResponseMessage LocalFT([FromBody] LocalFT req)
        {
            var ip = GetIP();
            string r = JsonConvert.SerializeObject(req);
            DateTime loginTime = DateTime.Now;           

            var response = "";
            string output = "";

            InterBankNameEnqResponse resp = new InterBankNameEnqResponse();
            responseclass rsp = new responseclass();
            try
            {
                JaizInternal.processmessage S = new JaizInternal.processmessage();
                
                XmlDocument xml2 = new XmlDocument();
                XmlElement root = xml2.CreateElement("FTSingleRequest");
                xml2.AppendChild(root);

                XmlElement SessionID = xml2.CreateElement("SessionID");
                XmlElement ChannelCode = xml2.CreateElement("ChannelCode");
                XmlElement DebitAccountNumber = xml2.CreateElement("DebitAccountNumber");
                XmlElement CreditAccountNumber = xml2.CreateElement("CreditAccountNumber");
                XmlElement TransactionType = xml2.CreateElement("TransactionType");
                XmlElement CurrencyType = xml2.CreateElement("CurrencyType");
                XmlElement BranchCode = xml2.CreateElement("BranchCode");
                XmlElement Narration = xml2.CreateElement("Narration");
                XmlElement Amount = xml2.CreateElement("Amount");
                XmlElement ValueDate = xml2.CreateElement("ValueDate");

                var CreditAccount = "";
                //get credit account
                if (req.ChannelCode == 4 || req.ChannelCode==6)
                {
                    CreditAccount = ConfigurationManager.AppSettings["eBillsAccount"];
                }
                else if (req.ChannelCode == 5)
                {
                    CreditAccount = ConfigurationManager.AppSettings["NIPAccount"];
                }
                else if (req.ChannelCode == 8)
                {
                    CreditAccount = ConfigurationManager.AppSettings["InsurtechAccount"];
                }
                else if (req.ChannelCode == 1 || req.ChannelCode == 2 || req.ChannelCode == 3)
                {
                    CreditAccount = req.CreditAccount;
                }

                //get the transaction codes

                SessionID.InnerText = req.SessionID;
                ChannelCode.InnerText = req.ChannelCode.ToString();
                DebitAccountNumber.InnerText = req.DebitAccount;
                //CreditAccountNumber.InnerText = req.CreditAccount;
                CreditAccountNumber.InnerText = CreditAccount;
                //TransactionType.InnerText = ReturnTransType(req.Fee);
                TransactionType.InnerText = ReturnTransType2(req.ChannelCode);
                CurrencyType.InnerText = "566";
                BranchCode.InnerText = "1";
                Narration.InnerText = req.Narration;
                Amount.InnerText = req.Amount.ToString();
                ValueDate.InnerText = DateTime.Now.ToString("dd/MM/yyyy");

                root.AppendChild(SessionID);
                root.AppendChild(ChannelCode);
                root.AppendChild(DebitAccountNumber);
                root.AppendChild(CreditAccountNumber);
                root.AppendChild(TransactionType);
                root.AppendChild(CurrencyType);
                root.AppendChild(BranchCode);
                root.AppendChild(Narration);
                root.AppendChild(Amount);
                root.AppendChild(ValueDate);

                var rs = "";

                rs = xml2.OuterXml;

                response = S.fundtransfersingleitem(rs);

                //convert to agreed format he can use
                //<?xml version="1.0" encoding="UTF-8" ?><FTSingleResponse><SessionID>233501190102084647142302115438</SessionID><ResponseCode>00</ResponseCode></FTSingleResponse>
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response);
                XmlNodeList xnList = xml.SelectNodes("FTSingleResponse");
            
                var rspCode = "";
            

                foreach (XmlNode xn in xnList)
                {
                   rspCode=xn["ResponseCode"].InnerText;
                
                }
                if (rspCode == "00")
                {
                    rsp.responseCode = rspCode;
                    rsp.responseMessage = "SUCCESSFUL";
                }
                else
                {
                    rsp.responseCode = rspCode;
                    rsp.responseMessage = "NOT SUCCESSFUL";
                }

                output = JsonConvert.SerializeObject(rsp);
                logRequestResponse2(ip, rs, "LocalFT", loginTime, response);

            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "96";
                resp.responseMessage = "Error Connecting to FT service";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, r, "LocalFT", loginTime, output);
            }
          
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
            };
            //return r;
        }

        [HttpPost]
        [Route("api/CustomerAPI/TSQx")]
        public HttpResponseMessage TSQx(HttpRequestMessage req)
        {
            var ip = GetIP();
            //string req = JsonConvert.SerializeObject(name);
            var doc = new XmlDocument();
            doc.Load(req.Content.ReadAsStreamAsync().Result);
            var r = doc.DocumentElement.OuterXml;
            long logID = logRequest(ip, r);

            var response = "";


            InterBankNameEnqResponse resp = new InterBankNameEnqResponse();

            try
            {
                JaizNIBSSInterface.processmessage S = new JaizNIBSSInterface.processmessage();
                //InternetBankingAPI.JaizHelper S = new InternetBankingAPI.JaizHelper();
                               
                response = S.newNIPTSQ(r);

                updateRequest(logID, response);

            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);
                resp.responseCode = "96";
                resp.responseMessage = "Error Connecting to NIP service";


                updateRequest(logID, response);
            }
            // string output = JsonConvert.SerializeObject(resp);
            // HttpResponseMessage r = Request.CreateResponse(HttpStatusCode.OK, output);
            return new HttpResponseMessage()
            {
                Content = new StringContent(response, System.Text.Encoding.UTF8, "application/xml")
            };
            //return r;
        }

        //------------------Start of the Jaiz OyaOya POS Implementation----------------//

        private static readonly string JaizOpenDigitalConn_ = ConfigurationManager.ConnectionStrings["JaizOpenDigitalBankingConn_"].ConnectionString.ToString();
        private static readonly string PostCardConn_ = ConfigurationManager.ConnectionStrings["PostCardConn_"].ConnectionString.ToString();
        private static readonly string ImalConn_ = ConfigurationManager.ConnectionStrings["OracConnection_"].ConnectionString.ToString();

        public void logRequestResponse2(string ip, string request, string method, DateTime loginTime, string response)
        {
            string query = "";
            query = "INSERT INTO log (logIP, logXML, logDate, methodName, logXMLOut, logXMLOutDate) VALUES (@logIP, @logXML, @logDate, @methodName, @logXMLOut, @logXMLOutDate)";

            _jobLogger.Info("Query to insert into log table ===> " + query);

            try
            {
                using (SqlConnection Connect = new SqlConnection(JaizOpenDigitalConn_))
                {
                    using (SqlCommand Cmd = new SqlCommand(query, Connect))
                    {

                        Cmd.Parameters.AddWithValue("@logIP", ip);
                        Cmd.Parameters.AddWithValue("@logXML", request);
                        Cmd.Parameters.AddWithValue("@logDate", loginTime);
                        Cmd.Parameters.AddWithValue("@methodName", method);
                        Cmd.Parameters.AddWithValue("@logXMLOut", response);
                        Cmd.Parameters.AddWithValue("@logXMLOutDate", DateTime.Now);

                        Connect.Open();

                        Cmd.ExecuteNonQuery();

                        Connect.Close();

                        _jobLogger.Info("Inserted Successfully============");
                    }
                }
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "Error Inserting into log table from logRequestResponse2 method",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                _jobLogger.Info("Log Table insertion error =========== " + ex.Message + Environment.NewLine + ex.InnerException);
            }
            finally
            {

            }
        }

        public string fetchCardAccount(string decryptedPAN)
        {
            string ret = "";
            var url = ConfigurationManager.AppSettings["POSCardAccountValidationURL"];
            //posCardValReq poscard = new posCardValReq();
            //poscard.cardPAN = decryptedPAN;
            //string rs = JsonConvert.SerializeObject(poscard);

            _jobLogger.Info("=======Inside the fetchCardAccount(string decryptedPAN) method==========");

            try
            {
               
                _jobLogger.Info("=======URL to call========== " +url);
                var client = new RestClient(url);
                var request = new RestRequest(RestSharp.Method.POST);

                request.AddHeader("ContentType", "application/json");
                request.AddParameter("cardPAN", decryptedPAN.Trim(), ParameterType.GetOrPost);                

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                _jobLogger.Info("Calling webservice:::: " + url);
                var s = client.Execute(request);

                var Xxc2 = JsonConvert.DeserializeObject<dynamic>(s.Content);
                var response = JsonConvert.SerializeObject(Xxc2);
                var resp = JsonConvert.DeserializeObject<fetchCard>(response);

                //var resp = JsonConvert.DeserializeObject<fetchCard>(r.ToString());

                _jobLogger.Info("Got from URL call: " +url+ " response:::: " + response);

                if (resp.ResponseCode == "00")
                {
                    ret = resp.AccountNo;
                }
                else
                {
                    ret = "";
                }
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "fetchCardAccount Webservice Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                _jobLogger.Error("Webservice call error:::: " + ex.Message + Environment.NewLine + ex.InnerException);
            }
            finally
            {

            }


            return ret;
        }

        //public string fetchCIF(string decryptedPAN)
        //{
        //    string ret = "";
        //    string query = "";

        //    query = "select customer_id FROM[postcard].[dbo].[pc_cards_1_A] where pan = '" + decryptedPAN + "' and card_status = 1";
        //    try
        //    {
        //        using (SqlConnection Connect = new SqlConnection(PostCardConn_))
        //        {
        //            using (SqlCommand Cmd = new SqlCommand(query, Connect))
        //            {
        //                Connect.Open();

        //                using (SqlDataReader reader = Cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        ret = reader["customer_id"].ToString();
        //                    }

        //                    Connect.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var err2 = new LogUtility.Error()
        //        {
        //            ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //            ErrorTime = DateTime.Now,
        //            ModulePointer = "Error",
        //            StackTrace = ex.StackTrace

        //        };
        //        LogUtility.ActivityLogger.WriteErrorLog(err2);
        //    }
        //    finally
        //    {

        //    }

        //    return ret;
        //}

        //public string fetchAccountNo(string cif)
        //{
        //    string acctNo = "";
        //    string query = "";
           
        //    query = "select additional_reference from amf where cif_sub_no = '" + cif + "'";
        //    try
        //    {
        //        using (OracleConnection connection = new OracleConnection(ImalConn_))
        //        {

        //            OracleCommand command = new OracleCommand(query, connection);
        //            connection.Open();
        //            OracleDataReader reader;
        //            reader = command.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                acctNo = reader["ADDITIONAL_REFERENCE"].ToString();
        //            }

        //            reader.Close();
        //            connection.Close();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        var err2 = new LogUtility.Error()
        //        {
        //            ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //            ErrorTime = DateTime.Now,
        //            ModulePointer = "Error",
        //            StackTrace = ex.StackTrace

        //        };
        //        LogUtility.ActivityLogger.WriteErrorLog(err2);

        //    }
        //    finally
        //    {
        //        //db.Configuration.Close();
        //    }

        //    return acctNo;
        //}

        public string fetchAccountNo(string cif)
        {
            string ret = "";
            var url = ConfigurationManager.AppSettings["FetchOnlineContactcenterDetailsURL"];
           
            _jobLogger.Info("=======Inside the  fetchAccountNo(string cif) method==========");

            try
            {
               
                _jobLogger.Info("=======URL to call========== " + url + "/FetchAccountNumber");
                var client = new RestClient(url + "/FetchAccountNumber");
                var request = new RestRequest(RestSharp.Method.POST);

                request.AddHeader("ContentType", "application/json");
                request.AddParameter("cif", cif.Trim(), ParameterType.GetOrPost);

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                _jobLogger.Info("Calling webservice:::: " + url + "/FetchAccountNumber");
                var s = client.Execute(request);

                var Xxc2 = JsonConvert.DeserializeObject<dynamic>(s.Content);
                var response = JsonConvert.SerializeObject(Xxc2);
                var resp = JsonConvert.DeserializeObject<fetchAcctNo>(response);

                //var resp = JsonConvert.DeserializeObject<fetchCard>(r.ToString());

                _jobLogger.Info("Got from URL call: " + url + "/FetchAccountNumber response:::: " + response);

                if (resp.responseCode == "00")
                {
                    ret = resp.accountNo;
                }
                else
                {
                    ret = "";
                }
            }
            catch (Exception ex)
            {
                var err2 = new LogUtility.Error()
                {
                    ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                    ErrorTime = DateTime.Now,
                    ModulePointer = "fetchAccountNumber Webservice Error",
                    StackTrace = ex.StackTrace

                };
                LogUtility.ActivityLogger.WriteErrorLog(err2);

                _jobLogger.Error("Webservice call error:::: " + ex.Message + Environment.NewLine + ex.InnerException);
            }
            finally
            {

            }


            return ret;
        }

        [HttpGet]
        [Route("api/CustomerAPI/CardNameEnquiry")]
        public HttpResponseMessage CardNameEnquiry(string AccountNumber)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string output = "";
            string account = "";
            responseClass resp = new responseClass();
            CardNameEnquiryResponse cnp = new CardNameEnquiryResponse();
            var re = Request;
            var headers = re.Headers;
            var ip = GetIP();
            DateTime loginTime = DateTime.Now;
            string iv = "";
            string decryptedAccountNo = "";
            string key = "";

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);
                // auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }

            if (String.IsNullOrEmpty(AccountNumber))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, "api/CustomerAPI/CardNameEnquiry? " + AccountNumber, "CardNameEnquiry", loginTime, output);


                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(AccountNumber, key, iv);
            account = fetchCardAccount(decryptedAccountNo);
            _jobLogger.Info("fetched AccountNo ===> " + account);
            bool permittedip = CheckPermittedIP(ip);

            if (auth > 0 && (permittedip))
            {
                
                try
                {
                    var url = ConfigurationManager.AppSettings["FetchOnlineContactcenterDetailsURL"];                   

                    var client = new RestClient(url + "/FetchCardNameEnquiry");
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("ContentType", "application/json");
                    request.AddParameter("accountNo", account.Trim(), ParameterType.GetOrPost);

                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                    _jobLogger.Info("Calling webservice CIF ===> " + url + "/FetchCardNameEnquiry");
                    var s = client.Execute(request);

                    var Xxc = JsonConvert.DeserializeObject<dynamic>(s.Content);
                    var respon = JsonConvert.SerializeObject(Xxc);
                    var json = JsonConvert.DeserializeObject<cardNameEnquiryResponse>(respon);

                    _jobLogger.Info("webservice responsecode ===> " + json.ResponseCode);

                    if (json.ResponseCode == "00")
                    {
                        cnp.AccountName = json.AccountName;
                        cnp.PhoneNumber = json.PhoneNumber;
                        cnp.ResponseCode = "00";
                        cnp.ResponseDescription = "Success";

                        output = JsonConvert.SerializeObject(cnp);
                        logRequestResponse2(ip, "api/CustomerAPI/CardNameEnquiry? " + AccountNumber, "CardNameEnquiry", loginTime, output);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                    else
                    {
                        cnp.AccountName = "";
                        cnp.PhoneNumber = "";
                        cnp.ResponseCode = "99";
                        cnp.ResponseDescription = "No record found!";

                        output = JsonConvert.SerializeObject(cnp);
                        logRequestResponse2(ip, "api/CustomerAPI/CardNameEnquiry? " + AccountNumber, "CardNameEnquiry", loginTime, output);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }


                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "api/CustomerAPI/CardNameEnquiry? " + AccountNumber + "Data fetch Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);
                    _jobLogger.Info("webservice error ===> " + ex.Message + Environment.NewLine + ex.InnerException);
                    resp.ResponseCode = "96";
                    resp.ResponseDescription = "Error fetching records";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, "api/CustomerAPI/CardNameEnquiry? " + AccountNumber, "CardNameEnquiry", loginTime, output);

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }

            }
            else
            {
                responseClass res = new responseClass();
                res.ResponseCode = "99";
                res.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(res);
                logRequestResponse2(ip, "api/CustomerAPI/CardNameEnquiry? " + AccountNumber, "CardNameEnquiry", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpGet]
        [Route("api/CustomerAPI/CardBalanceEnquiry")]
        public HttpResponseMessage CardBalanceEnquiry(string AccountNumber)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string output = "";
            string accountNo = "";
            responseClass resp = new responseClass();
            CardBalanceEnquiryResponse cnp = new CardBalanceEnquiryResponse();
            var re = Request;
            var headers = re.Headers;
            var ip = GetIP();
            DateTime loginTime = DateTime.Now;
            string iv = "";
            string decryptedAccountNo = "";
            string key = "";

            _jobLogger.Info("======Inside the CardBalanceEnquiry Method=========== ");

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);
                // auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }

            if (String.IsNullOrEmpty(AccountNumber))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber, "CardBalanceEnquiry", loginTime, output);

                _jobLogger.Info("======validation Response=========== " + output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(AccountNumber, key, iv);

            accountNo = fetchCardAccount(decryptedAccountNo);

            _jobLogger.Info("fetched the Account No of customer:: " + accountNo);

            bool permittedip = CheckPermittedIP(ip);

            if (auth > 0 && (permittedip))
            {
                
                try
                {
                    var url = ConfigurationManager.AppSettings["FetchOnlineContactcenterDetailsURL"];

                    var client = new RestClient(url + "/FetchCardBalanceEnquiry");
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("ContentType", "application/json");
                    request.AddParameter("accountNo", accountNo.Trim(), ParameterType.GetOrPost);

                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                    _jobLogger.Info("Calling webservice===> " + url + "/FetchCardBalanceEnquiry");
                    var s = client.Execute(request);

                    var Xxc = JsonConvert.DeserializeObject<dynamic>(s.Content);
                    var respon = JsonConvert.SerializeObject(Xxc);
                    var json = JsonConvert.DeserializeObject<cardBalanceEnquiryResponse>(respon);

                    _jobLogger.Info("webservice responsecode ===> " + json.ResponseCode);

                    if (json.ResponseCode == "00")
                    {
                        cnp.AccountName = json.AccountName;
                        cnp.PhoneNumber = json.PhoneNumber;
                        cnp.Balance = json.Balance;
                        cnp.ResponseCode = "00";
                        cnp.ResponseDescription = "Success";

                        output = JsonConvert.SerializeObject(cnp);
                        logRequestResponse2(ip, "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber, "CardBalanceEnquiry", loginTime, output);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                    else
                    {
                        cnp.AccountName = "";
                        cnp.PhoneNumber = "";
                        cnp.Balance = Convert.ToDecimal(0.00);
                        cnp.ResponseCode = "99";
                        cnp.ResponseDescription = "No record found!";

                        output = JsonConvert.SerializeObject(cnp);
                        logRequestResponse2(ip, "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber, "CardBalanceEnquiry", loginTime, output);

                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber + " Data Fetch Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);

                    _jobLogger.Error("webservice error ===> " + ex.Message + Environment.NewLine + ex.InnerException);

                    resp.ResponseCode = "96";
                    resp.ResponseDescription = "Error fetching records";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber, "CardBalanceEnquiry", loginTime, output);

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }

            }
            else
            {
                responseClass res = new responseClass();
                res.ResponseCode = "99";
                res.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(res);
                logRequestResponse2(ip, "api/CustomerAPI/CardBalanceEnquiry? " + AccountNumber, "CardBalanceEnquiry", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("api/CustomerAPI/CardDeposit")]
        public async Task<HttpResponseMessage> CardDeposit([FromBody] CardDepositRequest req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string iv = "";
            string key = "";
            string accountNo = "";
            string accountToCredit = "";
            string decryptedAccountNo = "";

            var re = Request;
            var headers = re.Headers;
            DateTime loginTime = DateTime.Now;
            _jobLogger.Info("======Inside the CardDeposit Method=========== ");

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string request = JsonConvert.SerializeObject(req);
            _jobLogger.Info("======Request Body=========== " + request);
            responseClass resp = new responseClass();
            bool permittedip = CheckPermittedIP(ip);
            string output = "";

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(req.CardCreditAccount, key, iv);
            //fetch the value of the custId of customer using the decryptedAccountNo(PAN) from PostCard
            //  select customer_id FROM [postcard].[dbo].[pc_cards_1_A]
            // where pan = '5538130177612095' and default_account_type = 20 and card_status = 1
            //id for this is: 13001532
            accountToCredit = fetchCardAccount(decryptedAccountNo);
            //cif = "13001532";          
            var response = "";
            string chann = ConfigurationManager.AppSettings["CardDepositChannel"].ToString();

            if (string.IsNullOrEmpty(req.CardCreditAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "card credit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.DebitAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "debit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Amount == new decimal(0) || req.Amount < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "amount cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.TransactionReference))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "transaction reference cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (!string.IsNullOrEmpty(req.TransactionReference))
            {
                if (req.TransactionReference.Length > 30)
                {
                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "transaction reference length cannot be more than 30!";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }

            if (auth > 0 && (permittedip))
            {

                try
                {
                    JaizInternal.processmessage S = new JaizInternal.processmessage();

                    XmlDocument xml = new XmlDocument();
                    XmlElement root = xml.CreateElement("FTSingleRequest");
                    xml.AppendChild(root);

                    XmlElement SessionID = xml.CreateElement("SessionID");
                    XmlElement ChannelCode = xml.CreateElement("ChannelCode");
                    XmlElement DebitAccountNumber = xml.CreateElement("DebitAccountNumber");
                    XmlElement CreditAccountNumber = xml.CreateElement("CreditAccountNumber");
                    XmlElement TransactionType = xml.CreateElement("TransactionType");
                    XmlElement CurrencyType = xml.CreateElement("CurrencyType");
                    XmlElement BranchCode = xml.CreateElement("BranchCode");
                    XmlElement Narration = xml.CreateElement("Narration");
                    XmlElement Amount = xml.CreateElement("Amount");
                    XmlElement ValueDate = xml.CreateElement("ValueDate");

                    SessionID.InnerText = req.TransactionReference;
                    ChannelCode.InnerText = chann;
                    DebitAccountNumber.InnerText = req.DebitAccount;
                    CreditAccountNumber.InnerText = accountToCredit;
                    TransactionType.InnerText = ReturnTransType2(Convert.ToInt16(chann));
                    CurrencyType.InnerText = "566";
                    BranchCode.InnerText = "1";
                    Narration.InnerText = req.Narration;
                    Amount.InnerText = req.Amount.ToString();
                    ValueDate.InnerText = DateTime.Now.ToString("dd/MM/yyyy");

                    root.AppendChild(SessionID);
                    root.AppendChild(ChannelCode);
                    root.AppendChild(DebitAccountNumber);
                    root.AppendChild(CreditAccountNumber);
                    root.AppendChild(TransactionType);
                    root.AppendChild(CurrencyType);
                    root.AppendChild(BranchCode);
                    root.AppendChild(Narration);
                    root.AppendChild(Amount);
                    root.AppendChild(ValueDate);

                    var rs = "";

                    rs = xml.OuterXml;
                    var send = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + xml.InnerXml;
                    _jobLogger.Info("======Request Body Sent to LocalFT service=========== " + send);
                    response = S.fundtransfersingleitem(send);
                    _jobLogger.Info("======Response Obtained From LocalFT service=========== " + response);
                    XmlDocument xml2 = new XmlDocument();
                    xml2.LoadXml(response);
                    XmlNodeList xnList = xml2.SelectNodes("FTSingleResponse");

                    var rspCode = "";

                    foreach (XmlNode xn in xnList)
                    {
                        rspCode = xn["ResponseCode"].InnerText;
                    }

                    if (rspCode == "00")
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = "Success";
                    }
                    else
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = "Failed";

                        //Adjust later////////
                        //resp.ResponseCode = "00";
                        //resp.ResponseDescription = "Success";
                    }

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardDeposit", loginTime, output);

                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "CardDeposit Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);

                    resp.ResponseCode = "96";
                    resp.ResponseDescription = "Card Deposit Method Error!";

                    //Adjust later////////
                    //resp.ResponseCode = "00";
                    //resp.ResponseDescription = "Success";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
                }
            }
            else
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardDeposit", loginTime, output);
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        [HttpPost]
        [Route("api/CustomerAPI/CardWithdrawal")]
        public async Task<HttpResponseMessage> CardWithdrawal([FromBody] CardWithdrawalRequest req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string iv = "";
            string key = "";
           // string cif = "";
            string accountToDebit = "";
            string decryptedAccountNo = "";

            _jobLogger.Info("=======Inside The Card Withdrawal Method=======");
            string chann = ConfigurationManager.AppSettings["CardWithdrawalChannel"].ToString();
            //string jaizAccount = ConfigurationManager.AppSettings["AgencyBankingCommJaizAcct"].ToString();
            var re = Request;
            var headers = re.Headers;
            DateTime loginTime = DateTime.Now;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string request = JsonConvert.SerializeObject(req);
            _jobLogger.Info("======Request Body Sent======== " + request);
            responseClass resp = new responseClass();
            bool permittedip = CheckPermittedIP(ip);
            string output = "";

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(req.CardDebitAccount, key, iv);
            accountToDebit = fetchCardAccount(decryptedAccountNo);
            _jobLogger.Info("======Account to Debit Fetched IS======== " + accountToDebit);
            //accountToDebit = fetchAccountNo(cif);

            var response = "";

            if (string.IsNullOrEmpty(req.CreditAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "credit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.CardDebitAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "card debit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Amount == new decimal(0) || req.Amount < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "amount cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.TransactionReference))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "transaction reference cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (!string.IsNullOrEmpty(req.TransactionReference))
            {
                if (req.TransactionReference.Length > 30)
                {
                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "transaction reference length cannot be more than 30!";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }


            if (auth > 0 && (permittedip))
            {

                try
                {
                    JaizInternal.processmessage S = new JaizInternal.processmessage();

                    XmlDocument xml = new XmlDocument();
                    XmlElement root = xml.CreateElement("FTSingleRequest");
                    xml.AppendChild(root);

                    XmlElement SessionID = xml.CreateElement("SessionID");
                    XmlElement ChannelCode = xml.CreateElement("ChannelCode");
                    XmlElement DebitAccountNumber = xml.CreateElement("DebitAccountNumber");
                    XmlElement CreditAccountNumber = xml.CreateElement("CreditAccountNumber");
                    XmlElement TransactionType = xml.CreateElement("TransactionType");
                    XmlElement CurrencyType = xml.CreateElement("CurrencyType");
                    XmlElement BranchCode = xml.CreateElement("BranchCode");
                    XmlElement Narration = xml.CreateElement("Narration");
                    XmlElement Amount = xml.CreateElement("Amount");
                    XmlElement ValueDate = xml.CreateElement("ValueDate");

                    SessionID.InnerText = req.TransactionReference;
                    ChannelCode.InnerText = chann;
                    DebitAccountNumber.InnerText = accountToDebit;
                    CreditAccountNumber.InnerText = req.CreditAccount;
                    //TransactionType.InnerText = ReturnTransType(req.Fee);
                    TransactionType.InnerText = ReturnTransType2(Convert.ToInt16(chann));
                    CurrencyType.InnerText = "566";
                    BranchCode.InnerText = "1";
                    Narration.InnerText = req.Narration;
 
                    Amount.InnerText = req.Amount.ToString();
                    ValueDate.InnerText = DateTime.Now.ToString("dd/MM/yyyy");

                    root.AppendChild(SessionID);
                    root.AppendChild(ChannelCode);
                    root.AppendChild(DebitAccountNumber);
                    root.AppendChild(CreditAccountNumber);
                    root.AppendChild(TransactionType);
                    root.AppendChild(CurrencyType);
                    root.AppendChild(BranchCode);
                    root.AppendChild(Narration);
                    root.AppendChild(Amount);
                    root.AppendChild(ValueDate);

                    var rs = "";

                    rs = xml.OuterXml;
                    var send = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + xml.InnerXml;
                    _jobLogger.Info("======Request Body Sent to LocalFT service=========== " + send);
                    response = S.fundtransfersingleitem(send);
                    _jobLogger.Info("======Response Obtained From LocalFT service=========== " + response);

                    XmlDocument xml2 = new XmlDocument();
                    xml2.LoadXml(response);
                    XmlNodeList xnList = xml2.SelectNodes("FTSingleResponse");

                    var rspCode = "";
                    var sess = "";

                    foreach (XmlNode xn in xnList)
                    {
                        rspCode = xn["ResponseCode"].InnerText;
                       // sess = xn["SessionID"].InnerText;
                    }

                    if (rspCode == "00")
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = "Success";

                        //logPOSTransaction(sess, account, customerCharge, req.CreditAccount, amountForAgent, jaizAccount, amountForJaiz);

                    }
                    else
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = "Failed";
                    }

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);

                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "CardWithdrawal Error",
                        StackTrace = ex.StackTrace

                    };
                    LogUtility.ActivityLogger.WriteErrorLog(err2);

                    resp.ResponseCode = "96";
                    resp.ResponseDescription = "Card Withdrawal Method Error!";

                    //Adjust later////////
                    //resp.ResponseCode = "00";
                    //resp.ResponseDescription = "Success";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
                }
            }
            else
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardWithdrawal", loginTime, output);
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }

        //[HttpPost]
        //[Route("api/CustomerAPI/CardLocalTransfer")]
        //public async Task<HttpResponseMessage> CardLocalTransfer([FromBody] CardWithdrawalRequest req)
        //{
        //    var username = "";
        //    var password = "";
        //    int auth = 0;
        //    string iv = "";
        //    string key = "";
        //    string cif = "";
        //    string account = "";
        //    string decryptedAccountNo = "";

        //    var re = Request;
        //    var headers = re.Headers;
        //    DateTime loginTime = DateTime.Now;

        //    if (headers.Contains("Username"))
        //    {
        //        username = headers.GetValues("Username").First();
        //    }
        //    if (headers.Contains("Password"))
        //    {
        //        password = headers.GetValues("Password").First();
        //    }
        //    try
        //    {
        //        auth = authenticateUsers(username, password);
        //        //auth = 1;
        //    }
        //    catch (Exception ex)
        //    {
        //        auth = 0;
        //    }
        //    var ip = GetIP();
        //    string request = JsonConvert.SerializeObject(req);
        //    responseClass resp = new responseClass();
        //    bool permittedip = CheckPermittedIP(ip);
        //    string output = "";

        //    iv = ConfigurationManager.AppSettings["IV"];
        //    key = ConfigurationManager.AppSettings["Key"];
        //    AESCrypto aec = new AESCrypto();
        //    decryptedAccountNo = aec.Decrypt(req.CardDebitAccount, key, iv);
        //    //fetch the value of the custId of customer using the decryptedAccountNo(PAN) from PostCard
        //    //  select customer_id FROM [postcard].[dbo].[pc_cards_1_A]
        //    // where pan = '5538130177612095' and default_account_type = 20 and card_status = 1
        //    //id for this is: 13001532
        //    cif = fetchCIF(decryptedAccountNo);            
        //    account = fetchAccountNo(cif);
        //    var response = "";

        //    if (string.IsNullOrEmpty(req.CreditAccount))
        //    {
        //        resp.ResponseCode = "99";
        //        resp.ResponseDescription = "credit account number cannot be null or empty!";

        //        output = JsonConvert.SerializeObject(resp);
        //        logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //        return new HttpResponseMessage()
        //        {
        //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //        };
        //    }

        //    if (string.IsNullOrEmpty(req.CardDebitAccount))
        //    {
        //        resp.ResponseCode = "99";
        //        resp.ResponseDescription = "card debit account number cannot be null or empty!";

        //        output = JsonConvert.SerializeObject(resp);
        //        logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //        return new HttpResponseMessage()
        //        {
        //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //        };
        //    }

        //    if (req.Amount == new decimal(0) || req.Amount < new decimal(1))
        //    {
        //        resp.ResponseCode = "99";
        //        resp.ResponseDescription = "amount cannot be 0 or less than 0!";

        //        output = JsonConvert.SerializeObject(resp);
        //        logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //        return new HttpResponseMessage()
        //        {
        //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //        };
        //    }

        //    if (string.IsNullOrEmpty(req.TransactionReference))
        //    {
        //        resp.ResponseCode = "99";
        //        resp.ResponseDescription = "transaction reference cannot be null or empty!";

        //        output = JsonConvert.SerializeObject(resp);
        //        logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //        return new HttpResponseMessage()
        //        {
        //            Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //        };
        //    }

        //    if (!string.IsNullOrEmpty(req.TransactionReference))
        //    {
        //        if (req.TransactionReference.Length > 30)
        //        {
        //            resp.ResponseCode = "99";
        //            resp.ResponseDescription = "transaction reference length cannot be more than 30!";

        //            output = JsonConvert.SerializeObject(resp);
        //            logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //            return new HttpResponseMessage()
        //            {
        //                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //            };
        //        }
        //    }

        //    if (auth > 0 && (permittedip))
        //    {

        //        try
        //        {
        //            JaizInternal2.processmessage S = new JaizInternal2.processmessage();

        //            XmlDocument xml = new XmlDocument();
        //            XmlElement root = xml.CreateElement("FTSingleRequest");
        //            xml.AppendChild(root);

        //            XmlElement SessionID = xml.CreateElement("SessionID");
        //            XmlElement ChannelCode = xml.CreateElement("ChannelCode");
        //            XmlElement DebitAccountNumber = xml.CreateElement("DebitAccountNumber");
        //            XmlElement CreditAccountNumber = xml.CreateElement("CreditAccountNumber");
        //            XmlElement TransactionType = xml.CreateElement("TransactionType");
        //            XmlElement CurrencyType = xml.CreateElement("CurrencyType");
        //            XmlElement BranchCode = xml.CreateElement("BranchCode");
        //            XmlElement Narration = xml.CreateElement("Narration");
        //            XmlElement Amount = xml.CreateElement("Amount");
        //            XmlElement ValueDate = xml.CreateElement("ValueDate");

        //            SessionID.InnerText = req.TransactionReference;
        //            ChannelCode.InnerText = "1";
        //            DebitAccountNumber.InnerText = account;
        //            CreditAccountNumber.InnerText = req.CreditAccount;
        //            TransactionType.InnerText = ReturnTransType(req.Fee);
        //            CurrencyType.InnerText = "566";
        //            BranchCode.InnerText = "1";
        //            Narration.InnerText = req.Narration;
        //            Amount.InnerText = req.Amount.ToString();
        //            ValueDate.InnerText = DateTime.Now.ToString("dd/MM/yyyy");

        //            root.AppendChild(SessionID);
        //            root.AppendChild(ChannelCode);
        //            root.AppendChild(DebitAccountNumber);
        //            root.AppendChild(CreditAccountNumber);
        //            root.AppendChild(TransactionType);
        //            root.AppendChild(CurrencyType);
        //            root.AppendChild(BranchCode);
        //            root.AppendChild(Narration);
        //            root.AppendChild(Amount);
        //            root.AppendChild(ValueDate);

        //            var rs = "";

        //            rs = xml.OuterXml;
        //            var send = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + xml.InnerXml;
        //            response = S.fundtransfersingleitem(send);

        //            XmlDocument xml2 = new XmlDocument();
        //            xml2.LoadXml(response);
        //            XmlNodeList xnList = xml2.SelectNodes("FTSingleResponse");

        //            var rspCode = "";

        //            foreach (XmlNode xn in xnList)
        //            {
        //                rspCode = xn["ResponseCode"].InnerText;

        //            }
        //            if (rspCode == "00")
        //            {
        //                resp.ResponseCode = rspCode;
        //                resp.ResponseDescription = "Success";
        //            }
        //            else
        //            {
        //                resp.ResponseCode = rspCode;
        //                resp.ResponseDescription = "failed";

        //                //Adjust later////////
        //                //resp.ResponseCode = "00";
        //                //resp.ResponseDescription = "Success";
        //            }

        //            output = JsonConvert.SerializeObject(resp);
        //            logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);

        //        }
        //        catch (Exception ex)
        //        {
        //            var err2 = new LogUtility.Error()
        //            {
        //                ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
        //                ErrorTime = DateTime.Now,
        //                ModulePointer = "CardLocalTransfer Error",
        //                StackTrace = ex.StackTrace

        //            };
        //            LogUtility.ActivityLogger.WriteErrorLog(err2);

        //            resp.ResponseCode = "96";
        //            resp.ResponseDescription = "Card Deposit Method Error!";

        //            //Adjust later////////
        //            //resp.ResponseCode = "00";
        //            //resp.ResponseDescription = "Success";

        //            output = JsonConvert.SerializeObject(resp);
        //            logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //        }

        //    }
        //    else
        //    {
        //        resp.ResponseCode = "99";
        //        resp.ResponseDescription = "Invalid Username or Password";

        //        output = JsonConvert.SerializeObject(resp);
        //        logRequestResponse2(ip, request, "CardLocalTransfer", loginTime, output);
        //    }

        //    return new HttpResponseMessage()
        //    {
        //        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
        //    };
        //}

        [HttpPost]
        [Route("api/CustomerAPI/CardBillPayment")]
        public HttpResponseMessage CardBillPayment([FromBody] CardBillPaymentRequest req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string iv = "";
            string key = "";
            string decryptedAccountNo = "";
            //string cif = "";
            string account = "";

            var re = Request;
            var headers = re.Headers;
            DateTime loginTime = DateTime.Now;
            responseClass resp = new responseClass();

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                //auth = authenticateUsers(username, password);
                auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string output = "";
            string request = JsonConvert.SerializeObject(req);

            if (string.IsNullOrEmpty(req.ProductCode))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "product code cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.ReferenceID))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "reference id cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Amount == new decimal(0) || req.Amount < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "amount cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.CardDebitAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "card debit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(req.CardDebitAccount, key, iv);

            //fetch the value of the custId of customer using the decryptedAccountNo(PAN) from PostCard
            //  select customer_id FROM [postcard].[dbo].[pc_cards_1_A]
            // where pan = '5538130177612095' and default_account_type = 20 and card_status = 1
            account = fetchCardAccount(decryptedAccountNo);
            //account = fetchAccountNo(cif);

            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();

                var ls = bs.QTSendBillPaymentAdvice(account, req.ProductCode, req.ReferenceID, req.PhoneNumber, req.EmailAddress, req.Amount.ToString());

                var response = ls.responseCode;

                if (response.Contains("90000") || response.Contains("90009") || response.Contains("900a0"))
                {

                    resp.ResponseCode = "00";
                    resp.ResponseDescription = "Success";
                }
                else
                {
                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "Failed";

                    //Adjust later////////
                    //resp.ResponseCode = "00";
                    //resp.ResponseDescription = "Success";
                }

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {

                resp.ResponseCode = "99";
                resp.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardBillPayment", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
        }


        [HttpPost]
        [Route("api/CustomerAPI/CardMobileRecharge")]
        public HttpResponseMessage CardMobileRecharge([FromBody] CardBillPaymentRequest req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string iv = "";
            string key = "";
            string decryptedAccountNo = "";
            string cif = "";
            string account = "";

            var re = Request;
            var headers = re.Headers;
            DateTime loginTime = DateTime.Now;
            responseClass resp = new responseClass();

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                //decrypt the password sent
                // var decryptedpswd = Decrypt(password, "h28wi47");
                auth = authenticateUsers(username, password);
                //auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string output = "";
            string request = JsonConvert.SerializeObject(req);
            if (string.IsNullOrEmpty(req.ProductCode))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "product code cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.ReferenceID))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "reference id cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Amount == new decimal(0) || req.Amount < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "amount cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (string.IsNullOrEmpty(req.CardDebitAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "card debit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            iv = ConfigurationManager.AppSettings["IV"];
            key = ConfigurationManager.AppSettings["Key"];
            AESCrypto aec = new AESCrypto();
            decryptedAccountNo = aec.Decrypt(req.CardDebitAccount, key, iv);

            //fetch the value of the custId of customer using the decryptedAccountNo(PAN) from PostCard
            //  select customer_id FROM [postcard].[dbo].[pc_cards_1_A]
            // where pan = '5538130177612095' and default_account_type = 20 and card_status = 1
            account = fetchCardAccount(decryptedAccountNo);
            //account = fetchAccountNo(cif);

            bool permittedip = CheckPermittedIP(ip);
            if (auth > 0 && (permittedip))
            {

                BillsPaymentService.JaizHelper bs = new BillsPaymentService.JaizHelper();

                var ls = bs.QTSendBillPaymentAdvice(account, req.ProductCode, req.ReferenceID, req.PhoneNumber, req.EmailAddress, req.Amount.ToString());

                var response = ls.responseCode;

                if (response.Contains("90000") || response.Contains("90009") || response.Contains("900a0"))
                {

                    resp.ResponseCode = "00";
                    resp.ResponseDescription = "Success";
                }
                else
                {
                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "Failed";

                    //Adjust later////////
                    //resp.ResponseCode = "00";
                    //resp.ResponseDescription = "Success";
                }

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {

                resp.ResponseCode = "99";
                resp.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "CardMobileRecharge", loginTime, output);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };

            }
        }


        [HttpPost]
        [Route("api/CustomerAPI/OtherBankCardWithdrawalNotification")]
        public async Task<HttpResponseMessage> OtherBankCardWithdrawalNotification([FromBody] OtherBankCardsRequest req)
        {
            var username = "";
            var password = "";
            int auth = 0;
            string iv = "";
            string key = "";
            string cif = "";
            string account = "";
            string tempTransCode= "";

            var re = Request;
            var headers = re.Headers;
            DateTime loginTime = DateTime.Now;

            if (headers.Contains("Username"))
            {
                username = headers.GetValues("Username").First();
            }
            if (headers.Contains("Password"))
            {
                password = headers.GetValues("Password").First();
            }
            try
            {
                auth = authenticateUsers(username, password);
                // auth = 1;
            }
            catch (Exception ex)
            {
                auth = 0;
            }
            var ip = GetIP();
            string output = "";
            var response = "";
            _jobLogger.Info("|OtherBankCardWithdrawalNotification| Inside The OtherBankCardWithdrawalNotification Method===== ");
            string request = JsonConvert.SerializeObject(req);
            _jobLogger.Info("|OtherBankCardWithdrawalNotification| Request Body To Be Sent IS===== " + request);
            responseClass resp = new responseClass();

            if (string.IsNullOrEmpty(req.CreditAccount))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "credit account number cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Amount == new decimal(0.00) || req.Amount < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "amount cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (req.Fee == new decimal(0.00) || req.Fee < new decimal(1))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "fee cannot be 0 or less than 0!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }


            if (string.IsNullOrEmpty(req.TransactionReference))
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "transaction reference cannot be null or empty!";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                return new HttpResponseMessage()
                {
                    Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (!string.IsNullOrEmpty(req.TransactionReference))
            {
                if (req.TransactionReference.Length > 30)
                {
                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "transaction reference length cannot be more than 30!";

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }

            bool permittedip = CheckPermittedIP(ip);

            account = ConfigurationManager.AppSettings["TempJaizSuspenseAccount"];
            tempTransCode = ConfigurationManager.AppSettings["TempTransactionCodeOtherBankCardWithdrawal"];
            //account = ConfigurationManager.AppSettings["eBillsAccount"];
            //------we need to analyse this later------------//
            //decimal amountVariance = req.Amount - req.Fee;

            //------we need to analyse this later------------//
            string chann = ConfigurationManager.AppSettings["CardWithdrawalChannel"].ToString();
            //---------------------------//

            if (auth > 0 && (permittedip))
            {
                try
                {
                    JaizInternal.processmessage S = new JaizInternal.processmessage();

                    XmlDocument xml = new XmlDocument();
                    XmlElement root = xml.CreateElement("FTSingleRequest");
                    xml.AppendChild(root);

                    XmlElement SessionID = xml.CreateElement("SessionID");
                    XmlElement ChannelCode = xml.CreateElement("ChannelCode");
                    XmlElement DebitAccountNumber = xml.CreateElement("DebitAccountNumber");
                    XmlElement CreditAccountNumber = xml.CreateElement("CreditAccountNumber");
                    XmlElement TransactionType = xml.CreateElement("TransactionType");
                    XmlElement CurrencyType = xml.CreateElement("CurrencyType");
                    XmlElement BranchCode = xml.CreateElement("BranchCode");
                    XmlElement Narration = xml.CreateElement("Narration");
                    XmlElement Amount = xml.CreateElement("Amount");
                    XmlElement ValueDate = xml.CreateElement("ValueDate");

                    _jobLogger.Info("|OtherBankCardWithdrawalNotification| Account To Be debited IS===== " + account);

                    SessionID.InnerText = req.TransactionReference;
                    //ChannelCode.InnerText = "1";
                    ChannelCode.InnerText = chann;
                    DebitAccountNumber.InnerText = account;
                    CreditAccountNumber.InnerText = req.CreditAccount;
                    //------------
                    TransactionType.InnerText = ReturnTransType2(Convert.ToInt16(chann));
                    //TransactionType.InnerText = tempTransCode;
                    //TransactionType.InnerText = "302";
                    CurrencyType.InnerText = "566";
                    BranchCode.InnerText = "1";
                    Narration.InnerText = "IFO " + req.MaskedPAN + "/" + req.RRN + "/" + req.TerminalId + "/" + req.Stan + "/" + req.TerminalLocation;
                    Amount.InnerText = req.Amount.ToString();
                    //Amount.InnerText = amountVariance.ToString();
                    ValueDate.InnerText = DateTime.Now.ToString("dd/MM/yyyy");

                    root.AppendChild(SessionID);
                    root.AppendChild(ChannelCode);
                    root.AppendChild(DebitAccountNumber);
                    root.AppendChild(CreditAccountNumber);
                    root.AppendChild(TransactionType);
                    root.AppendChild(CurrencyType);
                    root.AppendChild(BranchCode);
                    root.AppendChild(Narration);
                    root.AppendChild(Amount);
                    root.AppendChild(ValueDate);

                    var rs = "";

                    rs = xml.OuterXml;
                    var send = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + xml.InnerXml;
                    _jobLogger.Info("======Request Body Sent to LocalFT service=========== " + send);
                    response = S.fundtransfersingleitem(send);
                    _jobLogger.Info("======Response Obtained From LocalFT service=========== " + response);
                    XmlDocument xml2 = new XmlDocument();
                    xml2.LoadXml(response);
                    XmlNodeList xnList = xml2.SelectNodes("FTSingleResponse");

                    var rspCode = "";

                    foreach (XmlNode xn in xnList)
                    {
                        rspCode = xn["ResponseCode"].InnerText;

                    }
                    if (rspCode == "00")
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = "Success";
                    }
                    else
                    {
                        resp.ResponseCode = rspCode;
                        resp.ResponseDescription = response;

                        //Adjust later////////
                        //resp.ResponseCode = "00";
                        //resp.ResponseDescription = "Success";
                    }

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, response);

                }
                catch (Exception ex)
                {
                    var err2 = new LogUtility.Error()
                    {
                        ErrorDescription = ex.Message + Environment.NewLine + ex.InnerException,
                        ErrorTime = DateTime.Now,
                        ModulePointer = "OtherBankCardWithdrawalNotification Error",
                        StackTrace = ex.StackTrace

                    };
                    string errr = ex.Message + Environment.NewLine + ex.InnerException;
                    LogUtility.ActivityLogger.WriteErrorLog(err2);

                    resp.ResponseCode = "99";
                    resp.ResponseDescription = "Internal Error!";

                    //Adjust later////////
                    //resp.ResponseCode = "00";
                    //resp.ResponseDescription = "Success";
                    _jobLogger.Info("======OtherBankCardWithdrawalNotification Error=========== " + errr);

                    output = JsonConvert.SerializeObject(resp);
                    logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, response);

                }

            }
            else
            {
                resp.ResponseCode = "99";
                resp.ResponseDescription = "Invalid Username or Password";

                output = JsonConvert.SerializeObject(resp);
                logRequestResponse2(ip, request, "OtherBankCardWithdrawalNotification", loginTime, output);
                _jobLogger.Info("======Authentication Error=========== " + output);
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json")
            };
        }


        public class fetchCard
        {
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string AccountNo { get; set; }
        }

        public class fetchAcctNo
        {
            public string responseCode { get; set; }
            public string accountNo { get; set; }           
        }

        public class posCardValReq
        {
            public string cardPAN { get; set; }
        }

        public class cardNameEnquiryResponse
        {
            public string AccountName { get; set; }
            public string PhoneNumber { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
        }

        public class cardBalanceEnquiryResponse
        {
            public string AccountName { get; set; }
            public string PhoneNumber { get; set; }
            public decimal Balance { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
        }

        //---------------------------End of Jaiz Oya Oya Implementation----------------//
    }
}
