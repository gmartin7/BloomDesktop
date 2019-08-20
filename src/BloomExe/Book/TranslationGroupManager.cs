using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Bloom.Collection;
using Bloom.ToPalaso;
using L10NSharp;
using SIL.Reporting;
using SIL.Xml;

namespace Bloom.Book
{
	/// <summary>
	/// Most editable elements in Bloom are multilingual. They have a wrapping <div> and then inner divs which are visible or not,
	/// depending on various settings. This class manages creating those inner divs, and marking them with classes that turn on
	/// visibility or move the element around the page, depending on the stylesheet in use.
	///
	/// Also, page <div/>s are marked with one of these classes: bloom-monolingual, bloom-bilingual, and bloom-trilingual
	///
	///	<div class="bloom-translationGroup" data-default-langauges='V, N1'>
	///		<div class="bloom-editable" contenteditable="true" lang="en">The Mother said...</div>
	///		<div class="bloom-editable" contenteditable="true" lang="tpi">Mama i tok:</div>
	///		<div class="bloom-editable contenteditable="true" lang="xyz">abada fakwan</div>
	///	</div>

	/// </summary>
	public class TranslationGroupManager
	{
		/// <summary>
		/// For each group of editable elements in the div which have lang attributes  (normally, a .bloom-translationGroup div),
		/// make a new element with the lang code of the vernacular (normally, a .bloom-editable).
		/// Also enable/disable editing as warranted (e.g. in shell mode or not)
		/// </summary>
		public static void PrepareElementsInPageOrDocument(XmlNode pageOrDocumentNode, CollectionSettings collectionSettings)
		{
			GenerateEditableDivsWithPreTranslatedContent(pageOrDocumentNode);

			PrepareElementsOnPageOneLanguage(pageOrDocumentNode, collectionSettings.Language1Iso639Code);
			PrepareElementsOnPageOneLanguage(pageOrDocumentNode, collectionSettings.Language2Iso639Code);

			if (!string.IsNullOrEmpty(collectionSettings.Language3Iso639Code))
			{
				PrepareElementsOnPageOneLanguage(pageOrDocumentNode, collectionSettings.Language3Iso639Code);
			}
			FixGroupStyleSettings(pageOrDocumentNode);
		}

		/// <summary>
		/// Normally, the connection between bloom-translationGroups and the dataDiv is that each bloom-editable child
		/// (which has an @lang) pulls the corresponding string from the dataDiv. This happens in BookData.
		///
		/// That works except in the case of xmatter which a) start empty and b) only normally get filled with
		/// .bloom-editable's for the current languages. Then, when bloom would normally show a source bubble listing
		/// the string in other languages, well there's nothing to show (the bubble can't pull from dataDiv).
		/// So our solution here is to pre-pack the translationGroup with bloom-editable's for each of the languages
		/// in the data-div.
		/// The original (an possibly only) instance of this is with book titles. See bl-1210.
		/// </summary>
		public static void PrepareDataBookTranslationGroups(XmlNode pageOrDocumentNode, IEnumerable<string> languageCodes)
		{
			//At first, I set out to select all translationGroups that have child .bloomEditables that have data-book attributes
			//however this has implications on other fields, noticeably the acknowledgments. So in order to get this fixed
			//and not open another can of worms, I've reduce the scope of this
			//fix to just the bookTitle, so I'm going with findOnlyBookTitleFields for now
			var findAllDataBookFields = "descendant-or-self::*[contains(@class,'bloom-translationGroup') and descendant::div[@data-book and contains(@class,'bloom-editable')]]";
			var findOnlyBookTitleFields = "descendant-or-self::*[contains(@class,'bloom-translationGroup') and descendant::div[@data-book='bookTitle' and contains(@class,'bloom-editable')]]";
			foreach (XmlElement groupElement in
					pageOrDocumentNode.SafeSelectNodes(findOnlyBookTitleFields))
			{
				foreach (var lang in languageCodes)
				{
					MakeElementWithLanguageForOneGroup(groupElement, lang);
				}
			}
		}

