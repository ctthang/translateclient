#region License block : MPL 1.1/GPL 2.0/LGPL 2.1
/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is the FreeCL.Net library.
 *
 * The Initial Developer of the Original Code is 
 *  Oleksii Prudkyi (Oleksii.Prudkyi@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2008
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */
#endregion


using FreeCL.RTL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace Translate
{
	/// <summary>
	/// Description of ServicesListHtmlGenerator.
	/// </summary>
	public static class ServicesListHtmlGenerator
	{
		public static void Generate()
		{
			foreach(string lang in FreeCL.RTL.LangPack.GetLanguages())
			{
				if(lang != "Russian" && lang != "Ukrainian" && lang != "English")
					continue;
				FreeCL.RTL.LangPack.Load(lang);
				string langcode = lang.ToLowerInvariant().Substring(0, 2);
				string unpacked_file = string.Format("..\\site\\services.unpackeddata.{0}.html", langcode);
				string java_file = string.Format("..\\site\\servicesdata_{0}.java", langcode);
				BuildFile(unpacked_file, java_file);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
			
		}
		
		static void BuildFile(string fileName, string classFileName)
		{
			WebBrowser wBrowser = new WebBrowser();
			wBrowser.CreateControl();
			wBrowser.Navigate(new Uri(WebUI.ResultsWebServer.Uri, "ServicesList.aspx"));
			WebBrowserHelper.Wait(wBrowser);
			
			HtmlDocument doc = WebBrowserHelper.GetDocument(wBrowser);
			string template = wBrowser.DocumentText;
			
			GenerateDocument(wBrowser);
			int bodyidx = template.IndexOf("<body>");
			template = template.Substring(0, bodyidx);
			StringBuilder body = new StringBuilder(doc.Body.OuterHtml);
			body.Replace("FONT-SIZE: 8.25pt;", "");
			body.Replace("FONT-FAMILY: Tahoma;", "");
			body.Replace("MARGIN: -7px;", "");
			body.Replace("</BODY>", "<br><span style='color: gray;'>Generated by : " + FreeCL.RTL.ApplicationInfo.ProductName + ", version :"+ FreeCL.RTL.ApplicationInfo.ProductVersion + "</span></body>" );
						
			
			string result = template + body.ToString() + "\r\n</html>";
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
			sw.Write(result);
			sw.Flush();
			sw.Dispose();
			wBrowser.Dispose();
			
			fs = new FileStream(classFileName, FileMode.Create);
			string className = Path.GetFileNameWithoutExtension(classFileName);
			sw = new StreamWriter(fs, Encoding.BigEndianUnicode);
			sw.Write("import java.applet.*;\r\n\r\n");
			sw.Write("public class ");
			sw.Write(className);
			sw.Write(" extends Applet{\r\npublic String d(){\r\n");
			int i = 0;
			int cnt;
			StringBuilder substr;
			int var_num = 0;
			while(i < result.Length)
			{
				cnt = 16384;
				if(i + cnt > result.Length)
					cnt = result.Length - i;
				substr = new StringBuilder(result.Substring(i, cnt));
				substr.Replace("\"", "\\\"");
				substr.Replace("\r", "\\r");
				substr.Replace("\n", "\\n");
				sw.Write("String s");
				sw.Write(var_num.ToString());
					var_num++;
				sw.Write(" = ");	
				sw.Write("\"" + substr.ToString() + "\";\r\n");
				i += 16384;
			}
			sw.Write("return ");	
			for(i = 0; i < var_num; i++)
			{
				sw.Write("s");
				sw.Write(i.ToString());
				if(i + 1 < var_num)
					sw.Write("+");
			}
			sw.Write(";\r\n}\r\n}");
			sw.Flush();
			sw.Dispose();
			wBrowser.Dispose();
			
		}
		
		static void GenerateDocument(WebBrowser wBrowser)
		{
			HtmlHelper.InitDocument(wBrowser);
			GenerateListByUrlHtml(wBrowser);
			GenerateListByLangHtml(wBrowser);
		}
		
		static string GetLangsPairsCount(int count)
		{
			string format = " (" + LangPack.TranslateString("{0} language pairs") + ")";
			return string.Format(format, count);
		}

		static string GetServicesCount(int count)
		{
			string format = " (" + LangPack.TranslateString("{0} services") + ")";
			return string.Format(format, count);
		}
		
		static void GenerateServiceItemSell(WebBrowser wBrowser, ServiceItem si, string parentName, bool first, bool generateLangs)
		{
			StringBuilder htmlString = new StringBuilder();
			
			htmlString.AppendFormat(CultureInfo.InvariantCulture, 
					HtmlHelper.ServiceNameFormat, 
					si.Service.Url, 
					HttpUtility.HtmlEncode(si.Service.Url.AbsoluteUri));
					
			htmlString.Append(", " + LangPack.TranslateString(si.Service.FullName));		
			
			if(si is MonolingualDictionary)
			{
				htmlString.Append(", ");
				htmlString.Append(LangPack.TranslateLanguage(si.SupportedTranslations[0].From));
			}
			
			htmlString.Append(", ");
			htmlString.Append(ServiceSettingsContainer.GetServiceItemType(si));
			
			if(si.SupportedSubjects.Count > 1)
			{
				htmlString.Append(", " + LangPack.TranslateString("Subjects") + " : ");
				htmlString.Append(LangPack.TranslateString(SubjectConstants.Common));
				foreach(string subject in si.SupportedSubjects)
				{
					if(subject != SubjectConstants.Common)
					{
						htmlString.Append(", ");
						htmlString.Append(LangPack.TranslateString(subject));
					}
				}
			}
					
			htmlString.Append(", ");
			htmlString.Append(HttpUtility.HtmlEncode(si.Service.Copyright));
		
			if(si is MonolingualDictionary || !generateLangs)
			{
				HtmlHelper.AddTranslationCell(wBrowser, parentName, first, htmlString.ToString(), si, true);
				return;
			}	
			
			//count langs without gb\us english 
			int pairsCount = 0;
			foreach(LanguagePair lp in si.SupportedTranslations)
			{
				if(lp.From != Language.English_GB && lp.From != Language.English_US && 
					lp.To != Language.English_GB && lp.To != Language.English_US)
				pairsCount ++;	
			}
			
			string langNodeName = si.FullName + "_langs";
			htmlString.Append("<br>" + GenerateTopNode(langNodeName, LangPack.TranslateString("Languages") + GetLangsPairsCount(pairsCount), 0.5));
			HtmlHelper.AddTranslationCell(wBrowser, parentName, first, htmlString.ToString(), si, true);
			
			SortedDictionary<string, SortedDictionary<string, string>> langs = new SortedDictionary<string, SortedDictionary<string, string>>();
			foreach(LanguagePair lp in si.SupportedTranslations)
			{
				if(lp.From == Language.English_GB || lp.From == Language.English_US || 
					lp.To == Language.English_GB || lp.To == Language.English_US)
					continue;
					
				string fromlang = LangPack.TranslateLanguage(lp.From);
				SortedDictionary<string, string> inner_list;
				if(!langs.TryGetValue(fromlang, out inner_list))
				{
					inner_list = new SortedDictionary<string, string>();
					langs.Add(fromlang, inner_list);
				}
				inner_list.Add(LangPack.TranslateLanguage(lp.To), "");
			}	
			
			if(si.SupportedTranslations.Count <= 10)		
			{
				htmlString = new StringBuilder();
				foreach(KeyValuePair<string, SortedDictionary<string, string>> kvp_langs in langs)	
				{
					foreach(KeyValuePair<string, string> kvp_to_langs in kvp_langs.Value)
					{
						htmlString.Append("<li>" + kvp_langs.Key + "->" + kvp_to_langs.Key + "</li>");
					}
				}
				HtmlHelper.SetNodeInnerHtml(wBrowser, langNodeName, htmlString.ToString());
			}
			else
			{
				htmlString = new StringBuilder();
				foreach(KeyValuePair<string, SortedDictionary<string, string>> kvp_langs in langs)	
				{
					string nodeName = si.FullName + "_lang_" + kvp_langs.Key;
					nodeName = nodeName.Replace("'", "_");
					htmlString.Append(GenerateTopNode(nodeName, kvp_langs.Key + "->" + GetLangsPairsCount(kvp_langs.Value.Count) , 1));
				}
				HtmlHelper.SetNodeInnerHtml(wBrowser, langNodeName, htmlString.ToString());

				
				foreach(KeyValuePair<string, SortedDictionary<string, string>> kvp_langs in langs)	
				{
					string nodeName = si.FullName + "_lang_" + kvp_langs.Key;
					nodeName = nodeName.Replace("'", "_");
					htmlString = new StringBuilder();
					foreach(KeyValuePair<string, string> kvp_to_langs in kvp_langs.Value)
					{
						htmlString.Append("<li>" + kvp_to_langs.Key + "</li>");
					}
					HtmlHelper.SetNodeInnerHtml(wBrowser, nodeName, htmlString.ToString());
				}
				
			}
			
		}
		
		static void GenerateListByUrlHtml(WebBrowser wBrowser)
		{
			string nodeName = "list_by_url";
			
			string InnerHtml = GenerateTopNode(nodeName, LangPack.TranslateString("Grouped by Service's Url") + " - " + Manager.Services.Count.ToString(), 0, true);
			HtmlHelper.AddTranslationCell(wBrowser, null, true, InnerHtml, null);
			
			HtmlHelper.CreateTable(wBrowser, nodeName, nodeName + "_table");
			
			SortedDictionary<string, List<ServiceItem>> list = new SortedDictionary<string, List<ServiceItem>>();
			foreach(Service service in Manager.Services)
			{
				List<ServiceItem> inner_list = new List<ServiceItem>();
				list.Add(service.Url.AbsoluteUri, inner_list);
				foreach(Translator translator in service.Translators)
					inner_list.Add(translator);
				foreach(BilingualDictionary dictionary in service.BilingualDictionaries)
					inner_list.Add(dictionary);
				foreach(MonolingualDictionary dictionary in service.MonolingualDictionaries)					
					inner_list.Add(dictionary);
			}
			
			bool is_first = true; 
			foreach(KeyValuePair<string, List<ServiceItem>> kvp in list)
			{
				foreach(ServiceItem si in kvp.Value)
				{
					GenerateServiceItemSell(wBrowser, si, nodeName + "_table_body", is_first, true);
					if(is_first) is_first = false;
				}
			}
		}

		static void GenerateListByLangHtml(WebBrowser wBrowser)
		{
			//count langs without gb\us english 
			int pairsCount = 0;
			foreach(LanguagePair lp in Manager.LanguagePairServiceItems.Keys)
			{
				if(lp.From != Language.English_GB && lp.From != Language.English_US && 
					lp.To != Language.English_GB && lp.To != Language.English_US)
				pairsCount ++;	
			}
		
			string nodeName = "list_by_lang";
			string InnerHtml = GenerateTopNode(nodeName, LangPack.TranslateString("Grouped by Language") + GetLangsPairsCount(pairsCount), 0, true);
			HtmlHelper.AddTranslationCell(wBrowser, null, true, InnerHtml, null);
			HtmlHelper.CreateTable(wBrowser, nodeName, nodeName + "_table");
			
			SortedDictionary<string, SortedDictionary<string, List<ServiceItem>>> langs = new SortedDictionary<string, SortedDictionary<string, List<ServiceItem>>>();
			
			foreach(KeyValuePair<LanguagePair, ServiceItemsCollection> kvpData in Manager.LanguagePairServiceItems)
			{
				if(kvpData.Key.From == Language.English_GB || kvpData.Key.From == Language.English_US || 
					kvpData.Key.To == Language.English_GB || kvpData.Key.To == Language.English_US)
					continue;
			
				string fromlang = LangPack.TranslateLanguage(kvpData.Key.From);
				string tolang = LangPack.TranslateLanguage(kvpData.Key.To);
				SortedDictionary<string, List<ServiceItem>> inner_list;
				if(!langs.TryGetValue(fromlang, out inner_list))
				{
					inner_list = new SortedDictionary<string, List<ServiceItem>>();
					langs.Add(fromlang, inner_list);
				}
				List<ServiceItem> items;
				if(!inner_list.TryGetValue(tolang, out items))
				{
					items = new List<ServiceItem>();
					inner_list.Add(tolang, items);
				}
				
				foreach(ServiceItem si in kvpData.Value)
				{
					if(!(si is MonolingualSearchEngine || si is BilingualSearchEngine))
						items.Add(si);
				}	
			}

			
			foreach(KeyValuePair<string, SortedDictionary<string, List<ServiceItem>>> kvp in langs)
			{
				string htmlString = "";
			
				string childnodeName = "by_lang_" + kvp.Key;
				childnodeName = childnodeName.Replace("'", "_");
				htmlString += GenerateTopNode(childnodeName, "-" + kvp.Key + " ->" + GetLangsPairsCount(kvp.Value.Count));
				HtmlHelper.AddTranslationCell(wBrowser, nodeName + "_table_body", true, htmlString, null);
				
				HtmlHelper.CreateTable(wBrowser, childnodeName, childnodeName + "_table");
				string topchildnodeName = childnodeName + "_table_body";
				foreach(KeyValuePair<string, List<ServiceItem>> kvpToLangs in kvp.Value)
				{
					if(kvpToLangs.Value.Count == 0)
						continue;
					htmlString = "";
				
					
					childnodeName = "by_lang_" + kvp.Key + "_" + kvpToLangs.Key;
					childnodeName = childnodeName.Replace("'", "_");
					htmlString += GenerateTopNode(childnodeName, kvp.Key + "->" + kvpToLangs.Key + " -" + GetServicesCount(kvpToLangs.Value.Count) , 1);
					HtmlHelper.AddTranslationCell(wBrowser, topchildnodeName, true, htmlString, null);
					
					HtmlHelper.CreateTable(wBrowser, childnodeName, childnodeName + "_table");
					
					SortedDictionary<string, List<ServiceItem>> sortedServices = new SortedDictionary<string, List<ServiceItem>>();
					foreach(ServiceItem si in kvpToLangs.Value)
					{
						List<ServiceItem> inner_list;
						if(!sortedServices.TryGetValue(si.Service.Url.AbsoluteUri, out inner_list))
						{
							inner_list = new List<ServiceItem>();
							sortedServices.Add(si.Service.Url.AbsoluteUri, inner_list);
						}
						inner_list.Add(si);
					}
					
					bool is_first = true; 
					foreach(KeyValuePair<string, List<ServiceItem>> kvpServices in sortedServices)
					{
						foreach(ServiceItem si in kvpServices.Value)
						{
							GenerateServiceItemSell(wBrowser, si, childnodeName + "_table_body", is_first, false);
							if(is_first) is_first = false;
						}
					}
					
				}
			}

		}
		
		static string GenerateTopNode(string nodeName, string nodeCaption)
		{
			return GenerateTopNode(nodeName, nodeCaption, 0, false);
		}
		
		static string GenerateTopNode(string nodeName, string nodeCaption, double indent)
		{
			return GenerateTopNode(nodeName, nodeCaption, indent, false);
		}
		
		static string GenerateTopNode(string nodeName, string nodeCaption, double indent, bool visible)
		{
			StringBuilder sb = new StringBuilder();
			if(indent == 0)
				sb.Append("<div class=\"no_margins\" style= \"margin-left: 0em;\">");
			else
				sb.AppendFormat("<div  class=\"no_margins\" style= \"margin-left: {0}em;\">", indent);
				
			sb.Append("<a href=\"javascript:treeView('");
			sb.Append(nodeName);
			sb.Append("');\">");
			sb.Append(nodeCaption);
			sb.Append("</a><br><div ");
			sb.Append("id='");
			sb.Append(nodeName);
			if(!visible)
				sb.Append("' style=\"display: none;\">");
			else	
				sb.Append("' style=\"display: inline;\">");
			sb.Append("</div></div>");
			return sb.ToString();
		}
		
	}
}
