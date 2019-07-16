//============================================================================
// TITLE: Opc.IServer.cs
//
// CONTENTS:
// 
// An interface that defines functionality that is common to all OPC servers.
//
// (c) Copyright 2003-2004 The OPC Foundation
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
// 2003/11/26 RSA   Updated documentation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc
{
	/// <summary>
	/// Defines functionality that is common to all OPC servers.
	/// </summary>
	public interface IServer : IDisposable
	{
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive server shutdown notifications.
		/// </summary>
		event ServerShutdownEventHandler ServerShutdown;

		//======================================================================
		// Localization

		/// <summary>
		/// The locale used in any error messages or results returned to the client.
		/// </summary>
		/// <returns>The locale name in the format "[languagecode]-[country/regioncode]".</returns>
		string GetLocale();

		/// <summary>
		/// Sets the locale used in any error messages or results returned to the client.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <returns>A locale that the server supports and is the best match for the requested locale.</returns>
		string SetLocale(string locale);

		/// <summary>
		/// Returns the locales supported by the server
		/// </summary>
		/// <remarks>The first element in the array must be the default locale for the server.</remarks>
		/// <returns>An array of locales with the format "[languagecode]-[country/regioncode]".</returns>
		string[] GetSupportedLocales();

		/// <summary>
		/// Returns the localized text for the specified result code.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <param name="resultID">The result code identifier.</param>
		/// <returns>A message localized for the best match for the requested locale.</returns>
		string GetErrorText(string locale, ResultID resultID);
	}

	/// <summary>
	/// Maintains the state of an asynchronous request.
	/// </summary>
	public interface IRequest 
	{	
		/// <summary>
		/// An unique identifier, assigned by the client, for the request.
		/// </summary>
		object Handle { get; }
	}

	/// <summary>
	/// Maintains the state of a browse operation
	/// </summary>
	public interface IBrowsePosition : IDisposable, ICloneable
	{	
	}

	//=============================================================================
	// Delegates

	/// <summary>
	/// A delegate to receive shutdown notifiations from the server.
	/// </summary>
	public delegate void ServerShutdownEventHandler(string reason);
}