		/// <summary>
		/// Returns the sequence of bloom-editable divs that would normally be created as the content of an empty bloom-translationGroup,
		/// given the current collection and book settings, with the classes that would normally be set on them prior to editing to make the right ones visible etc.
		/// </summary>
		public static string GetDefaultTranslationGroupContent(XmlNode pageOrDocumentNode, Book currentBook)
		{
			// First get a XMLDocument so that we can start creating elements using it.
			XmlDocument ownerDocument = pageOrDocumentNode?.OwnerDocument;
			if (ownerDocument == null)	// the OwnerDocument child can be null if pageOrDocumentNode is a document itself
			{
				if (pageOrDocumentNode is XmlDocument)
				{
					ownerDocument = pageOrDocumentNode as XmlDocument;
				}
				else
				{
					return "";
				}
			}

			// We want to use the usual routines that insert the required bloom-editables and classes into existing translation groups.
			// To make this work we make a temporary translation group
			// Since one of the routines expects the TG to be a descendant of the element passed to it, we make another layer of temporary wrapper around the translation group as well
			var containerElement = ownerDocument.CreateElement("div");
			containerElement.SetAttribute("class", "bloom-translationGroup");

			var wrapper = ownerDocument.CreateElement("div");
			wrapper.AppendChild(containerElement);

			PrepareElementsInPageOrDocument(containerElement, currentBook.CollectionSettings);

			TranslationGroupManager.UpdateContentLanguageClasses(wrapper, currentBook.CollectionSettings, currentBook.CollectionSettings.Language1Iso639Code,
				currentBook.MultilingualContentLanguage2, currentBook.MultilingualContentLanguage3);

			return containerElement.InnerXml;
		}

		private static void GenerateEditableDivsWithPreTranslatedContent(XmlNode elementOrDom)
		{
			foreach (XmlElement editableDiv in elementOrDom.SafeSelectNodes(".//*[contains(@class,'bloom-editable') and @data-generate-translations and @data-i18n]"))
			{
				var englishText = editableDiv.InnerText;
				var l10nId = editableDiv.Attributes["data-i18n"].Value;
				if (string.IsNullOrWhiteSpace(l10nId))
					continue;

				foreach (var uiLanguage in LocalizationManager.GetAvailableLocalizedLanguages())
				{
					var translation = LocalizationManager.GetDynamicStringOrEnglish("Bloom", l10nId, englishText, null, uiLanguage);
					if (translation == englishText)
						continue;

					var newEditableDiv = elementOrDom.OwnerDocument.CreateElement("div");
					newEditableDiv.SetAttribute("class", "bloom-editable");
					newEditableDiv.SetAttribute("lang", LanguageLookupModelExtensions.GetGeneralCode(uiLanguage));
					newEditableDiv.InnerText = translation;
					editableDiv.ParentNode.AppendChild(newEditableDiv);
				}

				editableDiv.RemoveAttribute("data-generate-translations");
				editableDiv.RemoveAttribute("data-i18n");
			}
		}

		/// <summary>
		/// This is used when a book is first created from a source; without it, if the shell maker left the book as trilingual when working on it,
		/// then every time someone created a new book based on it, it too would be trilingual.
		/// </summary>
		public static void SetInitialMultilingualSetting(BookData bookData, int oneTwoOrThreeContentLanguages,
			CollectionSettings collectionSettings)
		{
			//var multilingualClass =  new string[]{"bloom-monolingual", "bloom-bilingual","bloom-trilingual"}[oneTwoOrThreeContentLanguages-1];

			if (oneTwoOrThreeContentLanguages < 3)
				bookData.RemoveAllForms("contentLanguage3");
			if (oneTwoOrThreeContentLanguages < 2)
				bookData.RemoveAllForms("contentLanguage2");

			bookData.Set("contentLanguage1", collectionSettings.Language1Iso639Code, false);
			bookData.Set("contentLanguage1Rtl", collectionSettings.IsLanguage1Rtl.ToString(), false);
			if (oneTwoOrThreeContentLanguages > 1)
			{
				bookData.Set("contentLanguage2", collectionSettings.Language2Iso639Code, false);
				bookData.Set("contentLanguage2Rtl", collectionSettings.IsLanguage2Rtl.ToString(), false);
			}
			if (oneTwoOrThreeContentLanguages > 2 && !string.IsNullOrEmpty(collectionSettings.Language3Iso639Code))
			{
				bookData.Set("contentLanguage3", collectionSettings.Language3Iso639Code, false);
				bookData.Set("contentLanguage3Rtl", collectionSettings.IsLanguage3Rtl.ToString(), false);
			}
		}


