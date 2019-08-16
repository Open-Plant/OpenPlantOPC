using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc;
using Opc.Da;
using OpenPlant;
/// <summary>
/// Open-Plant OPCBrowser only supports OPC DA 2.05 and OPC DA 3.00
/// </summary>
namespace OpenPlantOPC
{

    public class OPCClassicBrowserEngine
    {
        #region Private Variables
        List<Opc.Da.Server> _OPCDAServers = new List<Opc.Da.Server>();
        OpcCom.ServerEnumerator _OPCWrapper;
        Opc.Da.BrowseFilters _BrowseFilters;
        #endregion

        #region Properties
        //public bool IsConnectedToOPCDAServer { get { if (oPCDAServer == null) return false; else return oPCDAServer.IsConnected; } }
        #endregion


        public OPCClassicBrowserEngine()
        {
            this.OPCGroupSizeLimit = OPCBackEnd.Config.OPCClassic_SubscriptionGroupSizeLimit.ToInt(40);
            _OPCWrapper = new OpcCom.ServerEnumerator();
            _BrowseFilters = new Opc.Da.BrowseFilters()
            {
                ReturnAllProperties = true,
                ReturnPropertyValues = true,
                BrowseFilter = Opc.Da.browseFilter.all,
                MaxElementsReturned = 0
            };


            //Check for Tag Inactivity every 5s. Every call by client will update it's activity.
            //If tag has been inactive for more than 3x it's update interval, remove the tag from subscription (5s min inactivity time)
            OPTimer CheckInactivityTimer = new OPTimer(5000);
            CheckInactivityTimer.Elapsed += (s) =>
            {
                List<RegisteredTag> InactiveTags = this.RegisteredTags.Values.Where(RT => 
                {
                    if (RT.UpdateIntervalInMS <= 5000) return (DateTime.Now - RT.LastCalled).TotalMilliseconds > 5000;
                    else return (DateTime.Now - RT.LastCalled).TotalMilliseconds > 3 * RT.UpdateIntervalInMS;
                }).ToList();
                foreach (RegisteredTag InactiveTag in InactiveTags)
                {
                    foreach (Opc.Da.Server OPCServer in _OPCDAServers)
                    {
                        if (!OPCServer.IsConnected) continue;
                        Subscription SubscriptionWhereInactiveTagIs = OPCServer.Subscriptions.FindSubcriptionThatHasItem(InactiveTag.Id, out Item InactiveItem);
                        if (SubscriptionWhereInactiveTagIs != null) SubscriptionWhereInactiveTagIs.RemoveItems(new ItemIdentifier[] { InactiveItem });
                    }
                    this.RegisteredTags.Remove(InactiveTag.Id);
                }
            };
            CheckInactivityTimer.Start();
        }


        public int OPCGroupSizeLimit { get; set; } = 40;
        public Opc.Specification CurrentSpecification { get; set; } = Opc.Specification.COM_DA_20;


        public void Disconnect()
        {
            foreach (var OPCServer in _OPCDAServers) try { OPCServer?.Disconnect(); } catch { }
        }


        #region Browse Procedures

        public BrowseEntireNetwork_Classic_Result BrowseEntireNetwork_Classic()
        {
            List<MachineNode> MachineNodes = new List<MachineNode>();
            try
            {                
                string[] Machines = _OPCWrapper.EnumerateHosts();
                foreach (string Machine in Machines) MachineNodes.Add(new MachineNode(Machine));
                return new BrowseEntireNetwork_Classic_Result()
                {
                    success = true,
                    result = MachineNodes
                };
            }
            catch (Exception ex)
            {
                MachineNodes = null;
                return new BrowseEntireNetwork_Classic_Result()
                {
                    success = false,
                    result = null,
                    error = ex.ToString()
                };
            }
        }



        public ServerStatus_Result ServerStatus_Classic(string OPCURL)
        {
            ServerStatus_Result ServerStatus_Result = new ServerStatus_Result();
            Opc.Da.Server ConnectedOPDAServer;
            try
            {
                ConnectedOPDAServer = ConnectToOPCServer(OPCURL);
                if (ConnectedOPDAServer == null) return new ServerStatus_Result() { success = false, error = "Fail to Connect to OPC Server '" + OPCURL + "'" };
                ServerStatus serverStatus = ConnectedOPDAServer.GetStatus();
                ServerStatus_Result.result = (serverStatus.ServerState == serverState.running);
                ServerStatus_Result.success = true;
                return ServerStatus_Result;
            }
            catch (Exception ex)
            {
                return new ServerStatus_Result() { success = false, error = ex.ToString() };
            }
        }


