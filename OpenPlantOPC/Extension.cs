using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
using System.Globalization;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Net;
namespace OpenPlantOPC
{
    public static partial class Extensions
    {

      

        public static string ToPropertyName(this Opc.Da.PropertyID propertyID)
        {
            switch (propertyID.Code)
            {
                case 1: return "Data Type";
                case 2: return "Value";
                case 3: return "Quality";
                case 4: return "Timestamp";
                case 5: return "Access Right";
                case 6: return "Scan Rate";
                case 7: return "EU Type";
                case 8: return "EU Info";
                case 100: return "EU Unit";
                case 101: return "Description";
                case 102: return "High EU";
                case 103: return "Low EU";
                case 104: return "High IR";
                case 105: return "Low IR";
                case 106: return "Close Label";
                case 107: return "Open Label";
                case 108: return "Time Zone";
                case 300: return "Condition Status";
                case 301: return "Alarm Quick Help";
                case 302: return "Alarm Area List";
                case 303: return "Primary Alarm Area";
                case 304: return "Condition Logic";
                case 305: return "Limit Exceeded";
                case 306: return "Deadband";
                case 307: return "HIHI Limit";
                case 308: return "HI Limit";
                case 309: return "LO Limit";
                case 310: return "LOLO Limit";
                case 311: return "Change Rate Limit";
                case 312: return "Deviation Limit";
                case 313: return "Sound File";
            }
            return null;
        }


        public static AccessLevel ToAccessLevel(this Opc.Da.accessRights accessRights)
        {
            switch (accessRights)
            {
                case Opc.Da.accessRights.readable: return AccessLevel.ReadOnly;
                case Opc.Da.accessRights.writable: return AccessLevel.WriteOnly;
                case Opc.Da.accessRights.readWritable: return AccessLevel.ReadAndWrite;
            }
            return AccessLevel.InvalidAccess;
        }
        
        public static string ToAccessLevelShortName(this AccessLevel AccessLevel)
        {
            switch (AccessLevel)
            {
                case AccessLevel.NoAccess: return "No Access";
                case AccessLevel.ReadOnly: return "Read Only";
                case AccessLevel.WriteOnly: return "Write Only";
                case AccessLevel.ReadAndWrite: return "Read, Write";
                case AccessLevel.HistoricalReadOnly: return "Historical Read Only";
                case AccessLevel.ReadAndHistoricalRead: return "Read, Historical Read";
                case AccessLevel.WriteAndHistoricalRead: return "Write, Historical Read";
                case AccessLevel.ReadAndWriteAndHistoricalRead: return "Read, Write, Historical Read";
                case AccessLevel.HistoricalWriteOnly: return "Historical Write Only";
                case AccessLevel.ReadAndHistoricalWrite: return "Read, Historical Write";
                case AccessLevel.WriteAndHistoricalWrite: return "Write, Historical Write";
                case AccessLevel.ReadAndWriteAndHistoricalWrite: return "Read, Write, Historical Write";
                case AccessLevel.HistoricalReadAndHistoricalWrite: return "Historical Read, Historical Write";
                case AccessLevel.ReadAndHistoricalReadAndHistoricalWrite: return "Read, Historical Read, Historical Write";
                case AccessLevel.WriteAndHistoricalReadAndHistoricalWrite: return "Write, Historical Read, Historical Write";
                case AccessLevel.ReadAndWriteAndHistoricalReadAndHistoricalWrite: return "Read, Write, Historical Read, Historical Write";
            }
            return "Invalid Access Level";
        }

        public static string RemoveLastCharacter(this object Object)
        {
            if (Object is String Text)
            {
                if (Text.Length == 0) return Text;
                return Text.Substring(0, Text.Length - 1);
            }
            return Object.ToString();
        }
        
        


        public static Opc.Da.Subscription FindSubcription(this Opc.Da.SubscriptionCollection source, Func<Opc.Da.Subscription, bool> predicate)
        {
            foreach (Opc.Da.Subscription S in source)
            {
                if (predicate(S))
                {
                    return S;
                }
            }
            return null;
        }

        public static Opc.Ua.Client.Subscription FindSubcription(this IEnumerable<Opc.Ua.Client.Subscription> source, Func<Opc.Ua.Client.Subscription, bool> predicate)
        {
            foreach (Opc.Ua.Client.Subscription S in source)
            {
                if (predicate(S))
                {
                    return S;
                }
            }
            return null;
        }


        public static Opc.Ua.Client.Subscription FindSubcriptionThatHasItem(this IEnumerable<Opc.Ua.Client.Subscription> source, string NodeId, out Opc.Ua.Client.MonitoredItem ItemToFind)
        {
            ItemToFind = null;
            foreach (Opc.Ua.Client.Subscription S in source)
            {
                ItemToFind = S.MonitoredItems.FirstOrDefault(I => I.StartNodeId.ToString() == NodeId);
                if (ItemToFind != null) return S;
            }
            return null;
        }


        public static Opc.Da.Subscription FindSubcriptionThatHasItem(this Opc.Da.SubscriptionCollection source, string ItemId, out Opc.Da.Item ItemToFind)
        {
            ItemToFind = null;
            foreach (Opc.Da.Subscription S in source)
            {
                ItemToFind = S.Items.FirstOrDefault(I => I.ItemName == ItemId);
                if (ItemToFind!= null) return S;
            }
            return null;
        }
        public static int ToInt(this object value, int ValueIfFail = 0)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return ValueIfFail;
            }
        }

        
    }
}