		/// <summary>
		/// We stick 'contentLanguage2' and 'contentLanguage3' classes on editable things in bilingual and trilingual books
		/// </summary>
		public static void UpdateContentLanguageClasses(XmlNode elementOrDom, CollectionSettings settings,
			string vernacularIso, string contentLanguageIso2, string contentLanguageIso3)
		{
			var multilingualClass = "bloom-monolingual";
			var contentLanguages = new Dictionary<string, string>();
			contentLanguages.Add(vernacularIso, "bloom-content1");

			if (!String.IsNullOrEmpty(contentLanguageIso2) && vernacularIso != contentLanguageIso2)
			{
				multilingualClass = "bloom-bilingual";
				contentLanguages.Add(contentLanguageIso2, "bloom-content2");
			}
			if (!String.IsNullOrEmpty(contentLanguageIso3) && vernacularIso != contentLanguageIso3 &&
				contentLanguageIso2 != contentLanguageIso3)
			{
				multilingualClass = "bloom-trilingual";
				contentLanguages.Add(contentLanguageIso3, "bloom-content3");
				Debug.Assert(!String.IsNullOrEmpty(contentLanguageIso2), "shouldn't have a content3 lang with no content2 lang");
			}

			//Stick a class in the page div telling the stylesheet how many languages we are displaying (only makes sense for content pages, in Jan 2012).
			foreach (
				XmlElement pageDiv in
					elementOrDom.SafeSelectNodes(
						"descendant-or-self::div[contains(@class,'bloom-page') and not(contains(@class,'bloom-frontMatter')) and not(contains(@class,'bloom-backMatter'))]")
				)
			{
				HtmlDom.RemoveClassesBeginingWith(pageDiv, "bloom-monolingual");
				HtmlDom.RemoveClassesBeginingWith(pageDiv, "bloom-bilingual");
				HtmlDom.RemoveClassesBeginingWith(pageDiv, "bloom-trilingual");
				HtmlDom.AddClassIfMissing(pageDiv, multilingualClass);
			}


			// This is the "code" part of the visibility system: https://goo.gl/EgnSJo
			foreach (XmlElement group in elementOrDom.SafeSelectNodes(".//*[contains(@class,'bloom-translationGroup')]"))
			{
				var dataDefaultLanguages = HtmlDom.GetAttributeValue(group, "data-default-languages").Split(new char[] { ',', ' ' },
					StringSplitOptions.RemoveEmptyEntries);

				//nb: we don't necessarily care that a div is editable or not
				foreach (XmlElement e in @group.SafeSelectNodes(".//textarea | .//div"))
				{
					HtmlDom.RemoveClassesBeginingWith(e, "bloom-content");
					var lang = e.GetAttribute("lang");

					//These bloom-content* classes are used by some stylesheet rules, primarily to boost the font-size of some languages.
					//Enhance: this is too complex; the semantics of these overlap with each other and with bloom-visibility-code-on, and with data-language-order.
					//It would be better to have non-overlapping things; 1 for order, 1 for visibility, one for the lang's role in this collection.
					string orderClass;
					if (contentLanguages.TryGetValue(lang, out orderClass))
					{
						HtmlDom.AddClass(e, orderClass); //bloom-content1, bloom-content2, bloom-content3
					}

					//Enhance: it's even more likely that we can get rid of these by replacing them with bloom-content2, bloom-content3
					if (lang == settings.Language2Iso639Code)
					{
						HtmlDom.AddClass(e, "bloom-contentNational1");
					}
					if (lang == settings.Language3Iso639Code)
					{
						HtmlDom.AddClass(e, "bloom-contentNational2");
					}

					HtmlDom.RemoveClassesBeginingWith(e, "bloom-visibility-code");
					if (ShouldNormallyShowEditable(lang, dataDefaultLanguages, contentLanguageIso2, contentLanguageIso3, settings))
					{
						HtmlDom.AddClass(e, "bloom-visibility-code-on");
					}

					UpdateRightToLeftSetting(settings, e, lang);
				}
			}
		}

