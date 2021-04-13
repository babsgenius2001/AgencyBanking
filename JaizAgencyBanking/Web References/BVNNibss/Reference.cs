﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace JaizAgencyBanking.BVNNibss {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3761.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="BVNValidationWebServiceSoapBinding", Namespace="http://validation.bvn.nibss.com/")]
    public partial class BVNValidationWebService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback verifySingleBVNOperationCompleted;
        
        private System.Threading.SendOrPostCallback verifyMultipleBVNsOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public BVNValidationWebService() {
            this.Url = global::JaizAgencyBanking.Properties.Settings.Default.JaizOpenDigitalBanking_BVNNibss_BVNValidationWebService;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event verifySingleBVNCompletedEventHandler verifySingleBVNCompleted;
        
        /// <remarks/>
        public event verifyMultipleBVNsCompletedEventHandler verifyMultipleBVNsCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("", RequestNamespace="http://validation.bvn.nibss.com/", ResponseNamespace="http://validation.bvn.nibss.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("searchResult", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string verifySingleBVN([System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)] string BVN, [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)] string organisationCode) {
            object[] results = this.Invoke("verifySingleBVN", new object[] {
                        BVN,
                        organisationCode});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void verifySingleBVNAsync(string BVN, string organisationCode) {
            this.verifySingleBVNAsync(BVN, organisationCode, null);
        }
        
        /// <remarks/>
        public void verifySingleBVNAsync(string BVN, string organisationCode, object userState) {
            if ((this.verifySingleBVNOperationCompleted == null)) {
                this.verifySingleBVNOperationCompleted = new System.Threading.SendOrPostCallback(this.OnverifySingleBVNOperationCompleted);
            }
            this.InvokeAsync("verifySingleBVN", new object[] {
                        BVN,
                        organisationCode}, this.verifySingleBVNOperationCompleted, userState);
        }
        
        private void OnverifySingleBVNOperationCompleted(object arg) {
            if ((this.verifySingleBVNCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.verifySingleBVNCompleted(this, new verifySingleBVNCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("", RequestNamespace="http://validation.bvn.nibss.com/", ResponseNamespace="http://validation.bvn.nibss.com/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("searchResults", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string verifyMultipleBVNs([System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)] string BVNs, [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)] string organisationCode) {
            object[] results = this.Invoke("verifyMultipleBVNs", new object[] {
                        BVNs,
                        organisationCode});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void verifyMultipleBVNsAsync(string BVNs, string organisationCode) {
            this.verifyMultipleBVNsAsync(BVNs, organisationCode, null);
        }
        
        /// <remarks/>
        public void verifyMultipleBVNsAsync(string BVNs, string organisationCode, object userState) {
            if ((this.verifyMultipleBVNsOperationCompleted == null)) {
                this.verifyMultipleBVNsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnverifyMultipleBVNsOperationCompleted);
            }
            this.InvokeAsync("verifyMultipleBVNs", new object[] {
                        BVNs,
                        organisationCode}, this.verifyMultipleBVNsOperationCompleted, userState);
        }
        
        private void OnverifyMultipleBVNsOperationCompleted(object arg) {
            if ((this.verifyMultipleBVNsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.verifyMultipleBVNsCompleted(this, new verifyMultipleBVNsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3761.0")]
    public delegate void verifySingleBVNCompletedEventHandler(object sender, verifySingleBVNCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3761.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class verifySingleBVNCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal verifySingleBVNCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3761.0")]
    public delegate void verifyMultipleBVNsCompletedEventHandler(object sender, verifyMultipleBVNsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.3761.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class verifyMultipleBVNsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal verifyMultipleBVNsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591