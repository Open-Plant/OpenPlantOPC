using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenPlant;

namespace OpenPlantOPC
{
    public class SettingsVM
    {
        #region BoilerPlate
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName, string P2 = null, string P3 = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            if (P2 != null) OnPropertyChanged(P2);
            if (P3 != null) OnPropertyChanged(P3);
            return true;
        }

        #endregion

        public SettingsVM() { }

        public OPCBackEndConfig ToOPCBackEndConfig()
        {
            OPCBackEndConfig oPCBackEndConfig = new OPCBackEndConfig()
            {
                EnableAPI_Http = this.EnableAPI_Http,
                EnableAPI_Https = this.EnableAPI_Https,
                Https_Port = this.Https_Port,
                Http_Port = this.Http_Port,
                OPCClassic_SubscriptionGroupSizeLimit = this.OPCClassic_SubscriptionGroupSizeLimit,
                OPCCUA_SubscriptionGroupSizeLimit = this.OPCUA_SubscriptionGroupSizeLimit,
                Password_ForAPIBasicAuthentication = this.Password_ForAPIBasicAuthentication,
                Username_ForAPIBasicAuthentication = this.Username_ForAPIBasicAuthentication,
                RequireAPIBasicAuthentication = this.RequireAPIBasicAuthentication,
                SubjectAlternativeNames_ForHTTPSCert = this.SubjectAlternativeNames_ForHTTPSCert
            };
            return oPCBackEndConfig;
        }
        public SettingsVM(OPCBackEndConfig oPCBackEndConfig)
        {
            this.EnableAPI_Http = oPCBackEndConfig.EnableAPI_Http;
            this.EnableAPI_Https = oPCBackEndConfig.EnableAPI_Https;
            this.Https_Port = oPCBackEndConfig.Https_Port;
            this.Http_Port = oPCBackEndConfig.Http_Port;
            this.OPCClassic_SubscriptionGroupSizeLimit = oPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit;
            this.OPCUA_SubscriptionGroupSizeLimit = oPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit;
            this.Username_ForAPIBasicAuthentication = oPCBackEndConfig.Username_ForAPIBasicAuthentication;
            this.Password_ForAPIBasicAuthentication = oPCBackEndConfig.Password_ForAPIBasicAuthentication;
            this.RequireAPIBasicAuthentication = oPCBackEndConfig.RequireAPIBasicAuthentication;
            this.SubjectAlternativeNames_ForHTTPSCert = oPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert;


        }

        private bool _EnableAPI_Https = false; public bool EnableAPI_Https { get { return _EnableAPI_Https; } set { SetField(ref _EnableAPI_Https, value, "EnableAPI_Https"); } }
        private bool _EnableAPI_Http = false; public bool EnableAPI_Http { get { return _EnableAPI_Http; } set { SetField(ref _EnableAPI_Http, value, "EnableAPI_Http"); } }
        private bool _RequireAPIBasicAuthentication = false; public bool RequireAPIBasicAuthentication { get { return _RequireAPIBasicAuthentication; } set { SetField(ref _RequireAPIBasicAuthentication, value, "RequireAPIBasicAuthentication"); } }
        private int _Https_Port = 33176; public int Https_Port { get { return _Https_Port; } set { SetField(ref _Https_Port, value, "Https_Port"); } }
        private int _Http_Port = 33177; public int Http_Port { get { return _Http_Port; } set { SetField(ref _Http_Port, value, "Http_Port"); } }
        private string _SubjectAlternativeNames_ForHTTPSCert = "localhost,127.0.0.1"; public string SubjectAlternativeNames_ForHTTPSCert { get { return _SubjectAlternativeNames_ForHTTPSCert; } set { SetField(ref _SubjectAlternativeNames_ForHTTPSCert, value, "SubjectAlternativeNames_ForHTTPSCert"); } }
        private string _Username_ForAPIBasicAuthentication = ""; public string Username_ForAPIBasicAuthentication { get { return _Username_ForAPIBasicAuthentication; } set { SetField(ref _Username_ForAPIBasicAuthentication, value, "Username_ForAPIBasicAuthentication"); } }
        private string _Password_ForAPIBasicAuthentication = ""; public string Password_ForAPIBasicAuthentication { get { return _Password_ForAPIBasicAuthentication; } set { SetField(ref _Password_ForAPIBasicAuthentication, value, "Password_ForAPIBasicAuthentication"); } }
        private string _OPCClassic_SubscriptionGroupSizeLimit = "30"; public string OPCClassic_SubscriptionGroupSizeLimit { get { return _OPCClassic_SubscriptionGroupSizeLimit; } set { SetField(ref _OPCClassic_SubscriptionGroupSizeLimit, value, "OPCClassic_SubscriptionGroupSizeLimit"); } }
        private string _OPCUA_SubscriptionGroupSizeLimit = "30"; public string OPCUA_SubscriptionGroupSizeLimit { get { return _OPCUA_SubscriptionGroupSizeLimit; } set { SetField(ref _OPCUA_SubscriptionGroupSizeLimit, value, "OPCUA_SubscriptionGroupSizeLimit"); } }

    }
}
