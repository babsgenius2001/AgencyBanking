using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace JaizAgencyBanking.Models
{
    public class ODBClass
    {

    }
    public class NewAccountOpeningResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountNo { get; set; }
    
    }
    public class NewAccountOpeningRequest
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
        public string title { get; set; }
        public string idtype { get; set; }
        public string idno { get; set; }
        public string marital { get; set; }
        public string division { get; set; }
        public string dept { get; set; }
        public string ecosector { get; set; }
        public string addref { get; set; }
        public string bvn { get; set; }
        public string Image { get; set; }
        public string SignatureImage { get; set; }
    
    }
    public class DataVendingRequest
    {
        public string loginId { get; set; }
        public string key { get; set; }
        public string serviceId { get; set; }
        public string amount { get; set; }
        public string recipient { get; set; }
        public DateTime date { get; set; }
        public string requestId { get; set; }


    }
    public class databundlerequest
    {
        public string loginId { get; set; }
        public string key { get; set; }
        public string serviceId { get; set; }
    }
    public class QTCat
    {
        public int categoryid { get; set; }
        public string categorydescription { get; set; }
        public string categoryname { get; set; }
    }
    public class QTBillCat
    {
        public string categoryname { get; set; }
        public string categorydescription { get; set; }
        public int billerid { get; set; }
        public string shortName { get; set; }
        public string currencyCode { get; set; }
        public string surcharge { get; set; }
        public string billername { get; set; }
        public int categorysid { get; set; }
    }
    public class staffDetailsRequest
    {
        public string domainID { get; set; }
    }
    public class staffIDRequest
    {
        public string email { get; set; }
    }
    public class staffUserIDResponse
    {
        public string username { get; set; }
    }
    public class staffDetailsResponse
    {
        public string lastName { get; set; }
        public string otherNames { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string designation { get; set; }
        public string branch { get; set; }
    }
    public class VendingCatRequest
    {
        public int typeID { get; set; }
    }
    public class DataBundleRequest
    {
        public string serviceId { get; set; }
    }
    public class aVendingResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class dVendingResponse
    {
        public string statusCode { get; set; }
        public string statusDescription { get; set; }
    }
    public class response
    {
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }
    public class BalanceEnquiry
    {
        public string accountNo { get; set; }
    }
    public class account
    {
        public string accountNo { get; set; }
    }
    public class BalanceEnquiryResponse
    {
        public string accountName { get; set; }
        public string phoneNo { get; set; }
        public string balance { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class NameEnquiry
    {
        public string accountNo { get; set; }
    }
    public class NameEnquiryResponse
    {
        public string accountName { get; set; }
        public string phoneNo { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class bankquery
    {
        public string action { get; set; }
    }

    public class LocalFT
    {
        public string CreditAccount { get; set; }
        public string DebitAccount { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string Narration { get; set; }
        public string SessionID { get; set; }
        public int ChannelCode { get; set; }
    }

    public class InterBankNameEnq
    {
        public string accountNo { get; set; }
        public string bankCode { get; set; }
    }
    public class InterBankNameEnqResponse
    {
        public string accountName { get; set; }
        public string nameEnqRef { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class ValidateADUser
    {
        public string UserID { get; set; }
        public string Password { get; set; }
    }
    public class Token
    {
        public string TokenID { get; set; }
        public string OTP { get; set; }
    }
    public class billerItems
    {
        public int billerID { get; set; }
    }
    public class CategoryBiller
    {
        public string categorybillerrequest { get; set; }
    }
    public class biller {
        public int billerID { get; set; }
    }

    public class phone
    {
        public string phoneNo { get; set; }
    }

    public class accountsbyphoneno
    {
        public string phoneNo { get; set; }
    }

    public class accountsbyphonenoresponse
    {
        public List<string> AccountNumbers { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }
    public class CategoryBillerResponse
    {
        public List<CategoryBill> Billers { get; set; }
        public string responseCode { get; set; }
        public string responseDescription { get; set; }
    }
    public class billeritemsresponse
    {
        public List<Bill> BillItems { get; set; }
        public string responseCode { get; set; }
        public string responseDescription { get; set; }
    }
    public class itembill
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PaymentCode { get; set; }
        public string Amount { get; set; }
        public string CustomerID { get; set; }
        public string PaymentType { get; set; }
    }
    public class BillPaymentAdvice
    {
        [Required]
        public string CustomerEmail { get; set; }
        [Required]
        public string CustomerMobile { get; set; }
        [Required]
        public string PaymentCode { get; set; }
        public string RequestReference { get; set; }
        [Required]
        public string TerminalId { get; set; }
        [Required]
        public string CustomerId { get; set; }
        [Required]
        public string Amount { get; set; }
        public string GetMessage()
        {
            return XMLize.SerializeToString(this, true);
        }
    }
    public class XMLize
    {
        public static string SerializeToString1(object objectInstance)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

            //Add an empty namespace and empty value
            ns.Add("", "");
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance, ns);
            }
            return sb.ToString();
        }
        public static string SerializeToString(object objectInstance, bool omitdeclare = false)
        {
            var emptyNamepsaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(objectInstance.GetType());
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = omitdeclare;
            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, objectInstance, emptyNamepsaces);
                return stream.ToString();
            }
        }

       
       
        
    }
    public class BillsPaymentRequest
    {
       
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PaymentCode { get; set; }
        public string Amount { get; set; }
        public string CustomerID { get; set; }
        public string PaymentType { get; set; }

    }
    public class AccountOpeningResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountNo { get; set; }
    }

    public class validatecustomer
    {
        public string phoneNo { get; set; }
        public string accountNo { get; set; }
        public string carddetails { get; set; }
    }
    public class validatecustomerresponse
    {
        public string responseMessage { get; set; }
        public string responseCode { get; set; }
    }

    public class bvnrequest
    {
        public string bvn { get; set; }
    }
    public class AccountOpening
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string sex { get; set; }
        public string address { get; set; }
        public string telephone { get; set; }
        public string accountName { get; set; }
        public int branchcode { get; set; }
        public int curencycode { get; set; }
        public string glcode { get; set; }
        public string cif { get; set; }
        public int title{get;set;}
        public int idtype{get;set;}
        public string idno{get;set;}
        public string marital{get;set;}
        public string addref{get;set;}        
        public string Image { get; set; }        
        public string SignatureImage { get; set; }
        public string bvn { get; set; }
    }
    public class bvnvalidationresponse
    {
        public string firstname { get; set; }
        public string lastname { get; set; }

    }
    public class transhistory
    {
        public string accountNo { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
    }
    public class transhistoryresponse
    {
        public List<String> history { get; set; }
    }
    public class transdetails
    {
        public DateTime TransDate { get; set; }
        public string Narration { get; set; }
        public Decimal Amount { get; set; }
        public string TransType { get; set; }
    }
    public class logreversal 
    {
        public string sessionID { get; set; }
    }
    public class customerdetails
    {
        public string accountNo { get; set; }
    }
    /*public class customerdetailsresponse
    {
        public string balance { get; set; }
        public string firstname { get; set; }
        public string secondname { get; set; }
        public string lastname { get; set; }
        public string accountname { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string dob { get; set; }
        public string bvn { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
    }*/
    public class linkbvnrequest
    {
        public string accountNo { get; set; }
        public string bvn { get; set; }
        public string cif { get; set; }
    }
    public class customerdetailsresponse
    {
        public string balance { get; set; }
        public string firstname { get; set; }
        public string secondname { get; set; }
        public string lastname { get; set; }
        public string accountname { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string dob { get; set; }
        public string bvn { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountType { get; set; }
        public string address { get; set; }
        public string sex { get; set; }
    }
    public class vendcatrequest{
        public string method{get;set;}
    }
    public class vendcatresponse
    {
        public List<string> vendcat { get; set; }
    }
    public class vendrequest
    {
        public int catID { get; set; }
    }
    public class vendresponse
    {
        public List<string> vendcat { get; set; }
    }
    public class AirTimeVendingRequest
    {
        public string loginId { get; set; }
        public string key { get; set; }
        public string serviceId { get; set; }
        public string amount { get; set; }
        public string recipient { get; set; }
        public DateTime date { get; set; }
        public string requestId { get; set; }


    }

    public class AirTimeVendResp
    {
        public bool response { get; set; }
    }
    public class AirTimeVendingResp
    {
        public string statusCode { get; set; }
        public string statusDescription { get; set; }
        public string mReference { get; set; }
        public string tranxReference { get; set; }
        public string recipient { get; set; }
        public string amount { get; set; }
        public string confirmCode { get; set; }
        public string tranxDate { get; set; }

    }
   /* public class validatecustomerdetails
    {
        public string balance { get; set; }
        public string firstname { get; set; }
        public string secondname { get; set; }
        public string lastname { get; set; }
        public string accountname { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string dob { get; set; }
        public string bvn { get; set; }
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountType { get; set; }
        public string address { get; set; }
        public string sex { get; set; }
    }*/
    public class sendsmsrequest
    {
        public string phoneNo { get; set; }
        public string message { get; set; }
    }
    public class customerdetailsrequest
    {
        public string accountNo { get; set; }
    }
    public class otpvalidationrequest
    {
        public string accountNo { get; set; }
        public string otp { get; set; }
    }
    public class newotprequest
    {
        public string phoneNo { get; set; }
        public string otp { get; set; }
    }
    public class genotp
    {
        public string phoneNo { get; set; }
        public string msg { get; set; }
    }
  
    public class branchrequest
    {
        public string branchMethod { get; set; }
    }
    public class linkbvnresponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class billspaymentresponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class AccountDetailsResponse{
        public string balance { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string cif { get; set; }
}
    public class internalbvnvalidation
    {
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
    }
    public class bvnlinkingresponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
    }
    public class bvnlinking
    {
        public string accountNo { get; set; }
        public string bvn { get; set; }
    }
    public class Itemsresponse
    {
        public List<Item> Items { get; set; }
        public string responseCode { get; set; }
        public string responseDescription { get; set; }
    }
    public class Items
    {
        public int itemID { get; set; }
    }
    public class Base64FileJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }


        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Convert.FromBase64String(reader.Value as string);
        }

        //Because we are never writing out as Base64, we don't need this. 
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class CardNameEnquiryResponse
    {
        public string AccountName { get; set; }
        public string PhoneNumber { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }

    public class CardBalanceEnquiryResponse
    {
        public string AccountName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Balance { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }

    public class responseClass
    {
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }

    public class CardDepositRequest
    {
        public string CardCreditAccount { get; set; }
        public string DebitAccount { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string Narration { get; set; }
        public string TransactionReference { get; set; }
    }

    public class CardWithdrawalRequest
    {
        public string CreditAccount { get; set; }
        public string CardDebitAccount { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string Narration { get; set; }
        public string TransactionReference { get; set; }
    }

    public class CardBillPaymentRequest
    {
        public string ProductCode { get; set; }
        public string ReferenceID { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string CardDebitAccount { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
    }

    public class OtherBankCardsRequest
    {
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string CreditAccount { get; set; }
        public string TransactionReference { get; set; }
        public string MaskedPAN { get; set; }
        public decimal RRN { get; set; }
        public string TerminalId { get; set; }
        public string TerminalLocation { get; set; }
        public string Stan { get; set; }
    }

}