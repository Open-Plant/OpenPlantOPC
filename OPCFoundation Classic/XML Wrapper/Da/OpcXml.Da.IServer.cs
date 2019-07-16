//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// The unified interface for an OPC Data Access server.
//
// (c) Copyright 2003 The OPC Foundation
// ALL RIGHTS RESERVED.
//
// DISCLAIMER:
//  This code is provided by the OPC Foundation solely to assist in 
//  understanding and use of the appropriate OPC Specification(s) and may be 
//  used as set forth in the License Grant section of the OPC Specification.
//  This code is provided as-is and without warranty or support of any sort
//  and is subject to the Warranty and Liability Disclaimers which appear
//  in the printed OPC Specification.
//
// MODIFICATION LOG:
//
// Date       By    Notes
// ---------- ---   -----
// 2003/03/26 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Globalization;
using Opc;
using Opc.Da;

namespace OpcXml.Da
{
	/// <summary>
	/// Defines functionality that is common to all XML-DA OPC servers.
	/// </summary>
	public interface IServer : IDisposable
	{
		/// <summary>
		/// Returns the set of supported locales.
		/// </summary>
		string[] SupportedLocales {get;}

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		ReplyBase GetStatus(
			string           locale,
			string           clientRequestHandle,
			out ServerStatus status);

		/// <summary>
		/// Reads a set of items.
		/// </summary>
		ReplyBase Read(
			RequestOptions          options, 
			ItemList                requestList,
			out ItemValueResultList replyList,
			out Error[]             errors);

		/// <summary>
		/// Writes a set of items and, if requested, returns the current values.
		/// </summary>
		ReplyBase Write(
			RequestOptions          options, 
			ItemValueList           requestList, 
			bool                    returnValues,
			out ItemValueResultList replyList,
			out Error[]             errors);

		/// <summary>
		/// Establishes a subscription for the set of items.
		/// </summary>
		ReplyBase Subscribe(
			RequestOptions          options, 
			ItemList                requestList, 
			TimeSpan                pingTime,
			bool                    returnValues,
			out string              subscriptionID,
			out ItemValueResultList replyList,
			out Error[]             errors);

		/// <summary>
		/// Polls the server for the any item changes for one or more subscriptions.
		/// </summary>
		ReplyBase PolledRefresh(
			RequestOptions            options, 
			string[]                  subscriptionIDs,
			TimeSpan                  holdTime,
			TimeSpan                  waitTime,
			bool                      returnAllValues,
			out string[]              invalidSubscriptionIDs,
			out ItemValueResultList[] values,
			out Error[]               errors,
			out bool                  dataBufferOverflow);

		/// <summary>
		/// Terminates one subscription.
		/// </summary>
		void Unsubscribe(string subscriptionID);

		/// <summary>
		/// Returns a set of elements at the specified position and that meet the filter criteria.
		/// </summary>
		ReplyBase Browse(
			string              locale,
			string              clientRequestHandle,
			bool                returnErrorText,
			ItemIdentifier      itemID,
			BrowseFilters       filters,
			ref string          continuationPoint,
			out bool            moreElements,
			out BrowseElement[] elements,
		    out Error[]         errors);

		/// <summary>
		/// Returns the specified properties for a set of items.
		/// </summary>
		ReplyBase GetProperties(
			string                       locale,
			string                       clientRequestHandle,
			bool                         returnErrorText,
			ItemIdentifier[]             itemIDs,
			PropertyID[]                 propertyIDs,
			string                       itemPath,
			bool                         returnValues,
			out ItemPropertyCollection[] properties,
			out Error[]                  errors);
	}

	/// <summary>
	/// The standard return parameter for XML-DA server methods.
	/// </summary>
	[Serializable]
	public class RequestOptions
	{
		/// <summary>
		/// A request handle assigned by client.
		/// </summary>
		public string RequestHandle = null;

		/// <summary>
		/// The maximum time a server should wait before returning whatever results it has.
		/// </summary>
		public DateTime RequestDeadline = DateTime.MinValue;

		/// <summary>
		/// The locale to used for strings in the results.
		/// </summary>
		public string Locale = null;
		
		/// <summary>
		/// The filters to apply to returned results.
		/// </summary>
		public int Filters = (int)ResultFilter.Minimal;
	}

	/// <summary>
	/// The standard return parameter for XML-DA server methods.
	/// </summary>
	[Serializable]
	public class ReplyBase 
	{
		/// <summary>
		/// The UTC time a request arrives at the server.
		/// </summary>
        public DateTime RcvTime;

