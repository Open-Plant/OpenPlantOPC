/* Copyright (c) 1996-2017, OPC Foundation. All rights reserved.

   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else

   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/

   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2

   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Opc.Ua
{
	/// <summary>
    /// An abstract interface to certficate stores.
    /// </summary>
	public interface ICertificateStore : IDisposable
    {
        /// <summary>
        /// Opens the store at the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <remarks>
        /// The syntax depends on the store implementation.
        /// </remarks>
		void Open(string location);

        /// <summary>
        /// Closes the store.
        /// </summary>
		void Close();

		/// <summary>
		/// Enumerates the certificates in the store.
		/// </summary>
        X509Certificate2Collection Enumerate();

        /// <summary>
        /// Adds a certificate to the store.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        void Add(X509Certificate2 certificate);

        /// <summary>
        /// Deletes a certificate from the store.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns>True if the certificate exists.</returns>
        bool Delete(string thumbprint);

        /// <summary>
        /// Finds the certificate with the specified thumprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns>The matching certificate</returns>
        X509Certificate2 FindByThumbprint(string thumbprint);
        
        #if !SILVERLIGHT
		/// <summary>
		/// Whether the store supports access control.
		/// </summary>
		bool SupportsAccessControl { get; }

        /// <summary>
        /// Returns the access rules that are currently applied to the store.
        /// </summary>
        /// <returns>The list of access rules.</returns>
		IList<ApplicationAccessRule> GetAccessRules();

        /// <summary>
        /// Sets the access rules that are currently applied to the store.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="replaceExisting">if set to <c>true</c> the existing access rules are replaced.</param>
        void SetAccessRules(IList<ApplicationAccessRule> rules, bool replaceExisting);   
        
		/// <summary>
		/// Whether the store supports access control on certificates.
		/// </summary>
		bool SupportsCertificateAccessControl { get; }  
        #endif

		/// <summary>
		/// Whether the store supports private keys.
		/// </summary>
        bool SupportsPrivateKeys { get; }

        /// <summary>
        /// Returns the file containing the private key for the specified certificate.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns>The full file path. Null if the certificate does not exist or the private key does not exist.</returns>
        string GetPrivateKeyFilePath(string thumbprint);
        
        #if !SILVERLIGHT
        /// <summary>
        /// Returns the access rules that are currently applied to the certficate's private key.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns>The access rules.</returns>
        IList<ApplicationAccessRule> GetAccessRules(string thumbprint);

        /// <summary>
        /// Sets the access rules that are currently applied to the certficate's private key.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="rules">The rules.</param>
        /// <param name="replaceExisting">if set to <c>true</c> the existing access rules are replaced.</param>
        void SetAccessRules(string thumbprint, IList<ApplicationAccessRule> rules, bool replaceExisting);

        /// <summary>
        /// Whether the store supports CRLs.
        /// </summary>
        bool SupportsCRLs { get; }

        /// <summary>
        /// Checks if issuer has revoked the certificate.
        /// </summary>
        StatusCode IsRevoked(X509Certificate2 issuer, X509Certificate2 certificate);
        
        /// <summary>
        /// Returns the CRLs in the store.
        /// </summary>
        List<X509CRL> EnumerateCRLs();

        /// <summary>
        /// Returns the CRLs for the issuer.
        /// </summary>
        List<X509CRL> EnumerateCRLs(X509Certificate2 issuer);

        /// <summary>
        /// Adds a CRL to the store.
        /// </summary>
        void AddCRL(X509CRL crl);

        /// <summary>
        /// Removes a CRL from the store.
        /// </summary>
        bool DeleteCRL(X509CRL crl);
        #endif
    };
}
