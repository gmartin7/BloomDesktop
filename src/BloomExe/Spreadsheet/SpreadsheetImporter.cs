﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Bloom.Book;
using SIL.Xml;

namespace Bloom.Spreadsheet
{
	/// <summary>
	/// Imports data from an internal spreadsheet into a bloom book.
	/// </summary>
	public class SpreadsheetImporter
	{
		private HtmlDom _dest;
		private InternalSpreadsheet _sheet;
		private int _currentRowIndex;
		private int _currentPageIndex;
		private int _groupOnPageIndex;
		private XmlElement _currentPage;
		private XmlElement _currentGroup;
		private List<XmlElement> _pages;
		private List<XmlElement> _groupsOnPage;
		private List<string> _warnings;
		private List<ContentRow> _inputRows;

		public SpreadsheetImporter(HtmlDom dest, InternalSpreadsheet sheet)
		{
			_dest = dest;
			_sheet = sheet;
		}

		/// <summary>
		/// If true, bloom-editable elements in matched translation groups which do
		/// not have a corresponding column in the input will be deleted.
		/// </summary>
		public bool RemoveOtherLanguages => Params.RemoveOtherLanguages;

		public SpreadsheetImportParams Params = new SpreadsheetImportParams();

		/// <summary>
		/// Import the spreadsheet into the dom
		/// </summary>
		/// <returns>a list of warnings</returns>
		public List<string> Import()
		{
			_warnings = new List<string>();
			_inputRows = _sheet.ContentRows.ToList();
			_pages = _dest.GetPageElements().ToList();
			_currentRowIndex = 0;
			_currentPageIndex = -1;
			_groupsOnPage = new List<XmlElement>(0);
			AdvanceToNextGroup();
			while (_currentGroup != null || _currentRowIndex < _inputRows.Count)
			{
				var pageNumber = HtmlDom.NumberOfPage(_currentPage);
				if (_currentRowIndex >= _inputRows.Count)
				{
					if (_groupOnPageIndex > 0)
					{
						_warnings.Add($"No input row found for block {_groupOnPageIndex + 1} of page {pageNumber}");
						_currentPageIndex++;
					}

					// complain about any pages that have numbers and TGs and no input.
					// xmatter pages are not an issue.
					pageNumber = "";
					while (_currentPageIndex < _pages.Count && pageNumber == "")
					{
						var page = _pages[_currentPageIndex];
						_currentPageIndex++;
						if (TranslationGroupManager.SortedGroupsOnPage(page).Count == 0)
							continue;
						pageNumber = HtmlDom.NumberOfPage(page);
						
					}
					if (pageNumber != "")
						_warnings.Add($"No input found for pages from {pageNumber} onwards.");
					break;
				}
				var currentRow = _inputRows[_currentRowIndex];
				var rowLabel = currentRow.PageNumber;
				if (rowLabel != pageNumber)
				{
					// Do we have a later page that has the right number?
					var indexOfTargetPage = IndexOfNextPageWithNumber(rowLabel);
					if (indexOfTargetPage > 0)
					{
						// We're missing input for the current group.
						_warnings.Add($"No input row found for block {_groupOnPageIndex+1} of page {pageNumber}");
						// We want to continue the loop, ensuring that GetNextGroup() will return the first group
						// on the indicated page.
						// Enhance: possibly we should do another warning if there are also whole pages with no input?
						// but not if they have no groups.
						_currentPageIndex = indexOfTargetPage - 1;
						_groupsOnPage = new List<XmlElement>(0); // so we will at once move to next page
						AdvanceToNextGroup();
						continue; // same row, put it on that page.
					}
					// No later page matches this row. So we have nowhere to put it (until we implement
					// adding pages). Warn the user.
					var previousRowsOnSamePage = PreviousRowsOnSamePage(rowLabel);
					if (previousRowsOnSamePage == 0)
					{
						// entire page is missing.
						// Or possibly, there IS such a page, but we couldn't put
						// even one row on it because it has no TGs at all.
						// Enhance: possibly better to give different messages for these two cases?
						_warnings.Add($"Input has rows for page {rowLabel}, but document has no page {rowLabel} that can hold text");
						// advance to input row on another page
						_currentRowIndex++;
						while (_currentRowIndex < _inputRows.Count &&
						       _inputRows[_currentRowIndex].PageNumber == rowLabel)
							_currentRowIndex++;
						continue; // keep same group
					}
					else
					{
						// We've put some rows on this page, but it doesn't have room for enough.
						var rowsForPage = previousRowsOnSamePage;
						while (_currentRowIndex < _inputRows.Count &&
						       _inputRows[_currentRowIndex].PageNumber == rowLabel)
						{
							_currentRowIndex++;
							rowsForPage++;
						}

						_warnings.Add($"Input has {rowsForPage} row(s) for page {rowLabel}, but page {rowLabel} has only {previousRowsOnSamePage} place(s) for text");
						continue; // keep same group
					}
				}

				// This is actually the normal case. The next group matches the current row.
				// Fill it in and advance to the next row and group.
				PutRowInGroup(currentRow, _currentGroup);
				_currentRowIndex++;
				AdvanceToNextGroup();
			}

			return _warnings;
		}

