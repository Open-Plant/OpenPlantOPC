using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using OpenPlant;

namespace OpenPlantOPC
{

    //This porion of code will run on the Server

    //IMPORTANT NOTE:
    // - Add reference to 'System.ServiceModel'

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class OPCBrowserContract : iOPCBrowserContract
    {
        
        public string GetCallerIP()
        {
            string CallerIP = "";
            if (OperationContext.Current.EndpointDispatcher.EndpointAddress.Uri.Scheme == "net.pipe")
            {
                CallerIP = "Localhost Pipe";
            }
            else
            {
                OperationContext context = OperationContext.Current;
                MessageProperties properties = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;                
                if (properties.Keys.Contains(HttpRequestMessageProperty.Name))
                {
                    if (properties[HttpRequestMessageProperty.Name] is HttpRequestMessageProperty endpointLoadBalancer && endpointLoadBalancer.Headers["X-Forwarded-For"] != null)
                        CallerIP = endpointLoadBalancer.Headers["X-Forwarded-For"];
                }
                if (string.IsNullOrEmpty(CallerIP))
                {
                    CallerIP = endpoint.Address;
                }
            }
            return CallerIP;

        }


        //**************************
        //  OPC Classic 
        //**************************
        public ServerStatus_Result ServerStatus_Classic(string OPCURL)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            return OPCBackEnd.OPCClassicBrowserEngine.ServerStatus_Classic(OPCURL);
        }

        public BrowseEntireNetwork_Classic_Result BrowseEntireNetwork_Classic()
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Entire Network (OPC Classic)...");
            var Res = OPCBackEnd.OPCClassicBrowserEngine.BrowseEntireNetwork_Classic();
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Entire Network (OPC Classic) returned an error\r\n" + Res.error);
            return Res;
        }


        public BrowseMachine_Result BrowseMachine_Classic(string Host)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Machine '" + Host + "' (OPC Classic)...");
            var Res = OPCBackEnd.OPCClassicBrowserEngine.BrowseMachine_Classic(Host);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Browse Machine '" + Host + "' (OPC Classic) returned an error\r\n" + Res.error);
            return Res;
        }


        public BrowseGeneric_Result BrowseBranch_Classic(string OPCURL, string ItemId)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Branch '" + ItemId + "' for OPCURL '" + OPCURL + "' (OPC Classic)...");
            var Res = OPCBackEnd.OPCClassicBrowserEngine.BrowseBranch_Classic(OPCURL, ItemId);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Browse Branch '" + ItemId + "' for OPCURL '" + OPCURL + "' (OPC Classic) returned an error\r\n" + Res.error);
            return Res;
        }


        public BrowseTag_Classic_Result BrowseTag_Classic(string OPCURL, string ItemId)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Tag '" + ItemId + "' for OPCURL '" + OPCURL + "' (OPC Classic)...");
            var Res = OPCBackEnd.OPCClassicBrowserEngine.BrowseTag_Classic(OPCURL, ItemId);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Browse Tag '" + ItemId + "' for OPCURL '" + OPCURL + "' (OPC Classic) returned an error\r\n" + Res.error);
            return Res;
        }


        public Read_Result Read_Classic(string OPCURL, string UpdateInterval, string ItemIds)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            return OPCBackEnd.OPCClassicBrowserEngine.Read_Classic(OPCURL, UpdateInterval, ItemIds);
        }




        //**************************
        //  OPC UA
        //**************************
        public ServerStatus_Result ServerStatus_UA(string OPCURL, bool UseSecurity, string Username = "", string Password = "")
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            return OPCBackEnd.OPCUABrowserEngine.ServerStatus_UA(OPCURL,UseSecurity,Username, Password);
        }

        public BrowseMachine_Result BrowseLocalDiscoveryServer_UA(string Host, int Port)
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Local Discovery Server for Host '" + Host + "', Port '" + Port + "' (OPC UA)...");
            var Res = OPCBackEnd.OPCUABrowserEngine.BrowseLocalDiscoveryServer_UA(Host, Port);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' reques to Browse Local Discovery Server for Host '" + Host + "', Port '" + Port + "' (OPC UA) returned an error\r\n" + Res.error);
            return Res;
        }

        public BrowseGeneric_Result BrowseBranch_UA(string OPCURL, bool UseSecurity, string NodeId, string Username = "", string Password = "")
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Branch '" + NodeId + "', OPCURL '" + OPCURL + "' (OPC UA, UseSecurity=" + UseSecurity + ", Username=" + Username +")...");
            var Res = OPCBackEnd.OPCUABrowserEngine.BrowseBranch_UA(OPCURL, UseSecurity, NodeId, Username, Password);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Browse Branch '" + NodeId + "', OPCURL '" + OPCURL + "' (OPC UA, UseSecurity=" + UseSecurity + ", Username=" + Username + ") returned an error\r\n" + Res.error);
            return Res;
        }


        public BrowseAttribute_UA_Result BrowseAttribute_UA(string OPCURL, bool UseSecurity, string NodeId, string Username = "", string Password = "")
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            string CallerIP = GetCallerIP();
            Logger.Log("'" + CallerIP + "' requested to Browse Attributes for '" + NodeId + "', OPCURL '" + OPCURL + "' (OPC UA, UseSecurity=" + UseSecurity + ", Username=" + Username + ")...");
            var Res = OPCBackEnd.OPCUABrowserEngine.BrowseAttribute_UA(OPCURL, UseSecurity, NodeId, Username, Password);
            if (!Res.success) Logger.Log("ERROR: '" + CallerIP + "' request to Browse Attributes for '" + NodeId + "', OPCURL '" + OPCURL + "' (OPC UA, UseSecurity=" + UseSecurity + ", Username=" + Username + ") returned an error\r\n" + Res.error);
            return Res;
        }


        public Read_Result Read_UA(string OPCURL, bool UseSecurity, string UpdateInterval, string ItemIds, string Username = "", string Password = "")
        {
            OPCBackEnd.TotalAPICalls.Add(DateTime.UtcNow);
            return OPCBackEnd.OPCUABrowserEngine.Read_UA(OPCURL, UseSecurity, UpdateInterval, ItemIds, Username, Password);
        }


    }


}