		private static void UpdateRightToLeftSetting(CollectionSettings settings, XmlElement e, string lang)
		{
			HtmlDom.RemoveRtlDir(e);
			if((lang == settings.Language1Iso639Code && settings.IsLanguage1Rtl) ||
			   (lang == settings.Language2Iso639Code && settings.IsLanguage2Rtl) ||
			   (lang == settings.Language3Iso639Code && settings.IsLanguage3Rtl))
			{
				HtmlDom.AddRtlDir(e);
			}
		}

		/// <summary>
		/// Here, "normally" means unless the user overrides via a .bloom-visibility-user-on/off
		/// </summary>
		internal static bool ShouldNormallyShowEditable(string lang, string[] dataDefaultLanguages,
			string contentLanguageIso2, string contentLanguageIso3, // these are effected by the multilingual settings for this book
			CollectionSettings settings) // use to get the collection's current N1 and N2 in xmatter or other template pages that specify default languages
		{
			if (dataDefaultLanguages == null || dataDefaultLanguages.Length == 0
				|| string.IsNullOrWhiteSpace(dataDefaultLanguages[0])
				|| dataDefaultLanguages[0].Equals("auto",StringComparison.InvariantCultureIgnoreCase))
			{
					return lang == settings.Language1Iso639Code || lang == contentLanguageIso2 || lang == contentLanguageIso3;
			}
			else
			{
				// Hote there are (perhaps unfortunately) two different labelling systems, but they have a 1-to-1 correspondence:
				// The V/N1/N2 system feels natural in vernacular book contexts
				// The L1/L2/L3 system is more natural in source book contexts.
				return (lang == settings.Language1Iso639Code && dataDefaultLanguages.Contains("V")) ||
				   (lang == settings.Language1Iso639Code && dataDefaultLanguages.Contains("L1")) ||

				   (lang == settings.Language2Iso639Code && dataDefaultLanguages.Contains("N1")) ||
				   (lang == settings.Language2Iso639Code && dataDefaultLanguages.Contains("L2")) ||

				   (lang == settings.Language3Iso639Code && dataDefaultLanguages.Contains("N2")) ||
				   (lang == settings.Language3Iso639Code && dataDefaultLanguages.Contains("L3")) ||

				   dataDefaultLanguages.Contains(lang); // a literal language id, e.g. "en" (used by template starter)
			}
		}

		private static int GetOrderOfThisLanguageInTheTextBlock(string editableIso, string vernacularIso, string contentLanguageIso2, string contentLanguageIso3)
		{
			if (editableIso == vernacularIso)
				return 1;
			if (editableIso == contentLanguageIso2)
				return 2;
			if (editableIso == contentLanguageIso3)
				return 3;
			return -1;
		}



		private static void PrepareElementsOnPageOneLanguage(XmlNode pageDiv, string isoCode)
		{
			foreach (
				XmlElement groupElement in
					pageDiv.SafeSelectNodes("descendant-or-self::*[contains(@class,'bloom-translationGroup')]"))
			{
				MakeElementWithLanguageForOneGroup(groupElement, isoCode);

				//remove any elements in the translationgroup which don't have a lang (but ignore any label elements, which we're using for annotating groups)
				foreach (
					XmlElement elementWithoutLanguage in
						groupElement.SafeSelectNodes("textarea[not(@lang)] | div[not(@lang) and not(self::label)]"))
				{
					elementWithoutLanguage.ParentNode.RemoveChild(elementWithoutLanguage);
				}
			}

			//any editable areas which still don't have a language, set them to the vernacular (this is used for simple templates (non-shell pages))
			foreach (
				XmlElement element in
					pageDiv.SafeSelectNodes( //NB: the jscript will take items with bloom-editable and set the contentEdtable to true.
						"descendant-or-self::textarea[not(@lang)] | descendant-or-self::*[(contains(@class, 'bloom-editable') or @contentEditable='true'  or @contenteditable='true') and not(@lang)]")
				)
			{
				element.SetAttribute("lang", isoCode);
			}

			foreach (XmlElement e in pageDiv.SafeSelectNodes("descendant-or-self::*[starts-with(text(),'{')]"))
			{
				foreach (var node in e.ChildNodes)
				{
					XmlText t = node as XmlText;
					if (t != null && t.Value.StartsWith("{"))
						t.Value = "";
					//otherwise html tidy will throw away spans (at least) that are empty, so we never get a chance to fill in the values.
				}
			}
		}

