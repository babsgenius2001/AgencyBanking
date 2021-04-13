using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JaizAgencyBanking.Models
{
    public class EmailSendingClass
    {
        public string from { get; set; }
        public string to { get; set; }
        public string subject { get; set; }
        public string message { get; set; }
    }
}