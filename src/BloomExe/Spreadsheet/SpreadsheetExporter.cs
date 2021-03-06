﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Bloom.Book;
using SIL.Xml;

namespace Bloom.Spreadsheet
{
	public class SpreadsheetExporter
	{
		InternalSpreadsheet _spreadsheet = new InternalSpreadsheet();

		public SpreadsheetExportParams Params = new SpreadsheetExportParams();
		public InternalSpreadsheet Export(HtmlDom dom)
		{
			var pages = dom.GetPageElements();
			foreach (var page in pages)
			{
				var pageNumber = page.Attributes["data-page-number"]?.Value ?? "";
				// For now we will ignore all un-numbered pages, particularly xmatter,
				// which eventually needs to be handled by exporting data-book items.
				if (pageNumber == "")
					continue;
				var groups = TranslationGroupManager.SortTranslationGroups(TranslationGroupManager.GetTranslationGroups(page));
				int groupIndex = 1;
				foreach (var group in groups)
				{
					AddTranslationGroupRow(group, pageNumber, groupIndex);
					groupIndex++;
				}
			}
			return _spreadsheet;
		}

		private void AddTranslationGroupRow(XmlNode group, string pageNumber, int groupIndex)
		{
			var row = new ContentRow();
			row.AddCell(InternalSpreadsheet.TextGroupLabel);
			row.AddCell(pageNumber);
			row.AddCell(groupIndex.ToString(CultureInfo.InvariantCulture));
			foreach (var editable in group.SafeSelectNodes("./*[contains(@class, 'bloom-editable')]").Cast<XmlElement>())
			{
				var lang = editable.Attributes["lang"]?.Value ?? "";
				if (lang == "z" || lang == "")
					continue;
				var index = _spreadsheet.ColumnForLang(lang);
				var content = Params.RetainMarkup ? editable.InnerXml : GetContent(editable);
				row.SetCell(index,content);
			}

			_spreadsheet.AddRow(row);
		}

		/// <summary>
		/// Get some sort of reasonable text representation of the content of a bloom-editable.
		/// Adds newlines after paragraphs, except the last. (Adding after last produces
		/// a blank line at the end of every cell.)
		/// Drops leading and trailing, but not intermediate, white space.
		/// </summary>
		/// <param name="editable"></param>
		/// <returns></returns>
		public static string GetContent(XmlElement editable)
		{
			var result = new StringBuilder();
			var pending = "";
			foreach (XmlNode x in editable.ChildNodes.Cast<XmlNode>())
			{
				if (string.IsNullOrWhiteSpace(x.InnerText) && x.Name != "p")
				{
					if (result.Length > 0)
						pending += x.InnerText;
					continue;
				}

				result.Append(pending);
				pending = "";
				result.Append(x.InnerText);
				if (x.Name == "p")
				{
					// We want a line break here, but only if something follows...we don't need a blank line at
					// the end of the cell, which is what Excel will do with a trailing newline.
					// Review or Environment.Newline? But I'd rather generate something consistent.
					// Linux: what line break is best to use when constructing an Excel spreadsheet in Linux?
					pending = "\r\n";
				}
			}

			return result.ToString();
		}
	}
}