		/// <summary>
		/// The UTC time a response is returned from the server.
		/// </summary>
		public DateTime ReplyTime;

		/// <summary>
		/// The request handle assigned by the client.
		/// </summary>
		public string ClientRequestHandle = null;

		/// <summary>
		/// The actual locale id used by the server.
		/// </summary>
		public string RevisedLocaleID = null;

		/// <summary>
		/// The current state of the server.
		/// </summary>
		public serverState ServerState;
	}

	/// <summary>
	/// Contains the localized text for a result code.
	/// </summary>
	[Serializable]
	public class Error 
	{
		/// <summary>
		/// The unique id for the result code.
		/// </summary>
		public XmlQualifiedName ID;

		/// <summary>
		/// The localized verbose message,
		/// </summary>
		public string Text;

		/// <summary>
		/// All errors that are defined in the XML-DA specification.
		/// </summary>summary>
		public static readonly XmlQualifiedName E_FAIL                     = new XmlQualifiedName("E_FAIL",                     Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_OUTOFMEMORY              = new XmlQualifiedName("E_OUTOFMEMORY",              Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_SERVERSTATE              = new XmlQualifiedName("E_SERVERSTATE",              Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_TIMEDOUT                 = new XmlQualifiedName("E_TIMEDOUT",                 Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_BUSY                     = new XmlQualifiedName("E_BUSY",                     Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDCONTINUATIONPOINT = new XmlQualifiedName("E_INVALIDCONTINUATIONPOINT", Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDFILTER            = new XmlQualifiedName("E_INVALIDFILTER",            Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_NOSUBSCRIPTION           = new XmlQualifiedName("E_NOSUBSCRIPTION",           Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDHOLDTIME          = new XmlQualifiedName("E_INVALIDHOLDTIME",          Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_UNKNOWNITEMNAME          = new XmlQualifiedName("E_UNKNOWNITEMNAME",        Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDITEMNAME          = new XmlQualifiedName("E_INVALIDITEMNAME",        Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_UNKNOWNITEMPATH          = new XmlQualifiedName("E_UNKNOWNITEMPATH",        Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDITEMPATH          = new XmlQualifiedName("E_INVALIDITEMPATH",        Namespace.OPC_DATA_ACCESS_XML10);		
		/// <remarks/>
		public static readonly XmlQualifiedName E_NOTSUPPORTED             = new XmlQualifiedName("E_NOTSUPPORTED",             Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_BADTYPE                  = new XmlQualifiedName("E_BADTYPE",                  Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_RANGE                    = new XmlQualifiedName("E_RANGE",                    Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_READONLY                 = new XmlQualifiedName("E_READONLY",                 Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_WRITEONLY                = new XmlQualifiedName("E_WRITEONLY",                Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName E_INVALIDPID               = new XmlQualifiedName("E_INVALIDPID",               Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName S_FALSE                    = new XmlQualifiedName("S_FALSE",                    Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName S_CLAMP                    = new XmlQualifiedName("S_CLAMP",                    Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName S_UNSUPPORTEDRATE          = new XmlQualifiedName("S_UNSUPPORTEDRATE",          Namespace.OPC_DATA_ACCESS_XML10);	
		/// <remarks/>
		public static readonly XmlQualifiedName S_DATAQUEUEOVERFLOW        = new XmlQualifiedName("S_DATAQUEUEOVERFLOW",        Namespace.OPC_DATA_ACCESS_XML10);	
		/// <remarks/>
		public static readonly XmlQualifiedName E_TYPE_CHANGED             = new XmlQualifiedName("E_TYPE_CHANGED",             Namespace.OPC_COMPLEX_DATA);
		/// <remarks/>
		public static readonly XmlQualifiedName E_FILTER_DUPLICATE         = new XmlQualifiedName("E_FILTER_DUPLICATE",         Namespace.OPC_COMPLEX_DATA);
		/// <remarks/>
		public static readonly XmlQualifiedName E_FILTER_INVALID           = new XmlQualifiedName("E_FILTER_INVALID",           Namespace.OPC_COMPLEX_DATA);
		/// <remarks/>
		public static readonly XmlQualifiedName E_FILTER_ERROR             = new XmlQualifiedName("E_FILTER_ERROR",             Namespace.OPC_COMPLEX_DATA);
		/// <remarks/>
		public static readonly XmlQualifiedName S_FILTER_NO_DATA           = new XmlQualifiedName("S_FILTER_NO_DATA",           Namespace.OPC_COMPLEX_DATA);	
	}
}
