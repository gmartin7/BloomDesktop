﻿using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bloom.Publish;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SIL.IO;
using SIL.Progress;
using System.Drawing;
using System;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using SIL.Windows.Forms.ImageToolbox;

namespace Bloom.Book
{
	public class BookCompressor
	{
		public const string ExtensionForDeviceBloomBook = ".bloomd";

		// these image files may need to be reduced before being stored in the compressed output file
		internal static readonly string[] ImageFileExtensions = new[] { ".tif", ".tiff", ".png", ".bmp", ".jpg", ".jpeg" };

		// these files (if encountered) won't be included the compressed version
		internal static readonly string[] ExcludedFileExtensionsLowerCase = { ".db", ".pdf", ".bloompack", ".bak", ".userprefs", ".wav", ".bloombookorder" };

		public static string CompressBookForDevice(Book book)
		{
			// todo: probably not needed once this is being called from Publish
			{
				// In case we have any new settings since the last time we were in the Edit tab (BL-3881)
				book.BringBookUpToDate(new NullProgress());
				book.Save();
			}

			var bookFolderPath = book.FolderPath;
			// todo: there are already methods in PublishModel/PublishView to handle audio including displaying the Lame is missing page.
			// Once this is actually being called from Publish, this can probably be reworked a little.
			AudioProcessor.TryCompressingAudioAsNeeded(bookFolderPath, book.RawDom);

			using (var tempFile = TempFile.WithFilenameInTempFolder(book.Title + ExtensionForDeviceBloomBook))
			{
				CompressDirectory(tempFile.Path, bookFolderPath, "", reduceImages:true, omitMetaJson: true);

				// todo: if the code remains as is such that the caller just gets a path, it will be responsible
				// for ensuring we aren't leaving temp files hanging around. But it might be better to eventually have this
				// code handle moving/cleaning up the zip file once it has been created.
				tempFile.Detach();
				return tempFile.Path;
			}
		}

		public static void CompressDirectory(string outputPath, string directoryToCompress, string dirNamePrefix,
			bool forReaderTools = false, bool excludeAudio = false, bool reduceImages = false, bool omitMetaJson = false)
		{
			using (var fsOut = RobustFile.Create(outputPath))
			{
				using (var zipStream = new ZipOutputStream(fsOut))
				{
					zipStream.SetLevel(9);

					var rootName = Path.GetFileName(directoryToCompress);
					int dirNameOffset = directoryToCompress.Length - rootName.Length;
					CompressDirectory(directoryToCompress, zipStream, dirNameOffset, dirNamePrefix, forReaderTools, excludeAudio, reduceImages, omitMetaJson);

					zipStream.IsStreamOwner = true; // makes the Close() also close the underlying stream
					zipStream.Close();
				}
			}
		}

