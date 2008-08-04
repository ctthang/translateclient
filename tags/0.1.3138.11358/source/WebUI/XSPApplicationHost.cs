//
// Mono.WebServer.XSPApplicationHost
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// (C) Copyright 2004-2007 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
#if MONO
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Protocol.Tls;
using SecurityProtocolType = Mono.Security.Protocol.Tls.SecurityProtocolType;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;
#endif

namespace Mono.WebServer
{	
	//
	// XSPApplicationHost: The application host for XSP.
	//
	public class XSPApplicationHost : BaseApplicationHost
	{
		public void ProcessRequest (int reqId, long localEPAddr, int localEPPort, long remoteEPAdds,
					int remoteEPPort, string verb, string path,
					string queryString, string protocol, byte [] inputBuffer, string redirect,
					IntPtr socket, SslInformations ssl)
		{
			XSPRequestBroker broker = (XSPRequestBroker) RequestBroker;
			IPEndPoint localEP = new IPEndPoint (localEPAddr, localEPPort);
			IPEndPoint remoteEP = new IPEndPoint (remoteEPAdds, remoteEPPort);
			bool secure = (ssl != null);
			XSPWorkerRequest mwr = new XSPWorkerRequest (reqId, broker, this, localEP, remoteEP, verb, path,
								queryString, protocol, inputBuffer, socket, secure);

			if (secure) {
				// note: we're only setting what we use (and not the whole lot)
				mwr.AddServerVariable ("CERT_KEYSIZE", ssl.KeySize.ToString (CultureInfo.InvariantCulture));
				mwr.AddServerVariable ("CERT_SECRETKEYSIZE", ssl.SecretKeySize.ToString (CultureInfo.InvariantCulture));
 
				if (ssl.RawClientCertificate != null) {
					// the worker need to be able to return it (if asked politely)
					mwr.SetClientCertificate (ssl.RawClientCertificate);

					// XSPWorkerRequest will answer, as required, for CERT_COOKIE, CERT_ISSUER, 
					// CERT_SERIALNUMBER and CERT_SUBJECT (as anyway it requires the client 
					// certificate - if it was provided)

					if (ssl.ClientCertificateValid) {
						// client cert present (bit0 = 1) and valid (bit1 = 0)
						mwr.AddServerVariable ("CERT_FLAGS", "1");
					} else {
						// client cert present (bit0 = 1) but invalid (bit1 = 1)
						mwr.AddServerVariable ("CERT_FLAGS", "3");
					}
				} else {
					// no client certificate (bit0 = 0) ? does bit1 matter ?
					mwr.AddServerVariable ("CERT_FLAGS", "0");
				}

#if MONO
				if (ssl.RawServerCertificate != null) {
					X509Certificate server = ssl.GetServerCertificate ();
					mwr.AddServerVariable ("CERT_SERVER_ISSUER", server.GetIssuerName ());
					mwr.AddServerVariable ("CERT_SERVER_SUBJECT", server.GetName ());
				}
#endif				
			}

			string translated = mwr.GetFilePathTranslated ();
			if (path [path.Length - 1] != '/' && Directory.Exists (translated))
				redirect = path + '/';

			if (redirect != null) {
				Redirect (mwr, redirect);
				broker.UnregisterRequest (reqId);
				return;
			}

			ProcessRequest (mwr);
		}

		static string content301 = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
				"<html><head>\n<title>301 Moved Permanently</title>\n</head><body>\n" +
				"<h1>Moved Permanently</h1>\n" +
				"<p>The document has moved to <a href='http://{0}{1}'>http://{0}{1}</a>.</p>\n" +
				"</body></html>\n";

		static void Redirect (XSPWorkerRequest wr, string location)
		{
			string host = wr.GetKnownRequestHeader (HttpWorkerRequest.HeaderHost);
			wr.SendStatus (301, "Moved Permanently");
			wr.SendUnknownResponseHeader ("Connection", "close");
			wr.SendUnknownResponseHeader ("Date", DateTime.Now.ToUniversalTime ().ToString ("r"));
			wr.SendUnknownResponseHeader ("Location", String.Format ("http://{0}{1}", host, location));
			Encoding enc = Encoding.ASCII;
			wr.SendUnknownResponseHeader ("Content-Type", "text/html; charset=" + enc.WebName);
			string content = String.Format (content301, host, location);
			byte [] contentBytes = enc.GetBytes (content);
			wr.SendUnknownResponseHeader ("Content-Length", contentBytes.Length.ToString ());
			wr.SendResponseFromMemory (contentBytes, contentBytes.Length);
			wr.FlushResponse (true);
			wr.CloseConnection ();
		}
	}

	[Serializable]
	public class SslInformations {
		bool client_cert_allowed;
		bool client_cert_required;
		bool client_cert_valid;
		byte[] raw_client_cert;
		byte[] raw_server_cert;
		int key_size;
		int secret_key_size;

		public bool AllowClientCertificate {
			get { return client_cert_allowed; }
			set { client_cert_allowed = value; }
		}

		public bool RequireClientCertificate {
			get { return client_cert_required; }
			set { client_cert_required = value; }
		}

		public bool ClientCertificateValid {
			get { return client_cert_valid; }
			set { client_cert_valid = value; }
		}

		public int KeySize {
			get { return key_size; }
			set { key_size = value; }
		}			

		public byte[] RawClientCertificate {
			get { return raw_client_cert; }
			set { raw_client_cert = value; }
		}

		public byte[] RawServerCertificate {
			get { return raw_server_cert; }
			set { raw_server_cert = value; }
		}

		public int SecretKeySize {
			get { return secret_key_size; }
			set { secret_key_size = value; }
		}

#if MONO
		public X509Certificate GetServerCertificate ()
		{
			if (raw_server_cert == null)
				return null;

			return new X509Certificate (raw_server_cert);
		}
#endif		
	}
}

