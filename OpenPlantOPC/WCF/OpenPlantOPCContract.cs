using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using OpenPlant;

namespace OpenPlantOPC
{

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class OpenPlantOPCContract : OPCBrowserContract, iOpenPlantOPCContract
    {
        //This is for IKeepAlive
        public void TrySendKeepAlivePulse()
        {
        }

        public bool TryGetLogs(DateTime GetLogsNewerThanAndExcludingThisDateTime_InUtc, out List<LogStruct> LogList, string MACAddress = "")
        {
            return Logger.TryGetLogs(GetLogsNewerThanAndExcludingThisDateTime_InUtc, out LogList);
        }

        public bool TryGetLatestLogs(int NumberOfLogs, out List<LogStruct> LogList, string MACAddress = "")
        {
            return Logger.TryGetLatestLogs(NumberOfLogs, out LogList);
        }

        public bool TryGetSettings(out OPCBackEndConfig OPCBackEndConfig)
        {
            OPCBackEndConfig = null;
            try
            {
                OPCBackEndConfig = (OPCBackEndConfig)Global.Product.Config;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TrySaveSettings(OPCBackEndConfig OPCBackEndConfig)
        {
            try
            {
                Logger.Log("Saving and applying Open-Plant OPC New Back End Configuration...");
                bool ChangeOccured = false;
                OPCBackEndConfig CurrentOPCBackEndConfig = (OPCBackEndConfig)Global.Product.Config;

                //HTTP Settings
                if (CurrentOPCBackEndConfig.EnableAPI_Http != OPCBackEndConfig.EnableAPI_Http 
                    || CurrentOPCBackEndConfig.Http_Port != OPCBackEndConfig.Http_Port)
                {
                    ChangeOccured = true;
                    if (OPCBackEndConfig.EnableAPI_Http) { OPCBackEnd.WCFHost_Http?.Stop(); OPCBackEnd.EnableHttp(); }
                    else OPCBackEnd.WCFHost_Http?.Stop();
                }

                //HTTPS Settings
                if (CurrentOPCBackEndConfig.EnableAPI_Https != OPCBackEndConfig.EnableAPI_Https
                    || CurrentOPCBackEndConfig.Https_Port != OPCBackEndConfig.Https_Port
                    || CurrentOPCBackEndConfig.RequireAPIBasicAuthentication != OPCBackEndConfig.RequireAPIBasicAuthentication
                    || CurrentOPCBackEndConfig.Username_ForAPIBasicAuthentication != OPCBackEndConfig.Username_ForAPIBasicAuthentication
                    || CurrentOPCBackEndConfig.Password_ForAPIBasicAuthentication != OPCBackEndConfig.Password_ForAPIBasicAuthentication
                    || CurrentOPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert != OPCBackEndConfig.SubjectAlternativeNames_ForHTTPSCert)
                {
                    ChangeOccured = true;
                    if (OPCBackEndConfig.EnableAPI_Https) { OPCBackEnd.WCFHost_Https?.Stop(); OPCBackEnd.EnableHttps(); }
                    else OPCBackEnd.WCFHost_Https?.Stop();
                }

                //OPC Settings
                if (CurrentOPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit != OPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit)                  
                {
                    ChangeOccured = true;
                    OPCBackEnd.OPCClassicBrowserEngine.OPCGroupSizeLimit = OPCBackEndConfig.OPCClassic_SubscriptionGroupSizeLimit.ToInt(40);
                }
                if (CurrentOPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit != OPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit)
                {
                    ChangeOccured = true;
                    OPCBackEnd.OPCUABrowserEngine.OPCGroupSizeLimit = OPCBackEndConfig.OPCCUA_SubscriptionGroupSizeLimit.ToInt(40);
                }
                if (ChangeOccured)
                {
                    Global.Product.Config = OPCBackEndConfig;
                    Global.Product.Config.TrySaveConfigFile();
                    Logger.Log("Successfully saved Open-Plant OPC Back End Configuration at '" + Global.Product.ConfigFilePath + "'");
                }
                else
                {
                    Logger.Log("Save did not execute as there were no changes made!");
                }
                return true;
            }
            catch
            {
                return false;
            }
            
        }
    }

    
}