		private int PreviousRowsOnSamePage(string label)
		{
			int lastRowOnDifferentPage = _currentRowIndex - 1;
			while (lastRowOnDifferentPage >= 0 && _inputRows[lastRowOnDifferentPage].PageNumber == label)
				lastRowOnDifferentPage--;
			return _currentRowIndex - lastRowOnDifferentPage - 1;
		}

		private int IndexOfNextPageWithNumber(string number)
		{
			for (int i = _currentPageIndex; i < _pages.Count; i++)
			{
				if (HtmlDom.NumberOfPage(_pages[i]) == number)
					return i;
			}

			return -1;
		}

		private void AdvanceToNextGroup()
		{
			_groupOnPageIndex++;
			// We arrange for this to be always true initially
			while (_groupOnPageIndex >= _groupsOnPage.Count)
			{
				_currentPageIndex++;
				if (_currentPageIndex >= _pages.Count)
				{
					_currentGroup = null;
					_currentPage = null;
					return;
				}

				_currentPage = _pages[_currentPageIndex];
				if (HtmlDom.NumberOfPage(_currentPage) == "")
					_groupsOnPage = new List<XmlElement>(0); // skip xmatter or similar page
				else
					_groupsOnPage = TranslationGroupManager.SortedGroupsOnPage(_currentPage);
				_groupOnPageIndex = 0;
			}

			_currentGroup = _groupsOnPage[_groupOnPageIndex];
		}

		/// <summary>
		/// This is where all the excitement happens. We update the specified group
		/// with the data from the spreadsheet row.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="group"></param>
		private void PutRowInGroup(ContentRow row, XmlElement group)
		{
			var sheetLanguages = _sheet.Languages;
			foreach (var lang in sheetLanguages)
			{
				var colIndex = _sheet.ColumnForLang(lang);
				var content = row.GetCell(colIndex).Content;
				var editable = HtmlDom.GetEditableChildInLang(group, lang);
				if (editable == null)
				{
					if (string.IsNullOrEmpty(content))
						continue; // Review: or make an empty one?
					var temp = HtmlDom.GetEditableChildInLang(group, "z"); // standard template element
					if (temp == null)
						temp = HtmlDom.GetEditableChildInLang(group, null); // use any available template
					if (temp == null)
					{
						// Enhance: Eventually we should be able to come up with some sort of default here.
						// Since this is a rather simple temporary expedient I haven't unit tested it.
						_warnings.Add(
							$"Could not import group {_groupOnPageIndex} ({content}) on page {HtmlDom.NumberOfPage(_currentPage)} because it has no bloom-editable children to use as templates.");
						return;
					}

					editable = temp.Clone() as XmlElement;
					editable.SetAttribute("lang", lang);
					group.AppendChild(editable);
				}

				if (HasMarkup(content))
				{
					try
					{
						editable.InnerXml = content;
					}
					catch (XmlException)
					{
						// It wasn't XML after all? Just somehow had a wedge? Keep the whole lot as text.
						SetContentAsText(editable, content);
					}
				}
				else
				{
					SetContentAsText(editable, content);
				}

			}

			if (RemoveOtherLanguages)
			{
				HtmlDom.RemoveOtherLanguages(@group, sheetLanguages);
			}
		}

		public static void SetContentAsText(XmlElement editable, string content)
		{
			// Enhance: if multiple lines, make multiple paragraphs.
			// Enhance: handle some formatting.
			foreach (var node in editable.ChildNodes.Cast<XmlNode>().ToArray())
			{
				editable.RemoveChild(node);
			}

			foreach (var item in content.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None))
			{
				var para = editable.OwnerDocument.CreateElement("p");
				editable.AppendChild(para);
				para.InnerText = item;
			}
		}

		private bool HasMarkup(string content)
		{
			// Anything that is bloom marked-up content is bound to have angle brackets.
			return content.IndexOf("<", StringComparison.InvariantCulture) >= 0;
		}
	}
}