        public BrowseMachine_Result BrowseMachine_Classic(string Host)
        {
            List<OPCServerNode> OPCServers = new List<OPCServerNode>();
            try
            {
                Opc.Server[] servers2 = (_OPCWrapper).GetAvailableServers(Opc.Specification.COM_DA_20, Host, null);
                foreach (Opc.Server server in servers2) OPCServers.Add(new OPCServerNode(server.Name, server.Url.ToString(), OPCSpecification.Classic_DA_20));
                Opc.Server[] servers3 = (_OPCWrapper).GetAvailableServers(Opc.Specification.COM_DA_30, Host, null);
                foreach (Opc.Server server in servers3)
                {
                    if (!OPCServers.Exists(O => O.OPCURL == server.Url.ToString()))
                    {
                        OPCServers.Add(new OPCServerNode(server.Name, server.Url.ToString(), OPCSpecification.Classic_DA_30));
                    }
                }
                return new BrowseMachine_Result()
                {
                    success = true,
                    result = OPCServers
                };
            }
            catch (Exception ex)
            {
                return new BrowseMachine_Result()
                {
                    success = false,
                    result = null,
                    error = ex.ToString()
                };
            }         
        }

      


        public BrowseGeneric_Result BrowseBranch_Classic(string OPCURL, string ItemId)
        {
            Exception Exception = null;
            List<BaseNode> Output = new List<BaseNode>();
            Opc.Da.Server ConnectedOPDAServer;
            try
            {
                ConnectedOPDAServer = ConnectToOPCServer(OPCURL);
                if (ConnectedOPDAServer == null)
                {
                    Exception = new Exception("Fail to Connect to OPC Server '" + OPCURL + "'");
                    return new BrowseGeneric_Result() { success = false, result = null, error = Exception.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new BrowseGeneric_Result() { success = false, result = null, error = ex.ToString() };
            }

            
            Opc.Da.BrowseElement[] Elements = null;
            try
            {
                if (ItemId == null)
                {
                    Elements = ConnectedOPDAServer.Browse(null, _BrowseFilters, out Opc.Da.BrowsePosition position);
                }
                else
                {
                    Elements = ConnectedOPDAServer.Browse(new ItemIdentifier(null, ItemId), _BrowseFilters, out Opc.Da.BrowsePosition position); // begin a browse.                
                    if (Elements == null) Elements = ConnectedOPDAServer.Browse(new ItemIdentifier(ItemId, null), _BrowseFilters, out position); // begin a browse.                
                }
                if (Elements != null)
                {
                    foreach (Opc.Da.BrowseElement Element in Elements)
                    {
                        if (Element.IsItem) //If it is an OPC Tag Item
                        {
                            TagNode AddedNode = null;
                            if (!string.IsNullOrWhiteSpace(Element.ItemName)) Output.Add(AddedNode = new TagNode(Element.ItemName, OPCURL));
                            else if (!string.IsNullOrWhiteSpace(Element.ItemPath)) Output.Add(AddedNode = new TagNode(Element.ItemPath, OPCURL));
                            else if (!string.IsNullOrWhiteSpace(Element.Name)) Output.Add(AddedNode = new TagNode(Element.Name, OPCURL));
                            if (AddedNode != null && Element.Properties != null)
                            {
                                foreach (Opc.Da.ItemProperty property in Element.Properties)
                                {
                                    if (!property.ResultID.Succeeded() || property.Value == null) continue;
                                    string PropertyName = property.ID.ToPropertyName();
                                    if (PropertyName == null) continue;
                                    //Create a Property Node (Can be Value, Data Type,etc)
                                    PropertyNode PN = new PropertyNode(PropertyName, property.Description, Opc.Convert.ToString(property.Value), property.ResultID.Succeeded());

                                    //For certain properties, there is special case
                                    switch (property.ID.Code)
                                    {
                                        case 1: //Data Type
                                            System.Type PropertyType = property.Value.GetType();
                                            if (PropertyType.FullName == "System.RuntimeType")
                                            {
                                                System.Type TTT = (System.Type)(property.Value);
                                                var DT = TTT.ToString();
                                                var DTComp = DT.Split('.');
                                                AddedNode.DataTypeString = DTComp.Last();
                                            }
                                            break;

                                        case 2: //Value
                                            if (property.Value is Array)
                                            {
                                                foreach (object element in (Array)property.Value)
                                                {
                                                    string ValueElement = Opc.Convert.ToString(element);
                                                    AddedNode.Value += ValueElement + ",";
                                                    PN.ArrayValues.Add(new ArrayValue(ValueElement));
                                                }
                                                if (AddedNode.Value != null) AddedNode.Value = AddedNode.Value.RemoveLastCharacter();
                                            }
                                            else AddedNode.Value = PN.Value;
                                            break;

                                        case 3: //Quality
                                            Opc.Da.Quality Q = (Opc.Da.Quality)(property.Value);
                                            AddedNode.QualityOK = (Q.QualityBits == Opc.Da.qualityBits.good || Q.QualityBits == Opc.Da.qualityBits.goodLocalOverride);
                                            break;

                                        case 5: //Timestamp
                                            if (property.Value is Opc.Da.accessRights AR) AddedNode.AccessLevel = AR.ToAccessLevel();
                                            break;

                                        case 100: //Engineering Unit
                                            AddedNode.EngineeringUnit = PN.Value.ToString();
                                            break;

                                        case 101: //Description
                                            AddedNode.Description = PN.Value.ToString();
                                            break;

                                    }
                                    AddedNode.Properties.Add(PN);

                                    
                                }
                            }
                        }
                        else //If it is a Branch
                        {
                            BranchNode AddedNode = null;
                            if (!string.IsNullOrWhiteSpace(Element.ItemName)) Output.Add(AddedNode = new BranchNode(Element.ItemName, OPCURL));
                            else if (!string.IsNullOrWhiteSpace(Element.ItemPath)) Output.Add(AddedNode = new BranchNode(Element.ItemPath, OPCURL));
                            else if (!string.IsNullOrWhiteSpace(Element.Name)) Output.Add(AddedNode = new BranchNode(Element.Name, OPCURL));
                        }
                    }
                    return new BrowseGeneric_Result() { success = true, result = Output };
                }
            }
            catch (Exception ex)
            {
                Exception = ex;
            }
            return new BrowseGeneric_Result() { success = false, result = null, error = Exception?.ToString() };
        }


        public BrowseTag_Classic_Result BrowseTag_Classic(string OPCURL, string ItemId)
        {
            BrowseTag_Classic_Result BrowseTag_Classic_Result = new BrowseTag_Classic_Result();
            Opc.Da.Server ConnectedOPDAServer;            
            try
            {
                ConnectedOPDAServer = ConnectToOPCServer(OPCURL);
                if (ConnectedOPDAServer == null) return new BrowseTag_Classic_Result() { success = false, result = null,  error = "Fail to Connect to OPC Server '" + OPCURL + "'" };

                ItemPropertyCollection[] PropertiesCollections = ConnectedOPDAServer.GetProperties(
                    new ItemIdentifier[] { new ItemIdentifier(ItemId) }, 
                    new Opc.Da.PropertyID[] { new PropertyID(1), new PropertyID(2), new PropertyID(3), new PropertyID(4),
                        new PropertyID(5), new PropertyID(100), new PropertyID(101)}, 
                    true); // begin a browse.                
                
                if (PropertiesCollections != null)
                {
                    foreach (ItemPropertyCollection Properties in PropertiesCollections)
                    {
                        foreach (Opc.Da.ItemProperty property in Properties)
                        {
                            if (!property.ResultID.Succeeded() || property.Value == null) continue;
                            string PropertyName = property.ID.ToPropertyName();
                            if (PropertyName == null) continue;

                            //Create a Property Node (Can be Value, Data Type,etc)
                            PropertyNode PN = new PropertyNode(PropertyName, property.Description, Opc.Convert.ToString(property.Value), property.ResultID.Succeeded());

                            //For certain properties, there are special case handlers
                            switch (property.ID.Code)
                            {
                                case 1: //Data Type
                                    System.Type PropertyType = property.Value.GetType();
                                    //if (PropertyType.FullName == "System.RuntimeType")
                                    //{
                                    //    System.Type TTT = (System.Type)(property.Value);
                                    //    var DT = TTT.ToString();
                                    //    var DTComp = DT.Split('.');
                                    //    AddedNode.DataTypeString = DTComp.Last();
                                    //}
                                    //BrowseTag_Classic_Result.DataType = PropertyType;
                                    BrowseTag_Classic_Result.result.DataTypeString = PropertyType.Name;
                                    break;

                                case 2: //Value
                                    if (property.Value is Array)
                                    {
                                        BrowseTag_Classic_Result.result.Value = "";
                                        foreach (object element in (Array)property.Value)
                                        {
                                            string ValueElement = Opc.Convert.ToString(element);
                                            BrowseTag_Classic_Result.result.Value += ValueElement + ",";
                                            PN.ArrayValues.Add(new ArrayValue(ValueElement));
                                        }
                                        if (BrowseTag_Classic_Result.result.Value != null) BrowseTag_Classic_Result.result.Value = BrowseTag_Classic_Result.result.Value.RemoveLastCharacter();
                                    }
                                    else BrowseTag_Classic_Result.result.Value = PN.Value;
                                    break;

                                case 3: //Quality
                                    Quality Q = (Quality)(property.Value);
                                    BrowseTag_Classic_Result.result.Quality = (Q.QualityBits == qualityBits.good || Q.QualityBits == qualityBits.goodLocalOverride);
                                    break;

                                case 5: //Timestamp
                                    if (property.Value is accessRights AR) BrowseTag_Classic_Result.result.AccessLevel = AR.ToAccessLevel();
                                    break;

                                case 100: //Engineering Unit
                                    BrowseTag_Classic_Result.result.EngineeringUnit = PN.Value.ToString();
                                    break;

                                case 101: //Description
                                    BrowseTag_Classic_Result.result.Description = PN.Value.ToString();
                                    break;

                            }
                            BrowseTag_Classic_Result.result.properties.Add(PN);


                        }
                    }
                    BrowseTag_Classic_Result.success = true;
                    return BrowseTag_Classic_Result;
                }

            }
            catch (Exception ex)
            {
                return new BrowseTag_Classic_Result() { success = false, error = ex.ToString() };
            }
            return new BrowseTag_Classic_Result() { success = false, error = "" };
        }
        #endregion



        public Dictionary<string, RegisteredTag> RegisteredTags = new Dictionary<string, RegisteredTag>();

        public Read_Result Read_Classic(string OPCURL, string UpdateInterval, string ItemIds)
        {            
            try
            {
                Read_Result Read_Result = new Read_Result(); RegisteredTag NewRegisteredTag;
                if (ItemIds.IsNullOrWhiteSpace()) return Read_Result;
                Opc.Da.Server ConnectedOPDAServer = ConnectToOPCServer(OPCURL);
                if (ConnectedOPDAServer == null) return new Read_Result() { success = false, result = null, error = "Fail to Connect to OPC Server '" + OPCURL + "'" };
                string[] ItemIdsSplit = ItemIds.Split(',');
                int LastIndex = ItemIdsSplit.Count() - 1;
                if (ItemIdsSplit[LastIndex].IsNullOrEmpty()) ItemIdsSplit = ItemIdsSplit.RemoveAt(LastIndex);
                if (ItemIdsSplit.Count() > 0)
                {
                    int UpdateIntervalInMS = UpdateInterval.ToInt(1000);
                    if (UpdateIntervalInMS < 250) UpdateIntervalInMS = 250;
                    foreach (string ItemId in ItemIdsSplit)
                    {
                        //if (ItemId.IsNullOrWhiteSpace()) continue;
                        //Check if the Item has already been subscribed 
                        if (this.RegisteredTags.TryGetValue(ItemId, out RegisteredTag FoundRegisteredTag))
                        {
                            //Straight forward case, simply obtain data
                            if (UpdateIntervalInMS >= FoundRegisteredTag.UpdateIntervalInMS)
                            {
                                FoundRegisteredTag.LastCalled = DateTime.Now;
                                Read_Result.result.Add(new DataValue(FoundRegisteredTag.Id, true, FoundRegisteredTag.TSUTC, FoundRegisteredTag.SourceTSUTC, FoundRegisteredTag.Value, FoundRegisteredTag.QualityOK));
                                continue;
                            }

                            //If the Update Interval is more Frequent we need to remove the tag and subscribe it to a group which has higher update rate
                            else
                            {
                                FoundRegisteredTag.UpdateIntervalInMS = UpdateIntervalInMS;
                                Subscription SubscriptionWhereTagIs = ConnectedOPDAServer.Subscriptions.FindSubcriptionThatHasItem(ItemId, out Item ItemFound);
                                if (SubscriptionWhereTagIs != null) SubscriptionWhereTagIs.RemoveItems(new ItemIdentifier[] { ItemFound }); //Remove From Subscription
                            }
                        }


                        //Tag WAS not found in Registered Tags Dictionary (or was removed due to update interval changed)
                        //Check if there are any subscriptions which have same Update Interval and has room for items
                        NewRegisteredTag = new RegisteredTag(ItemId, UpdateIntervalInMS);
                        ItemValueResult readResult;
                        Subscription SuitableSubscription = ConnectedOPDAServer.Subscriptions.FindSubcription(S => S.State.UpdateRate == UpdateIntervalInMS && S.Items.Count() < this.OPCGroupSizeLimit);
                        if (SuitableSubscription != null)
                        {
                            //if a suitable subscription was found, Add item to this subscription
                            ItemResult[] IR = SuitableSubscription.AddItems(new Item[] { new Item() { ItemName = ItemId } });
                            if (IR.Count() <= 0) { Read_Result.result.Add(new DataValue(ItemId, false, DateTime.UtcNow, DateTime.MinValue, "Failed to Add Item to OPC Group", false)); continue; }
                            if (IR[0].ResultID == ResultID.S_OK) readResult = SuitableSubscription.Read(new Item[] { IR[0] })[0];
                            else { Read_Result.result.Add(new DataValue(ItemId, false, DateTime.UtcNow, DateTime.MinValue, IR[0].ResultID.ToString(), false)); continue; }
                        }
                        else
                        {
                            //If no Subscriptions found, create new Subscription
                            ISubscription NewSubscription = ConnectedOPDAServer.CreateSubscription(new SubscriptionState() { UpdateRate = UpdateIntervalInMS });
                            NewSubscription.DataChanged -= new DataChangedEventHandler(this.OnOPCSubscriptionDataChanged);
                            NewSubscription.DataChanged += new DataChangedEventHandler(this.OnOPCSubscriptionDataChanged);
                            ItemResult[] IR = NewSubscription.AddItems(new Item[] { new Item() { ItemName = ItemId } });
                            if (IR.Count() <= 0) { Read_Result.result.Add(new DataValue(ItemId, false, DateTime.UtcNow, DateTime.MinValue, "Failed to Add Item to new OPC Group", false)); continue; }
                            if (IR[0].ResultID == ResultID.S_OK) readResult = NewSubscription.Read(new Item[] { IR[0] })[0];
                            else { Read_Result.result.Add(new DataValue(ItemId, false, DateTime.UtcNow, DateTime.MinValue, IR[0].ResultID.ToString(), false)); continue; }
                        }

                        NewRegisteredTag.TSUTC = DateTime.UtcNow;
                        if (readResult.TimestampSpecified) NewRegisteredTag.SourceTSUTC = readResult.Timestamp; else NewRegisteredTag.SourceTSUTC = DateTime.UtcNow;
                        if (readResult.Quality == Quality.Good)
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
                        RegisteredTags.Add(ItemId, NewRegisteredTag);
                    }
                }
                return Read_Result;
            }
            catch (Exception ex)
            {
                return new Read_Result() { success = false, error = ex.ToString() };
            }
        }

        public void OnOPCSubscriptionDataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] results)
        {
            DateTime CurSampTime = DateTime.UtcNow;

            //Update the Registered Tags Dictionary
            foreach (ItemValueResult readResult in results)
            {
                if (this.RegisteredTags.TryGetValue(readResult.ItemName, out RegisteredTag Tag))
                {
                    Tag.TSUTC = DateTime.UtcNow;
                    if (readResult.TimestampSpecified) Tag.SourceTSUTC = readResult.Timestamp; else Tag.SourceTSUTC = DateTime.MinValue;
                    if (readResult.Quality == Quality.Good)
                    {
                        Tag.QualityOK = true;
                        if (readResult.Value is string || readResult.Value.IsNumericType()) Tag.Value = (IComparable)readResult.Value;
                    }
                    else
                    {
                        Tag.QualityOK = false;
                        Tag.Value = null;
                    }
                }
            }
        }