		/// <summary>
		/// Adds a directory, along with all files and subdirectories, to the ZipStream.
		/// </summary>
		/// <param name="directoryToCompress">The directory to add recursively</param>
		/// <param name="zipStream">The ZipStream to which the files and directories will be added</param>
		/// <param name="dirNameOffset">This number of characters will be removed from the full directory or file name
		/// before creating the zip entry name</param>
		/// <param name="dirNamePrefix">string to prefix to the zip entry name</param>
		/// <param name="forReaderTools">If True, then some pre-processing will be done to the contents of decodable
		/// and leveled readers before they are added to the ZipStream</param>
		/// <param name="excludeAudio">If true, the contents of the audio directory will not be included</param>
		/// <param name="omitMetaJson">If true, meta.json is excluded (typically for HTML readers).</param>
		/// <para> name="reduceImages">If true, image files are reduced in size to no larger than 300x300 before saving</para>
		/// <remarks>Protected for testing purposes</remarks>
		private static void CompressDirectory(string directoryToCompress, ZipOutputStream zipStream, int dirNameOffset, string dirNamePrefix,
			bool forReaderTools, bool excludeAudio, bool reduceImages, bool omitMetaJson = false)
		{
			if (excludeAudio && Path.GetFileName(directoryToCompress).ToLowerInvariant() == "audio")
				return;
			var files = Directory.GetFiles(directoryToCompress);
			var bookFile = BookStorage.FindBookHtmlInFolder(directoryToCompress);

			foreach (var filePath in files)
			{
				if (ExcludedFileExtensionsLowerCase.Contains(Path.GetExtension(filePath.ToLowerInvariant())))
					continue; // BL-2246: skip putting this one into the BloomPack
				var fileName = Path.GetFileName(filePath).ToLowerInvariant();
				if (fileName.StartsWith(BookStorage.PrefixForCorruptHtmFiles))
					continue;
				// Various stuff we keep in the book folder that is useful for editing or bloom library
				// or displaying collections but not needed by the reader. The most important is probably
				// eliminating the pdf, which can be very large. Note that we do NOT eliminate the
				// basic thumbnail.png, as we want eventually to extract that to use in the Reader UI.
				if (fileName=="thumbnail-70.png" || fileName=="thumbnail-256.png"
					|| fileName == "previewmode.css")
					continue;
				if (fileName == "meta.json" && omitMetaJson)
					continue;

				FileInfo fi = new FileInfo(filePath);

				var entryName = dirNamePrefix + filePath.Substring(dirNameOffset);  // Makes the name in zip based on the folder
				entryName = ZipEntry.CleanName(entryName);          // Removes drive from name and fixes slash direction
				ZipEntry newEntry = new ZipEntry(entryName)
				{
					DateTime = fi.LastWriteTime,
					IsUnicodeText = true
				};
				// encode filename and comment in UTF8
				byte[] modifiedContent = {};

				// if this is a ReaderTools book, call GetBookReplacedWithTemplate() to get the contents
				if (forReaderTools && (bookFile == filePath))
				{
					modifiedContent = Encoding.UTF8.GetBytes(GetBookReplacedWithTemplate(filePath));
					newEntry.Size = modifiedContent.Length;
				}
				else if (forReaderTools && (Path.GetFileName(filePath) == "meta.json"))
				{
					modifiedContent = Encoding.UTF8.GetBytes(GetMetaJsonModfiedForTemplate(filePath));
					newEntry.Size = modifiedContent.Length;
				}
				else if (reduceImages && ImageFileExtensions.Contains(Path.GetExtension(filePath.ToLowerInvariant())))
				{
					modifiedContent = GetBytesOfReducedImage(filePath);
					newEntry.Size = modifiedContent.Length;
				}
				else if (reduceImages && (bookFile == filePath))
				{
					modifiedContent = StripImagesWithMissingSrc(bookFile);
					newEntry.Size = modifiedContent.Length;
				}
				else
				{
					newEntry.Size = fi.Length;
				}

				zipStream.PutNextEntry(newEntry);

				if (modifiedContent.Length > 0)
				{
					using (var memStream = new MemoryStream(modifiedContent))
					{
						StreamUtils.Copy(memStream, zipStream, new byte[modifiedContent.Length]);
					}
				}
				else
				{
					// Zip the file in buffered chunks
					byte[] buffer = new byte[4096];
					using (var streamReader = RobustFile.OpenRead(filePath))
					{
						StreamUtils.Copy(streamReader, zipStream, buffer);
					}
				}

				zipStream.CloseEntry();
			}

			var folders = Directory.GetDirectories(directoryToCompress);

			foreach (var folder in folders)
			{
				var dirName = Path.GetFileName(folder);
				if ((dirName == null) || (dirName.ToLowerInvariant() == "sample texts"))
					continue; // Don't want to bundle these up

				CompressDirectory(folder, zipStream, dirNameOffset, dirNamePrefix, forReaderTools, excludeAudio, reduceImages);
			}
		}

		private static byte[] StripImagesWithMissingSrc(string bookFile)
		{
			// Suspect this is faster than reading the whole thing into a DOM and using xpath.
			// That might be marginally more robust, but I think this is good enough.
			// The main purpose of this is to remove stubs put in to support optional branding files.
			// The javascript that hides the element if the file is not found doesn't work in the reader.
			var content = File.ReadAllText(bookFile, Encoding.UTF8);
			// Note that whitespace is not valid in places like < img> or <img / > or < / img>.
			// I expect tidy will make sure we really don't have any, so I'm not checking for it in
			// those spots.
			var regex = new Regex("<img[^>]*src\\s*=\\s*(['\"])(.*?)\\1[^>]*(/>|>\\s*</img\\s*>)");
			var match = regex.Match(content);
			var folderPath = Path.GetDirectoryName(bookFile);
			while (match.Success)
			{
				var file = match.Groups[2].Value;
				if (!File.Exists(Path.Combine(folderPath, file)))
				{
					content = content.Substring(0, match.Index) + content.Substring(match.Index + match.Length);
					match = regex.Match(content, match.Index);
				}
				else
				{
					match = regex.Match(content, match.Index + match.Length);
				}
			}
			return Encoding.UTF8.GetBytes(content);
		}

		private static string GetMetaJsonModfiedForTemplate(string path)
		{
			var meta = BookMetaData.FromString(RobustFile.ReadAllText(path));
			meta.IsSuitableForMakingShells = true;
			return meta.Json;
		}

