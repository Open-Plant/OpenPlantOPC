using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using OpenPlant;

/// <summary>
/// Open-Plant OPCBrowser only supports OPC DA 2.05 and OPC DA 3.00
/// </summary>
namespace OpenPlantOPC
{

    public class OPCUABrowserEngine
    {

        #region Private Variables
        //List<Opc.Ua.Serve.Server> _OPCDAServers = new List<Opc.Da.Server>();
        OpcCom.ServerEnumerator _OPCWrapper = null;
        ApplicationConfiguration _Config;
        string _SSLCertificateDirectory = null;
        List<OPCUAServer> _OPCUAServers = new List<OPCUAServer>();
        class OPCUAServer
        {
            public string URL;
            public string UserName;
            public string Password;
            public bool UseSecurity;
            public Session Session = null;
        }
        #endregion

        #region Properties
        public int OPCGroupSizeLimit { get; set; } = 40;
        public Session LastUsedOPCUASession { get; set; } = null;
        public Opc.Specification CurrentSpecification { get; set; } = Opc.Specification.UA10;
        #endregion


        public OPCUABrowserEngine(string SSLCertificateDirectory = null)
        {
            this.OPCGroupSizeLimit = OPCBackEnd.Config.OPCCUA_SubscriptionGroupSizeLimit.ToInt(40);
            #region GENERATE APPLICATION CONFIGURATION
            _OPCWrapper = new OpcCom.ServerEnumerator();
            if (SSLCertificateDirectory == null) _SSLCertificateDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Open-Plant\\OpenPlantOPC\\Certs";
            else _SSLCertificateDirectory = SSLCertificateDirectory;
            _Config = new ApplicationConfiguration()
            {
                ApplicationName = "OpenPlantOPCUABrowser",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    //This is the App's certificate that should exist it the 'Own' directory
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Directory",
                        StorePath = _SSLCertificateDirectory + "\\Own",
                        SubjectName = Utils.Format(@"CN={0}, DC={1}", "OpenPlantOPCUABrowser", System.Net.Dns.GetHostName())
                    },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = _SSLCertificateDirectory + "\\Trusted", },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = _SSLCertificateDirectory + "\\Issuers", },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = _SSLCertificateDirectory + "\\Rejected", },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                DisableHiResClock = true
            };
            _Config.Validate(ApplicationType.Client); //This only checks if any certificates exists (in the predefined area). If no certificates exist, nothing will happen.
            #endregion


            OPTimer CheckInactivityTimer = new OPTimer(20000);
            CheckInactivityTimer.Elapsed += (s) =>
            {
                List<RegisteredTag> InactiveTags = this.RegisteredTags.Values.Where(RT =>
                {
                    if (RT.UpdateIntervalInMS <= 20000) return (DateTime.Now - RT.LastCalled).TotalMilliseconds > 60000;
                    else return (DateTime.Now - RT.LastCalled).TotalMilliseconds > 3 * RT.UpdateIntervalInMS;
                }).ToList();
                foreach (RegisteredTag InactiveTag in InactiveTags)
                {
                    foreach (OPCUAServer OPCServer in _OPCUAServers)
                    {
                        if (OPCServer.Session == null || !OPCServer.Session.Connected) continue;
                        //ILocalNode NodeIdObject = OPCServer.Session.NodeCache.Find(new NodeId(InactiveTag.Id)) as ILocalNode;
                        Subscription SubscriptionWhereInactiveTagIs = OPCServer.Session.Subscriptions.FindSubcriptionThatHasItem(InactiveTag.Id, out MonitoredItem InactiveItem);
                        if (SubscriptionWhereInactiveTagIs != null) SubscriptionWhereInactiveTagIs.RemoveItem(InactiveItem);
                    }
                    this.RegisteredTags.Remove(InactiveTag.Id);
                }
            };
            CheckInactivityTimer.Start();
        }

        public void Disconnect()
        {
            foreach (var OPCServer in _OPCUAServers) try { OPCServer.Session?.Close(); } catch { }
        }

        public BrowseMachine_Result BrowseLocalDiscoveryServer_UA(string MachineIPAddress)
        {
            return BrowseLocalDiscoveryServer_UA(MachineIPAddress, 4840);
        }


        public BrowseMachine_Result BrowseLocalDiscoveryServer_UA(string Host, int Port)
        {
            List<string> serverUrls = new List<string>();

            List<OPCServerNode> OPCServers = new List<OPCServerNode>();
            try
            {
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(_Config);
                endpointConfiguration.OperationTimeout = 25000;
                using (DiscoveryClient client = DiscoveryClient.Create(new Uri("opc.tcp://" + Host + ":" + Port), endpointConfiguration))
                {
                    ApplicationDescriptionCollection servers = client.FindServers(null);
                    for (int ii = 0; ii < servers.Count; ii++)
                    {
                        if (servers[ii].ApplicationType == ApplicationType.DiscoveryServer) continue;
                        for (int jj = 0; jj < servers[ii].DiscoveryUrls.Count; jj++)
                        {
                            string discoveryUrl = servers[ii].DiscoveryUrls[jj];

                            // Many servers will use the '/discovery' suffix for the discovery endpoint.
                            // The URL without this prefix should be the base URL for the server. 
                            if (discoveryUrl.EndsWith("/discovery"))
                            {
                                discoveryUrl = discoveryUrl.Substring(0, discoveryUrl.Length - "/discovery".Length);
                            }

                            // ensure duplicates do not get added.
                            if (!serverUrls.Contains(discoveryUrl))
                            {
                                if (discoveryUrl.StartsWith("http")) continue; 
                                OPCServers.Add(new OPCServerNode(discoveryUrl, discoveryUrl, OPCSpecification.UA_10));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new BrowseMachine_Result() { success = false, result = null, error = ex.ToString() };
            }
            return new BrowseMachine_Result() { success = true, result = OPCServers };            
        }


        public ServerStatus_Result ServerStatus_UA(string OPCURL, bool UseSecurity, string Username, string Password)
        {
            ServerStatus_Result ServerStatus_Result = new ServerStatus_Result();
            try
            {
                if (this.TryConnectToOPCUADAServer(OPCURL, Username, Password, UseSecurity, out OPCUAServer oPCUAServer, out Exception Exception))
                {
                    ServerStatus_Result.result = !oPCUAServer.Session.KeepAliveStopped;
                    ServerStatus_Result.success = true;
                    return ServerStatus_Result;
                }
                else return new ServerStatus_Result() { success = false, error = "Unable to connect to OPC UA Server\r\n"+ Exception.ToString() };
            }
            catch (Exception ex)
            {
                return new ServerStatus_Result() { success = false, error = ex.ToString() };
            }
        }



        /// <param name="OPCURL"></param>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        /// <param name="NodeId">The fully qualified name of the Node. If NodeId is null, it will browse the top most level. For Example:
        /// - ns=2;s=1:Pipe1001?Measurement
        /// - ns=2;s=MyTemperature
        /// - i=2045
        /// - ns=1;g=09087e75-8e5e-499b-954f-f2a9603db28a
        /// - ns=1;b=M/RbKBsRVkePCePcx24oRA==
        /// </param>
        /// <param name="Exception"></param>
        /// <param name="UseSecurity"></param>
        /// <returns></returns>
        public BrowseGeneric_Result BrowseBranch_UA(string OPCURL, bool UseSecurity, string NodeId, string Username, string Password)
        {
            List<BaseNode> Output = new List<BaseNode>();
            try
            {
                if (this.TryConnectToOPCUADAServer(OPCURL,Username,Password, UseSecurity, out OPCUAServer oPCUAServer, out Exception Exception))
                {
                    try
                    {
                        NodeId NID;
                        if (string.IsNullOrWhiteSpace(NodeId)) NID = ObjectIds.ObjectsFolder; else NID = new NodeId(NodeId);
                        
                        BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection
                        {
                            new BrowseDescription
                            {
                                NodeId = NID,
                                BrowseDirection = BrowseDirection.Forward,
                                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                                IncludeSubtypes = true,
                                NodeClassMask = (uint)NodeClass.Variable | (uint)NodeClass.Object,
                                ResultMask = (uint)(int)BrowseResultMask.All
                            }
                        };
                        //oPCUAServer.Session.Browse(null, null, NID, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable | (uint)NodeClass.Object, out byte[] cp, out ReferenceDescriptionCollection refs);
                        oPCUAServer.Session.Browse(null, null, 0, nodesToBrowse, out BrowseResultCollection BrowseResults, out DiagnosticInfoCollection DiagnosticInfos);
                        if (BrowseResults.Count != 1 || StatusCode.IsBad(BrowseResults[0].StatusCode))
                        {
                            string ExStr = BrowseResults[0].StatusCode.ToString();
                            Exception = new Exception(ExStr);
                            return new BrowseGeneric_Result() { success = false, error = Exception.ToString() };
                        }
                        for (int ii = 0; ii < BrowseResults[0].References.Count; ii++)
                        {
                            ReferenceDescription reference = BrowseResults[0].References[ii];
                            if (reference.NodeClass == NodeClass.Object)
                            {
                                Output.Add(new BranchNode(reference.BrowseName.Name,reference.NodeId.ToString(), OPCURL, Username,Password, UseSecurity));
                            }
                            else if (reference.NodeClass == NodeClass.Variable)
                            {
                                Output.Add(new TagNode(reference.BrowseName.Name, OPCURL, reference.NodeId.ToString(), Username, Password));
                            }
                        }
                        while (BrowseResults[0].ContinuationPoint != null && BrowseResults[0].ContinuationPoint.Length > 0)
                        {
                            ByteStringCollection continuationPoints = new ByteStringCollection();
                            continuationPoints.Add(BrowseResults[0].ContinuationPoint);
                            oPCUAServer.Session.BrowseNext(null, false, continuationPoints, out BrowseResults, out DiagnosticInfos);
                            if (BrowseResults.Count != 1 || StatusCode.IsBad(BrowseResults[0].StatusCode))
                            {
                                string ExStr = BrowseResults[0].StatusCode.ToString();
                                Exception = new Exception(ExStr);
                                return new BrowseGeneric_Result() { success = false, error = Exception.ToString() };
                            }

                            for (int ii = 0; ii < BrowseResults[0].References.Count; ii++)
                            {
                                ReferenceDescription reference = BrowseResults[0].References[ii];
                                if (reference.NodeClass == NodeClass.Object)
                                {
                                    Output.Add(new BranchNode(reference.BrowseName.Name, OPCURL, NodeId, Username, Password, UseSecurity));
                                }
                                else if (reference.NodeClass == NodeClass.Variable)
                                {
                                    Output.Add(new TagNode(reference.BrowseName.Name, OPCURL, NodeId, Username, Password, UseSecurity));
                                }
                            }
                        }


                        //Add Property Nodes (Attributes)
                        foreach (BaseNode BN in Output)
                        {
                            if (BN is TagNode TN)
                            {
                                //Get Attributes
                                BrowseAttribute_UA_Result BrowseAttribute_UA_Result = this.BrowseAttribute_UA(OPCURL, UseSecurity, TN.NodeId, Username, Password);
                                if (BrowseAttribute_UA_Result.success)
                                {
                                    foreach (Attribute A in BrowseAttribute_UA_Result.result)
                                    {
                                        
                                        if (A.AttributeID == 5) TN.Description = A.Value.ToString();
                                        //if (A.AttributeID == 6) TN.AccessLevel = A.Value.ToA
                                        if (A.AttributeID == 13)
                                        {
                                            TN.Value = A.Value; //Value Attribute
                                            TN.QualityOK = A.QualityOk;
                                        }
                                        if (A.AttributeID == 14) TN.DataTypeString = A.Value.ToString(); //Data Type Attribute
                                        TN.Properties.Add(new PropertyNode(A.Name, A.Name, A.Value, true));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new BrowseGeneric_Result() { success = false, error = ex.ToString() };
                    }
                }
                else { return new BrowseGeneric_Result() { success = false, error = Exception.ToString() }; ; }
            }
            catch (Exception ex)
            {
                return new BrowseGeneric_Result() { success = false, error = ex.ToString() };
            }
            return new BrowseGeneric_Result() { success = true, result = Output };
        }
        

        public BrowseAttribute_UA_Result BrowseAttribute_UA(string OPCURL, bool UseSecurity, string NodeId, string Username, string Password)
        {
            List<Attribute> Output = new List<Attribute>();
            try
            {
                if (this.TryConnectToOPCUADAServer(OPCURL, Username, Password, UseSecurity, out OPCUAServer oPCUAServer, out Exception Exception))
                {
                    try
                    {
                        NodeId NID;                        
                        if (string.IsNullOrWhiteSpace(NodeId)) NID = ObjectIds.ObjectsFolder; else NID = new NodeId(NodeId);
                        ILocalNode node = oPCUAServer.Session.NodeCache.Find(NID) as ILocalNode;

                        if (node == null) { throw new Exception("OPC UA Server '" + OPCURL + "' was unable to find Node ID '" + NodeId + "'"); };

                        uint[] AttributesIds = Attributes.GetIdentifiers(); //Go through all system Attributes
                        foreach (uint AttributeId in AttributesIds)
                        {
                            if (!node.SupportsAttribute(AttributeId)) continue;
                            ServiceResult result = node.Read(null, AttributeId, new Opc.Ua.DataValue(StatusCodes.BadWaitingForInitialData));
                            if (ServiceResult.IsBad(result)) continue;

                            //Get Attribute Name and ID
                            Attribute AttributeToAdd = new Attribute();
                            AttributeToAdd.AttributeID = AttributeId;
                            AttributeToAdd.Name = Attributes.GetBrowseName(AttributeId);


                            //Get Attribute Type
                            Type AttributeType = null;
                            if (AttributeId != 0 && AttributeId != Attributes.Value)
                            {
                                BuiltInType builtInType = TypeInfo.GetBuiltInType(Attributes.GetDataTypeId(AttributeId));
                                int valueRank = Attributes.GetValueRank(AttributeId);
                                AttributeType = TypeInfo.GetSystemType(builtInType, valueRank);
                            }
                            else
                            {
                                IVariableBase variable = node as IVariableBase;
                                if (variable != null)
                                {
                                    BuiltInType builtInType = TypeInfo.GetBuiltInType(variable.DataType, oPCUAServer.Session.TypeTree);
                                    int valueRank = variable.ValueRank;
                                    AttributeType = TypeInfo.GetSystemType(builtInType, valueRank);
                                    if (builtInType == BuiltInType.ExtensionObject && valueRank < 0)
                                    {
                                        AttributeType = TypeInfo.GetSystemType(variable.DataType, oPCUAServer.Session.Factory);
                                        if (AttributeType == null) AttributeType = variable.GetType();
                                    }
                                }
                            }
                            AttributeToAdd.DataType = AttributeType.ToString();
                            Output.Add(AttributeToAdd);                            
                        }


                        //Get Values. Values are all obtained in one shot
                        ReadValueIdCollection valuesToRead = new ReadValueIdCollection();
                        foreach (Attribute Attribute in Output) valuesToRead.Add(new ReadValueId() { NodeId = NID, AttributeId = Attribute.AttributeID } );                        
                        oPCUAServer.Session.Read(null, 0, TimestampsToReturn.Neither, valuesToRead, out DataValueCollection results, out DiagnosticInfoCollection diagnosticInfos);
                        ClientBase.ValidateResponse(results, valuesToRead);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToRead);

                        for (int ii = 0; ii < valuesToRead.Count; ii++)
                        {
                            uint AttId = valuesToRead[ii].AttributeId;
                           
                            Attribute AttributeToAssignValue = Output[ii];
                            AttributeToAssignValue.QualityOk = !StatusCode.IsBad(results[ii].StatusCode);
                            if (results[ii].Value == null)
                            {
                                AttributeToAssignValue.Value = "";
                            }
                            else if (results[ii].Value.IsNumericType())
                            {
                                AttributeToAssignValue.Value = results[ii].Value as IComparable;
                                if (AttributeToAssignValue.Value == null) AttributeToAssignValue.Value = Convert.ToDouble(results[ii].Value.ToDouble());
                            }
                            else
                            {
                                if (AttId == 14) //if it is DataType Node, find the corresponding name
                                {
                                    NodeId NID2 = new NodeId(results[ii].Value.ToString());
                                    ILocalNode node2 = oPCUAServer.Session.NodeCache.Find(NID2) as ILocalNode;
                                    AttributeToAssignValue.Value = node2.ToString();
                                }
                                else AttributeToAssignValue.Value = results[ii].Value.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new BrowseAttribute_UA_Result() { success = false, error = ex.ToString() };
                    }
                }
                else { Exception = new Exception("Fail to Connect to OPC Server '" + OPCURL + "'"); return new BrowseAttribute_UA_Result() { success = false, error = Exception.ToString() }; }
            }
            catch (Exception ex)
            {
                return new BrowseAttribute_UA_Result() { success = false, error = ex.ToString() };
            }
            return new BrowseAttribute_UA_Result() { success = true, result = Output };
        }


        public Dictionary<string, RegisteredTag> RegisteredTags = new Dictionary<string, RegisteredTag>();


        public Read_Result Read_UA(string OPCURL, bool UseSecurity, string UpdateInterval, string NodeIds, string Username, string Password)
        {
            try
            {
                if (this.TryConnectToOPCUADAServer(OPCURL, Username, Password, UseSecurity, out OPCUAServer oPCUAServer, out Exception Exception))
                {
                    Read_Result Read_Result = new Read_Result(); RegisteredTag NewRegisteredTag;
                    if (NodeIds.IsNullOrWhiteSpace()) return Read_Result;
                    string[] NodeIdsSplit = NodeIds.Split(','); ILocalNode NodeIdObject = null;
                    int UpdateIntervalInMS = UpdateInterval.ToInt(1000);
                    foreach (string NodeId in NodeIdsSplit)
                    {
                        if (NodeId.IsNullOrWhiteSpace()) continue;
                        //Check if the Item has already been subscribed 
                        if (this.RegisteredTags.TryGetValue(NodeId, out RegisteredTag FoundRegisteredTag))
                        {
                            //Fast and Straight forward case, simply obtain data. 
                            if (UpdateIntervalInMS >= FoundRegisteredTag.UpdateIntervalInMS)
                            {
                                FoundRegisteredTag.LastCalled = DateTime.Now;
                                Read_Result.result.Add(new DataValue(FoundRegisteredTag.Id, true, FoundRegisteredTag.TSUTC, FoundRegisteredTag.SourceTSUTC, FoundRegisteredTag.Value, FoundRegisteredTag.QualityOK));
                                continue;
                            }

                            //If the Update Interval is more Frequent we need to remove the tag and subscribe it to a group which has higher update rate
                            else
                            {
                                //NodeIdObject = oPCUAServer.Session.NodeCache.Find(new NodeId(NodeId)) as ILocalNode;
                                FoundRegisteredTag.UpdateIntervalInMS = UpdateIntervalInMS;
                                Subscription SubscriptionWhereTagIs = oPCUAServer.Session.Subscriptions.FindSubcriptionThatHasItem(NodeId, out MonitoredItem ItemFound);
                                if (SubscriptionWhereTagIs != null) SubscriptionWhereTagIs.RemoveItems(new MonitoredItem[] { ItemFound }); //Remove From Subscription                                
                            }
                        }

                        //First check if the tag actually exists on the server. If it doesn't exist proceed to next tag
                        if (NodeIdObject == null) NodeIdObject = oPCUAServer.Session.NodeCache.Find(new NodeId(NodeId)) as ILocalNode;
                        if (NodeIdObject == null)
                        {
                            Read_Result.result.Add(new DataValue(NodeId, false, DateTime.UtcNow, DateTime.MinValue, null, false));
                            continue;
                        }

                        //Tag WAS NOT found in Registered Tags Dictionary (or was removed due to update interval changed)
                        //Check if there are any subscriptions which have same Update Interval and has room for items
                        NewRegisteredTag = new RegisteredTag(NodeId, UpdateIntervalInMS);
                        Opc.Ua.DataValue readResult;
                        Subscription SuitableSubscription = oPCUAServer.Session.Subscriptions.FindSubcription(S => S.PublishingInterval == UpdateIntervalInMS && S.MonitoredItemCount < this.OPCGroupSizeLimit);
                        if (SuitableSubscription != null)
                        {
                            //if a suitable subscription was found, Add item to this subscription
                            SuitableSubscription.AddItem(new MonitoredItem(SuitableSubscription.DefaultItem)
                            {
                                DisplayName = SuitableSubscription.Session.NodeCache.GetDisplayText(new ReferenceDescription() { NodeId = NodeId }),
                                StartNodeId = NodeIdObject.NodeId,
                                //TagName = NewTag.TagName,
                                //Parameter = NewTag.Parameter
                            });

                            //After succesfully added Item to Existing Subscription, read the values of the item
                            readResult = SuitableSubscription.Session.ReadValue(NodeIdObject.NodeId);
                        }
                        else
                        {
                            //If no Subscriptions found, create new Subscription
                            Subscription NewSubscription = new Subscription(oPCUAServer.Session.DefaultSubscription) { DisplayName = Guid.NewGuid().ToString(), PublishingInterval = UpdateIntervalInMS };
                            oPCUAServer.Session.AddSubscription(NewSubscription);
                            NewSubscription.AddItem(new MonitoredItem(NewSubscription.DefaultItem)
                            {
                                DisplayName = NewSubscription.Session.NodeCache.GetDisplayText(new ReferenceDescription() { NodeId = NodeId }),
                                StartNodeId = NodeIdObject.NodeId,
                                //TagName = NewTag.TagName,
                                //Parameter = NewTag.Parameter
                            });
                            NewSubscription.FastDataChangeCallback = FastDataChangeNotificationEventHandler;
                            NewSubscription.Create();
                            readResult = NewSubscription.Session.ReadValue(NodeIdObject.NodeId);
                        }

                        //Use Server TimeStamp if source time stamp is very old (can be due to faulty device)
                        NewRegisteredTag.TSUTC = DateTime.UtcNow;
                        NewRegisteredTag.SourceTSUTC = readResult.SourceTimestamp;
                        if (StatusCode.IsGood(readResult.StatusCode))
                        {
                            NewRegisteredTag.QualityOK = true;
                            if (readResult.Value is string || readResult.Value.IsNumericType()) NewRegisteredTag.Value = (IComparable)readResult.Value;
                        }
                        else
                        {
                            NewRegisteredTag.QualityOK = false;
                            NewRegisteredTag.Value = null;
                        }
                        Read_Result.result.Add(new DataValue(NewRegisteredTag.Id, true, NewRegisteredTag.TSUTC, NewRegisteredTag.SourceTSUTC, NewRegisteredTag.Value, NewRegisteredTag.QualityOK));


                        //Add New Tag to the Registered Tags
                        NewRegisteredTag.LastCalled = DateTime.Now;
                        RegisteredTags.Add(NodeId, NewRegisteredTag);
                    } //Go to next tag
                    return Read_Result;
                }
                else
                {
                    return new Read_Result() { success = false, result = null, error = "Fail to Connect to OPC UA Server '" + OPCURL + "'\r\n" + Exception.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new Read_Result() { success = false, error = ex.ToString() };
            }
        }


        private void FastDataChangeNotificationEventHandler(Subscription Subscription, DataChangeNotification notification, IList<string> stringTable)
        {
            foreach (MonitoredItem MonitoredItem in Subscription.MonitoredItems)
            {
                foreach (Opc.Ua.DataValue Value in MonitoredItem.DequeueValues())
                {
                    //Update the Registered Tags Dictionary
                    if (this.RegisteredTags.TryGetValue(MonitoredItem.StartNodeId.ToString(), out RegisteredTag Tag))
                    {
                        Tag.TSUTC = DateTime.UtcNow;
                        Tag.SourceTSUTC = Value.SourceTimestamp;
                        if (StatusCode.IsGood(Value.StatusCode))
                        {
                            Tag.QualityOK = true;
                            if (Value.Value is string || Value.Value.IsNumericType()) Tag.Value = (IComparable)Value.Value;
                        }
                        else
                        {
                            Tag.QualityOK = false;
                            Tag.Value = null;
                        }
                    }
                }
            }
        }


        private bool TryConnectToOPCUADAServer(string OPCUADAURL, string Username, string Password, bool UseSecurity, out OPCUAServer OPCUAServerObject, out Exception Ex)
        {
            OPCUAServerObject = null;
            try
            {
                OPCUAServerObject = _OPCUAServers.FirstOrDefault(S => S.URL == OPCUADAURL);
                if (OPCUAServerObject == null) _OPCUAServers.Add(OPCUAServerObject = new OPCUAServer() { URL = OPCUADAURL, UserName = Username, Password = Password, UseSecurity = UseSecurity });
                if (OPCUAServerObject.Session == null || !OPCUAServerObject.Session.Connected)
                {
                    #region CONNECT TO OPCUA SERVER

                    //Start Connecting to OPC UA Server
                    Match M = Regex.Match(OPCUADAURL, @"\/\/.*\:");
                    if (M.Success)
                    {
                        string OPCUADAHost = M.Value;
                        try { OPCUADAHost = OPCUADAHost.Substring(2, OPCUADAHost.Length - 3); }
                        catch (Exception ex)
                        {
                            Ex = new Exception("ERROR: Unable to obtain OPCUAHost from OPCUA URL. OPCUA URL may be invalid (OPCUA URL = '" + OPCUADAURL + "'). OPCUADA URL must be in the form of 'opc.tcp://[IP Address]:[Port]' such as 'opc.tcp://opserver.webredirect.org:48010'  \r\n" + ex.ToString());
                            return false;
                        }
                    }


                    if (UseSecurity)
                    {
                        //This creates a new certificate if a certificate with CN=ApplicationName does not exist. It also generates the private key and makes it trusted (adds it to the trusted store)
                        #region PERFORM SSL SECURITY VALIDATION
                        try
                        {
                            X509Certificate2 Certificate = _Config.SecurityConfiguration.ApplicationCertificate.Find(true); // find the existing certificate.
                            if (Certificate != null) //If certificate is found
                            {
                                //Validate the APPLICATION's Certificate, it must be signed correctly and trusted (in the trusted list)
                                if (!ValidateCertificate(_Config, Certificate, out Ex)) Certificate = null;
                            }
                            else //If certificate is not found
                            {
                                Certificate = _Config.SecurityConfiguration.ApplicationCertificate.Find(false); // check for missing private key.
                                if (Certificate != null) // If certificate exist but it's private key does not exist
                                {
                                    Ex = new Exception("ERROR: Application's SSL Certificates was found but private key could not be found/accessed. Subject='" + Certificate.Subject + "'");
                                    return false;
                                }
                            }
                            if (Certificate == null)
                            {
                                Certificate = ApplicationInstance.CreateApplicationInstanceCertificate(_Config, 2048, 600); // create a new certificate.                    
                            }
                            ApplicationInstance.AddToTrustedStore(_Config, Certificate); //Ensure it is trusted by placing it in the trusted certificate store
                        }
                        catch (ServiceResultException srex)
                        {
                            Ex = new Exception("ERROR: While trying to obtain Application Certificate\r\n" + srex.ToString());
                            return false;
                        }
                        catch (Exception ex)
                        {
                            Ex = new Exception("ERROR: While trying to obtain Application Certificate\r\n" + ex.ToString());
                            return false;
                        }
                        if (_Config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                        {
                            _Config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
                        }
                        #endregion
                    }

                    #region CREATE A CONNECTION SESSION WITH THE OPC UA SERVER
                    try
                    {
                        UserIdentity Identity = null; //This means to use aunonymous authentication
                        if (!string.IsNullOrWhiteSpace(Username)) Identity = new UserIdentity(Username, Password);
                        EndpointDescription endpointDescription = new EndpointDescription(OPCUADAURL);
                        if (UseSecurity) endpointDescription.SecurityMode = MessageSecurityMode.SignAndEncrypt; else endpointDescription.SecurityMode = MessageSecurityMode.None;
                        OPCUAServerObject.Session = Session.Create(_Config, new ConfiguredEndpoint(null, endpointDescription), true, "", 60000, Identity, null);
                    }
                    catch (ServiceResultException srex)
                    {
                        switch (srex.StatusCode)
                        {
                            case StatusCodes.BadSecurityChecksFailed:
                                Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] While trying to create a session with the OPCUADA server '" + OPCUADAURL + "'\r\nCheck if the application's certificate stored at '" + _SSLCertificateDirectory + "\\own' has been trusted by the OPC UA server\r\n" + srex.ToString());
                                break;
                            case StatusCodes.BadSecureChannelClosed:
                                Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] While trying to create a session with the OPCUADA server '" + OPCUADAURL + "'\r\nCheck if the application's certificate stored at '" + _SSLCertificateDirectory + "\\own' has been trusted by the OPC UA server\r\n" + srex.ToString());
                                break;
                            case StatusCodes.BadTcpInternalError:
                                Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] While trying to create a session with the OPCUADA server '" + OPCUADAURL + "'\r\nCheck if the OPC UA server is online\r\n" + srex.ToString());
                                break;
                            case StatusCodes.BadCertificateUntrusted:
                                if (!UseSecurity) Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] While trying to create a session with the OPCUADA server '" + OPCUADAURL + "'\r\nSince 'UseSecurity' has been configured as 'false', it may be that the server does not allow NO SECURITY. Try set this to 'false'\r\n" + srex.ToString());
                                else Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] While trying to create a session with the OPCUADA server\r\n" + srex.ToString());
                                break;
                            case StatusCodes.BadUserAccessDenied:
                                if (string.IsNullOrWhiteSpace(Username))
                                    Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] The server does not accept anonymous login. Please obtain an accepted Username and Password from the OPC UA Server '" + OPCUADAURL + "'\r\n" + srex.ToString());
                                else
                                    Ex = new Exception("ERROR: [" + StatusCodeNameDirectory.Names[srex.StatusCode] + "] The Username/Password ws not accepted by the OPC UA DA Server '" + OPCUADAURL + "'\r\n" + srex.ToString());
                                break;
                            default:
                                Ex = new Exception("ERROR: [" + srex.StatusCode.ToString() + "] While trying to create a session with the OPCUADA server\r\n" + srex.ToString());
                                break;

                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        OPCUAServerObject.Session = null;
                        Ex = new Exception("ERROR: While trying to create a session with the OPCUADA server\r\n" + ex.ToString());
                        return false;
                    }

                    #endregion

                    #endregion
                }
            }
            catch (Exception ex)
            {
                if (OPCUAServerObject != null)
                {
                    OPCUAServerObject.Session?.Dispose();
                    OPCUAServerObject.Session = null;
                    OPCUAServerObject = null;
                }
                throw ex;
            }
            this.LastUsedOPCUASession = OPCUAServerObject.Session;
            Ex = null;
            return true;
        }
        
        
        private bool ValidateCertificate(ApplicationConfiguration Config, X509Certificate2 Certificate, out Exception Exception)
        {
            bool ValidationResult; Exception = null;
            try
            {
                ValidationResult = ApplicationInstance.CheckApplicationInstanceCertificate(Config, Certificate, true, 2048);
            }
            catch (ServiceResultException srex)
            {
                if (srex.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    if (Config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                    {
                        ApplicationInstance.AddToTrustedStore(Config, Certificate);
                        return ValidateCertificate(Config, Certificate, out Exception);
                    }
                    else
                    {
                        return false;
                    }
                }
                //ApplicationInstance.AddToTrustedStore(Config, Certificate);
                ValidationResult = false;
            }
            catch (Exception ex) { Exception = new Exception("ERROR: Certificate Validation failed\r\n" + ex.ToString()); ValidationResult = false; }
            return ValidationResult;
        }


    }


}