		/// <summary>
		/// If the group element contains more than one child div in the given language, remove or
		/// merge the duplicates.
		/// </summary>
		/// <remarks>
		/// We've had at least one user end up with duplicate vernacular divs in a shell book.  (She
		/// was using Bloom 4.3 to translate a shell book created with Bloom 3.7.)  I haven't been
		/// able to reproduce the effect or isolate the cause, but this fixes things.
		/// See https://issues.bloomlibrary.org/youtrack/issue/BL-6923.
		/// </remarks>
		internal static void FixDuplicateLanguageDivs(XmlElement groupElement, string isoCode)
		{
			XmlNodeList list = groupElement.SafeSelectNodes("./div[@lang='" + isoCode + "']");
			if (list.Count > 1)
			{
				var count = list.Count;
				foreach (XmlNode div in list)
				{
					var innerText = div.InnerText.Trim();
					if (String.IsNullOrEmpty(innerText))
					{
						Logger.WriteEvent($"An empty duplicate div for {isoCode} has been removed from a translation group.");
						groupElement.RemoveChild(div);
						--count;
						if (count == 1)
							break;
					}
				}
				if (count > 1)
				{
					Logger.WriteEvent($"Duplicate divs for {isoCode} have been merged in a translation group.");
					list = groupElement.SafeSelectNodes("./div[@lang='" + isoCode + "']");
					XmlNode first = list[0];
					for (int i = 1; i < list.Count; ++i)
					{
						var newline = groupElement.OwnerDocument.CreateTextNode(Environment.NewLine);
						first.AppendChild(newline);
						foreach (XmlNode node in list[i].ChildNodes)
							first.AppendChild(node);
						groupElement.RemoveChild(list[i]);
					}
				}
			}
		}

		/// <summary>
		/// Shift any translationGroup level style setting to child editable divs that lack a style.
		/// This is motivated by the fact HTML/CSS underlining cannot be turned off in child nodes.
		/// So if underlining is enabled for Normal, everything everywhere would be underlined
		/// regardless of style or immediate character formatting.  If a style sets underlining
		/// on, then immediate character formatting cannot turn it off anywhere in a text box that
		/// uses that style.  See https://silbloom.myjetbrains.com/youtrack/issue/BL-6282.
		/// </summary>
		private static void FixGroupStyleSettings(XmlNode pageDiv)
		{
			foreach (
				XmlElement groupElement in
					pageDiv.SafeSelectNodes("descendant-or-self::*[contains(@class,'bloom-translationGroup')]"))
			{
				var groupStyle = HtmlDom.GetStyle(groupElement);
				if (String.IsNullOrEmpty(groupStyle))
					continue;
				// Copy the group's style setting to any child div with the bloom-editable class that lacks one.
				// Then remove the group style if it does have any child divs with the bloom-editable class.
				bool hasInternalEditableDiv = false;
				foreach (XmlElement element in groupElement.SafeSelectNodes("child::div[contains(@class, 'bloom-editable')]"))
				{
					var divStyle = HtmlDom.GetStyle(element);
					if (String.IsNullOrEmpty(divStyle))
						HtmlDom.AddClass(element, groupStyle);
					hasInternalEditableDiv = true;
				}
				if (hasInternalEditableDiv)
					HtmlDom.RemoveClass(groupElement, groupStyle);
			}
		}

