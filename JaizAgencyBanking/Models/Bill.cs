
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace JaizAgencyBanking.Models
{

using System;
    using System.Collections.Generic;
    
public partial class Bill
{

    public int ID { get; set; }

    public int ProviderRecognizedID { get; set; }

    public string Name { get; set; }

    public string ShortName { get; set; }

    public string Narration { get; set; }

    public string LogoUrl { get; set; }

    public string Url { get; set; }

    public string Surcharge { get; set; }

    public string CustomSectionUrl { get; set; }

    public string QuickTellerSiteUrlName { get; set; }

    public string SupportEmail { get; set; }

    public string CustomMessage { get; set; }

    public int QuickTellerCategoryId { get; set; }

    public bool Visible { get; set; }

    public string CustomerFieldLabel { get; set; }

    public Nullable<int> billerID { get; set; }

}

}