        /// <summary>
        /// Connects to the OPC Server
        /// </summary>
        /// <param name="OPCURL">
        /// The OPC URL should be in the following form (Examples):
        ///     - opcae://localhost/Matrikon.OPC.Simulation
        ///     - opcda://192.168.1.2/Matrikon.OPC.Simulation
        /// </param>
        /// <param name="credentials"></param>
        public Opc.Da.Server ConnectToOPCServer(string OPCURL, NetworkCredential credentials = null)
        {
            Opc.Da.Server OPCDAServerToConnectTo = null;
            try
            {
                URL URL = new Opc.URL(OPCURL);
                OPCDAServerToConnectTo = _OPCDAServers.FirstOrDefault(S => S.Url.ToString() == OPCURL);
                if (OPCDAServerToConnectTo == null) _OPCDAServers.Add(OPCDAServerToConnectTo = new Opc.Da.Server(new OpcCom.Factory(), null));
                if (!OPCDAServerToConnectTo.IsConnected) OPCDAServerToConnectTo.Connect(URL, new ConnectData(credentials));
                return OPCDAServerToConnectTo;
            }
            catch (Exception ex)
            {
                OPCDAServerToConnectTo?.Disconnect();
                OPCDAServerToConnectTo?.Dispose();
                throw ex;                
            }
        }
    }
    
       
}
