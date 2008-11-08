//
// Paths.cs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (c) Copyright 2002-2007 Novell, Inc
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
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Web;
using System.Web.Hosting;
using System.Collections;
using System.Text;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mono.WebServer
{
	public class Paths {
		private Paths ()
		{
		}

		public static void GetPathsFromUri (string uri, out string realUri, out string pathInfo)
		{
			// There's a hidden missing feature here... :)
			realUri = uri; pathInfo = "";
			string basepath = HttpRuntime.AppDomainAppPath;
			string vpath = HttpRuntime.AppDomainAppVirtualPath;
			if (vpath [vpath.Length - 1] != '/')
				vpath += '/';

			if (vpath.Length > uri.Length)
				return;

			uri = uri.Substring (vpath.Length);
			while (uri.Length > 0 && uri [0] == '/')
				uri = uri.Substring (1);

			int dot, slash;
			int lastSlash = uri.Length;
			bool windows = (Path.DirectorySeparatorChar == '\\');

			for (dot = uri.LastIndexOf ('.'); dot > 0; dot = uri.LastIndexOf ('.', dot - 1)) {
				slash = uri.IndexOf ('/', dot);
				string partial;
				if (slash == -1)
					slash = lastSlash;

				partial = uri.Substring (0, slash);
				lastSlash = slash;
				string partial_win = null;
				if (windows)
					partial_win = partial.Replace ('/', '\\');

				string path = Path.Combine (basepath, (windows ? partial_win : partial));
				if (!File.Exists (path))
					continue;
				
				realUri = vpath + uri.Substring (0, slash);
				pathInfo = uri.Substring (slash);
				break;
			}
		}
	}
}