		/// <summary>
		/// For each group (meaning they have a common parent) of editable items, we
		/// need to make sure there are the correct set of copies, with appropriate @lang attributes
		/// </summary>
		private static void MakeElementWithLanguageForOneGroup(XmlElement groupElement, string isoCode)
		{
			if (groupElement.GetAttribute("class").Contains("STOP"))
			{
				Console.Write("stop");
			}
			XmlNodeList editableChildrenOfTheGroup =
				groupElement.SafeSelectNodes("*[self::textarea or contains(@class,'bloom-editable')]");

			var elementsAlreadyInThisLanguage = from XmlElement x in editableChildrenOfTheGroup
												where x.GetAttribute("lang") == isoCode
												select x;
			if (elementsAlreadyInThisLanguage.Any())
				//don't mess with this set, it already has a vernacular (this will happen when we're editing a shellbook, not just using it to make a vernacular edition)
				return;

			if (groupElement.SafeSelectNodes("ancestor-or-self::*[contains(@class,'bloom-translationGroup')]").Count == 0)
				return;

			var prototype = editableChildrenOfTheGroup[0] as XmlElement;
			XmlElement newElementInThisLanguage;
			if (prototype == null) //this was an empty translation-group (unusual, but we can cope)
			{
				newElementInThisLanguage = groupElement.OwnerDocument.CreateElement("div");
				newElementInThisLanguage.SetAttribute("class", "bloom-editable");
				newElementInThisLanguage.SetAttribute("contenteditable", "true");
				if (groupElement.HasAttribute("data-placeholder"))
				{
					newElementInThisLanguage.SetAttribute("data-placeholder", groupElement.GetAttribute("data-placeholder"));
				}
				groupElement.AppendChild(newElementInThisLanguage);
			}
			else //this is the normal situation, where we're just copying the first element
			{
				//what we want to do is copy everything in the element, except that which is specific to a language.
				//so classes on the element, non-text children (like images), etc. should be copied
				newElementInThisLanguage = (XmlElement) prototype.ParentNode.InsertAfter(prototype.Clone(), prototype);
				//if there is an id, get rid of it, because we don't want 2 elements with the same id
				newElementInThisLanguage.RemoveAttribute("id");

				// No need to copy over the audio-sentence markup
				// Various code expects elements with class audio-sentence to have an ID.
				// Both will be added when and if  we do audio recording (in whole-text-box mode) on the new div.
				// Until then it makes things more consistent if we make sure elements without ids
				// don't have this class.
				// Also, if audio recording markup is done using one audio-sentence span per sentence, we won't copy it.  (Because we strip all out the text underneath this node)
				// So, it's more consistent to treat all scenarios the same way (don't copy the audio-sentence markup)
				HtmlDom.RemoveClass(newElementInThisLanguage, "audio-sentence");

				//OK, now any text in there will belong to the prototype language, so remove it, while retaining everything else
				StripOutText(newElementInThisLanguage);
			}
			newElementInThisLanguage.SetAttribute("lang", isoCode);
		}

		/// <summary>
		/// Remove nodes that are either pure text or exist only to contain text, including BR and P
		/// Elements with a "bloom-cloneToOtherLanguages" class are preserved
		/// </summary>
		/// <param name="element"></param>
		private static void StripOutText(XmlNode element)
		{
			var listToRemove = new List<XmlNode>();
			foreach (XmlNode node in element.SelectNodes("descendant-or-self::*[(self::p or self::br or self::u or self::b or self::i) and not(contains(@class,'bloom-cloneToOtherLanguages'))]"))
			{
				listToRemove.Add(node);
			}
			// clean up any remaining texts that weren't enclosed
			foreach (XmlNode node in element.SelectNodes("descendant-or-self::*[not(contains(@class,'bloom-cloneToOtherLanguages'))]/text()"))
			{
				listToRemove.Add(node);
			}
			RemoveXmlChildren(listToRemove);
		}

		private static void RemoveXmlChildren(List<XmlNode> removalList)
		{
			foreach (var node in removalList)
			{
				if(node.ParentNode != null)
					node.ParentNode.RemoveChild(node);
			}
		}
	}
}