		/// <summary>
		/// Does some pre-processing on reader files
		/// </summary>
		/// <param name="bookPath"></param>
		private static string GetBookReplacedWithTemplate(string bookPath)
		{
			//TODO: the following, which is the original code before late in 3.6, just modified the tags in the HTML
			//Whereas currently, we use the meta.json as the authoritative source.
			//TODO Should we just get rid of these tags in the HTML? Can they be accessed from javascript? If so,
			//then they will be needed eventually (as we involve c# less in the UI)
			var text = RobustFile.ReadAllText(bookPath, Encoding.UTF8);
			// Note that we're getting rid of preceding newline but not following one. Hopefully we cleanly remove a whole line.
			// I'm not sure the </meta> ever occurs in html files, but just in case we'll match if present.
			var regex = new Regex("\\s*<meta\\s+name=(['\\\"])lockedDownAsShell\\1 content=(['\\\"])true\\2>(</meta>)? *");
			var match = regex.Match(text);
			if (match.Success)
				text = text.Substring(0, match.Index) + text.Substring(match.Index + match.Length);

			// BL-2476: Readers made from BloomPacks should have the formatting dialog disabled
			regex = new Regex("\\s*<meta\\s+name=(['\\\"])pageTemplateSource\\1 content=(['\\\"])(Leveled|Decodable) Reader\\2>(</meta>)? *");
			match = regex.Match(text);
			if (match.Success)
			{
				// has the lockFormatting meta tag been added already?
				var regexSuppress = new Regex("\\s*<meta\\s+name=(['\\\"])lockFormatting\\1 content=(['\\\"])(.*)\\2>(</meta>)? *");
				var matchSuppress = regexSuppress.Match(text);
				if (matchSuppress.Success)
				{
					// the meta tag already exists, make sure the value is "true"
					if (matchSuppress.Groups[3].Value.ToLower() != "true")
					{
						text = text.Substring(0, matchSuppress.Groups[3].Index) + "true"
								+  text.Substring(matchSuppress.Groups[3].Index + matchSuppress.Groups[3].Length);
					}
				}
				else
				{
					// the meta tag has not been added, add it now
					text = text.Insert(match.Index + match.Length,
						"\r\n    <meta name=\"lockFormatting\" content=\"true\"></meta>");
				}
			}

			return text;
		}

		const int kMaxWidth = 300;
		const int kMaxHeight = 300;

		/// <summary>
		/// For electronic books, we want to minimize the actual size of images since they'll
		/// be displayed on small screens anyway.  So before zipping up the file, we replace its
		/// bytes with the bytes of a reduced copy of itself.  If the original image is already
		/// small enough, we return its bytes directly.
		/// </summary>
		/// <returns>The bytes of the (possibly) reduced image.</returns>
		internal static byte[] GetBytesOfReducedImage(string filePath)
		{
			using (var image = Image.FromFile(filePath))
			{
				int originalWidth = image.Width;
				int originalHeight = image.Height;
				if (originalWidth > kMaxWidth || originalHeight > kMaxHeight)
				{
					// Preserve the aspect ratio
					float scaleX = (float)kMaxWidth / (float)originalWidth;
					float scaleY = (float)kMaxHeight / (float)originalHeight;
					float scale = Math.Min(scaleX, scaleY);

					// New width and height maintaining the aspect ratio
					int newWidth = (int)(originalWidth * scale);
					int newHeight = (int)(originalHeight * scale);
					using (var newImage = new Bitmap(newWidth, newHeight, image.PixelFormat))
					{
						// Draws the image in the specified size with quality mode set to HighQuality
						using (Graphics graphics = Graphics.FromImage(newImage))
						{
							graphics.CompositingQuality = CompositingQuality.HighQuality;
							graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
							graphics.SmoothingMode = SmoothingMode.HighQuality;
							graphics.DrawImage(image, 0, 0, newWidth, newHeight);
						}
						// Save the file in the same format as the original, and return its bytes.
						using (var tempFile = TempFile.WithExtension(Path.GetExtension(filePath)))
						{
							RobustImageIO.SaveImage(newImage, tempFile.Path, image.RawFormat);
							// Copy the metadata from the original file to the new file.
							var metadata = SIL.Windows.Forms.ClearShare.Metadata.FromFile(filePath);
							if (!metadata.IsEmpty)
								metadata.Write(tempFile.Path);
							return RobustFile.ReadAllBytes(tempFile.Path);
						}
					}
				}
			}
			return RobustFile.ReadAllBytes(filePath);
		}
	}
}
