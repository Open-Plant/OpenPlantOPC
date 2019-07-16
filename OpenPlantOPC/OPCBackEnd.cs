using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenPlant;

namespace OpenPlantOPC
{
    public class RegisteredTag
    {
        public RegisteredTag() { }
        public RegisteredTag(string Id, int UpdateIntervalInMS)
        {
            this.Id = Id;
            this.UpdateIntervalInMS = UpdateIntervalInMS;
        }
        public string Id { get; set; } = null;
        public int UpdateIntervalInMS { get; set; } = 1000;
        public IComparable Value { get; set; } = null;
        public DateTime LastCalled { get; set; } = DateTime.MinValue;
        public DateTime TSUTC { get; set; } = DateTime.MinValue;
        public DateTime SourceTSUTC { get; set; } = DateTime.MinValue;
        public bool QualityOK { get; set; } = true;
    }

    public class OPCBackEnd
    {
        public static OPCClassicBrowserEngine OPCClassicBrowserEngine;
        public static OPCUABrowserEngine OPCUABrowserEngine;
        public static List<DateTime> TotalAPICalls = new List<DateTime>();
        public static WCFHost<OpenPlantOPCContract, iOpenPlantOPCContract> WCFHost_Pipe = null;
        public static WCFHost<OPCBrowserContract, iOPCBrowserContract> WCFHost_Http = null;
        public static WCFHost<OPCBrowserContract, iOPCBrowserContract> WCFHost_Https = null;

        public OPCBackEnd()
        {
            Logger.ClearLogs();
            if (!Global.Product.TryLoadConfigFile()) throw new CustomException("FATAL ERROR: Unable to load Config File '" + Global.Product.ConfigFilePath + "'");
            OPCBackEnd.Config = (OPCBackEndConfig)Global.Product.Config;            
            Logger.EnableLogger();
            Thread.Sleep(100);
            Logger.Log("***************************************************");
            Thread.Sleep(100);
            Logger.Log("OPEN-PLANT OPC (BACK END) STARTED");
            Thread.Sleep(100);
            Logger.Log("***************************************************");
            Thread.Sleep(100);
            Logger.Log("Log Path = '" + Global.Product.LogFilePath + "'");
            OPCBackEnd.OPCClassicBrowserEngine = new OPCClassicBrowserEngine();
            OPCBackEnd.OPCUABrowserEngine = new OPCUABrowserEngine();


            //*************************************************
            //   REPORT NUMBER OF CALLS PER MINUTE
            //*************************************************
            (new Thread(() =>
            {
                while (true)
                {
                    DateTime DTGate = DateTime.UtcNow.AddMinutes(-1);
                    int TotalCallsLastMinute = TotalAPICalls.Count(C => C > DTGate);
                    Logger.Log("Total API Calls Last Minute = " + TotalCallsLastMinute);
                    try { TotalAPICalls.RemoveWhere(C => C < DTGate); } catch { }
                    Thread.Sleep(30000);
                }
            })).Start();


            //*************************************************
            //   ENABLE NAMED PIPE FOR CONFIGURATION FRONT END
            //*************************************************
            WCFHost_Pipe = new WCFHost<OpenPlantOPCContract, iOpenPlantOPCContract>(OPCBackEnd.Config.PipeName, "");
            WCFHost_Pipe.Start();



            //****************************************
            //   ENABLE HTTP
            //****************************************
            if (OPCBackEnd.Config.EnableAPI_Http) OPCBackEnd.EnableHttp(); else Logger.Log("Http API not Enabled");


            //****************************************
            //   ENABLE HTTPS
            //****************************************
            if (OPCBackEnd.Config.EnableAPI_Https) OPCBackEnd.EnableHttps(); else Logger.Log("Https Secure API not Enabled");
        }

        static object Http_EnableOneAtATime = new object();
        public static void EnableHttp()
        {
            lock (Http_EnableOneAtATime)
            {
                Logger.Log("Enabling Http Host on Port " + OPCBackEnd.Config.Http_Port + "...");
                WCFHost_Http = new WCFHost<OPCBrowserContract, iOPCBrowserContract>(OPCBackEnd.Config.Http_Port, HostProtocolType.HTTP, OPCBackEnd.Config.RequireAPIBasicAuthentication);
                if (OPCBackEnd.Config.RequireAPIBasicAuthentication)
                {
                    Logger.Log("Enabling Username and Password Authentication for Http Host...");
                    WCFHost_Http.EnableUserAuthentication = true;
                    WCFHost_Http.OnValidatingUsernamePassword = (UN, PW) =>
                    {
                        return (UN.ToLower() == OPCBackEnd.Config.Username_ForAPIBasicAuthentication.ToLower() && PW == OPCBackEnd.Config.Password_ForAPIBasicAuthentication);
                    };
                }
                WCFHost_Http.Start();
            }
        }

        static object Https_EnableOneAtATime = new object();
        public static void EnableHttps()
        {
            lock (Https_EnableOneAtATime)
            {
                Logger.Log("Enabling Https (Secure) Host on Port " + OPCBackEnd.Config.Https_Port + "...");
                WCFHost_Https = new WCFHost<OPCBrowserContract, iOPCBrowserContract>(OPCBackEnd.Config.Https_Port, HostProtocolType.HTTPS, OPCBackEnd.Config.RequireAPIBasicAuthentication);
                if (OPCBackEnd.Config.RequireAPIBasicAuthentication)
                {
                    Logger.Log("Enabling Username and Password Authentication for Https Secure Host...");
                    WCFHost_Https.EnableUserAuthentication = true;
                    WCFHost_Https.OnValidatingUsernamePassword = (UN, PW) =>
                    {
                        return (UN.ToLower() == OPCBackEnd.Config.Username_ForAPIBasicAuthentication.ToLower() && PW == OPCBackEnd.Config.Password_ForAPIBasicAuthentication);
                    };
                }
                WCFHost_Https.Start();
            }
        }

        Serializer ConfigSerializer = new Serializer();
        //public string ProgramDataDir { get; set; } = null;
        //public string LogFilePath { get; set; } = null;
        //public string ConfigFilePath { get; set; } = null;
        public static OPCBackEndConfig Config { get; set; } = new OPCBackEndConfig();


        public void Stop()
        {
            Logger.Log("Stopping Open-Plant OPC Server...");
            OPCBackEnd.OPCClassicBrowserEngine?.Disconnect();
            OPCBackEnd.OPCUABrowserEngine?.Disconnect();
            OPCBackEnd.WCFHost_Pipe?.Stop();
            OPCBackEnd.WCFHost_Http?.Stop();
            OPCBackEnd.WCFHost_Https?.Stop();
            Logger.Log("Successfully stopped Open-Plant OPC Server!");
        }
    }
    
}