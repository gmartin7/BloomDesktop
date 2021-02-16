﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using Bloom.TeamCollection;
using BloomTemp;
using NUnit.Framework;
using SIL.IO;

namespace BloomTests.TeamCollection
{
	public class FolderTeamCollectionTests
	{
		private TemporaryFolder _sharedFolder;
		private TemporaryFolder _collectionFolder;
		private TestFolderTeamCollection _collection;
		private BookStatus _myBookStatus;
		private BookStatus _anotherBookStatus;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			_sharedFolder = new TemporaryFolder("FolderTeamCollectionTests_Shared");
			_collectionFolder = new TemporaryFolder("FolderTeamCollectionTests_Collection");
			FolderTeamCollection.CreateTeamCollectionSettingsFile(_collectionFolder.FolderPath, _sharedFolder.FolderPath);
			_collection = new TestFolderTeamCollection(_collectionFolder.FolderPath, _sharedFolder.FolderPath);

			// Make some books and check them in. Individual tests verify the results.
			// This book has an additional file, including a subfolder, to ensure they get
			// saved also.
			var bookFolderPath = Path.Combine(_collectionFolder.FolderPath, "My book");
			Directory.CreateDirectory(bookFolderPath);
			var bookPath = Path.Combine(bookFolderPath, "My book.htm");
			RobustFile.WriteAllText(bookPath, "This is just a dummy");
			var cssPath = Path.Combine(bookFolderPath, "BasicLayout.css");
			RobustFile.WriteAllText(cssPath, "This is another dummy");
			var audioDirectory = Path.Combine(bookFolderPath, "audio");
			Directory.CreateDirectory(audioDirectory);
			var mp3Path = Path.Combine(audioDirectory, "rubbish.mp3");
			RobustFile.WriteAllText(mp3Path, "Fake mp3");

			var secondBookFolderPath = Path.Combine(_collectionFolder.FolderPath, "Another book");
			Directory.CreateDirectory(secondBookFolderPath);
			var anotherBookPath = Path.Combine(secondBookFolderPath, "Another book.htm");
			RobustFile.WriteAllText(anotherBookPath, "This is just a dummy for another book");

			_myBookStatus = _collection.PutBook(bookFolderPath);
			_anotherBookStatus = _collection.PutBook(secondBookFolderPath);

			// Also put the book (twice!) into lost and found
			_collection.PutBook(secondBookFolderPath, inLostAndFound:true);
			_collection.PutBook(secondBookFolderPath, inLostAndFound:true);
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_collectionFolder.Dispose();
			_sharedFolder.Dispose();
		}

		[TearDown]
		public void TearDown()
		{
			// Several tests might leave something locked, especially if they fail.
			_collection.UnlockBook("My book");
			_collection.UnlockBook("Another book");
		}

		[Test]
		public void GetCheckSum_YieldsSameAsPutBook()
		{
			Assert.That(_collection.GetChecksum("My book"), Is.EqualTo(_myBookStatus.checksum));
		}

		[Test]
		public void GetCheckSum_YieldsDifferentResults()
		{
			Assert.That(_myBookStatus.checksum, Is.Not.EqualTo(_anotherBookStatus.checksum));
		}

		[Test]
		public void PutBook_CreatesExpectedBloomFile()
		{
			var destPath = Path.Combine(_sharedFolder.FolderPath, "Books" ,"My book.bloom");
			Assert.That(RobustFile.Exists(destPath));
		}

		[Test]
		public void PutBook_WritesLocalChecksum()
		{
			Assert.That(_collection.GetLocalStatus("My book").checksum, Is.EqualTo(_myBookStatus.checksum));
			Assert.That(_collection.GetLocalStatus("Another book").checksum, Is.EqualTo(_anotherBookStatus.checksum));
		}

		[Test]
		public void CopyAllBooksFromSharedToLocalFolder_RetrievesExpectedBooks_AndChecksums()
		{
			using (var destFolder = new TemporaryFolder("GetBooks_Retrieves"))
			{
				_collection.CopyAllBooksFromSharedToLocalFolder(destFolder.FolderPath);
				var destBookFolder = Path.Combine(destFolder.FolderPath, "My book");
				var destBookPath = Path.Combine(destBookFolder, "My book.htm");
				Assert.That(RobustFile.ReadAllText(destBookPath), Is.EqualTo("This is just a dummy"));
				var destCssPath = Path.Combine(destBookFolder, "BasicLayout.css");
				Assert.That(RobustFile.ReadAllText(destCssPath), Is.EqualTo("This is another dummy"));
				var destAudioDirectory = Path.Combine(destBookFolder, "audio");
				var destMp3Path = Path.Combine(destAudioDirectory, "rubbish.mp3");
				Assert.That(RobustFile.ReadAllText(destMp3Path), Is.EqualTo("Fake mp3"));

				var anotherDestBookFolder = Path.Combine(destFolder.FolderPath, "Another book");
				var anotherDestBookPath = Path.Combine(anotherDestBookFolder, "Another book.htm");
				Assert.That(RobustFile.ReadAllText(anotherDestBookPath), Is.EqualTo("This is just a dummy for another book"));

				Assert.That(Directory.EnumerateDirectories(destFolder.FolderPath).Count(), Is.EqualTo(2));

				AssertStatusMatch(destFolder.FolderPath, "My book");
				AssertStatusMatch(destFolder.FolderPath, "Another book");
			}
		}

		void AssertStatusMatch(string destFolder, string bookName)
		{
			var repoStatus = _collection.GetStatus(bookName);
			var localStatus = _collection.GetLocalStatus(bookName, destFolder);
			Assert.That(repoStatus.checksum, Is.EqualTo(localStatus.checksum));
			Assert.That(repoStatus.lockedBy, Is.EqualTo(localStatus.lockedBy));
			Assert.That(repoStatus.lockedWhen, Is.EqualTo(localStatus.lockedWhen));
			Assert.That(repoStatus.lockedWhere, Is.EqualTo(localStatus.lockedWhere));
		}

		[Test]
		public void PutBook_DoesNotRaiseNewBookEvent()
		{
			var folderPath = Path.Combine(_collectionFolder.FolderPath, "newly put book");
			var bookPath = Path.Combine(folderPath, "newly put book.htm");
			Directory.CreateDirectory(folderPath);
			RobustFile.WriteAllText(bookPath, "<html><body>This is our newly put book</body></html>");
			_collection.StartMonitoring();
			// This is used to wait up to a second for the OS to notify us that the book changed.
			// Only after that can we be sure the event is not going to be raised.
			ManualResetEvent newbookRaised = new ManualResetEvent(false);
			// This test-only callback is used to find out when we get the OS notification.
			_collection.OnCreatedCalled = () => { newbookRaised.Set(); };
			bool newBookWasCalled = false;
			EventHandler<NewBookEventArgs>  monitorFunction = (sender, args) =>
			{
				// unexpected! We don't want the event in this case. But clean up before reporting.
				newBookWasCalled = true;
			};
			_collection.NewBook += monitorFunction;

			//sut
			_collection.PutBook(folderPath);

			var waitSucceeded = newbookRaised.WaitOne(1000);

			// cleanup
			_collection.NewBook -= monitorFunction;
			_collection.StopMonitoring();
			var newBookPath = Path.Combine(_sharedFolder.FolderPath, "newly put book.bloom");
			RobustFile.Delete(newBookPath);

			Assert.IsTrue(waitSucceeded, "New book was not raised");
			Assert.That(newBookWasCalled, Is.False, "NewBook wrongly called");
		}

		[Test]
		public void NewBook_RaisesNewBookEvent()
		{
			var newBookPath = Path.Combine(_sharedFolder.FolderPath, "Books", "A new book.bloom");
			var newBookName = "";
			_collection.StartMonitoring();
			// used to wait for the OS notification to raise the event
			ManualResetEvent newbookRaised = new ManualResetEvent(false);
			_collection.NewBook += (sender, args) =>
			{
				newBookName = args.BookName;
				newbookRaised.Set();
			};
			RobustFile.WriteAllText(newBookPath, @"Newly added book"); // no, not a zip at all
			var waitSucceeded = newbookRaised.WaitOne(1000);
			_collection.StopMonitoring();
			RobustFile.Delete(newBookPath);

			Assert.IsTrue(waitSucceeded, "New book was not raised");
			Assert.That(newBookName, Is.EqualTo("A new book.bloom"));
		}

		[Test]
		public void ChangedBook_RaisesBookChangedEvent()
		{
			var bloomBookPath = Path.Combine(_sharedFolder.FolderPath, "Books", "put book to modify.bloom");
			// Don't use PutBook here...changing the file immediately after putting it won't work,
			// because of the code that tries to prevent notifications of our own checkins.
			RobustFile.WriteAllText(bloomBookPath, @"This is original"); // no, not a zip at all

			var modifiedBookName = "";

			_collection.StartMonitoring();
			ManualResetEvent bookChangedRaised = new ManualResetEvent(false);
			EventHandler<BookStateChangeEventArgs> monitorFunction = (sender, args) =>
			{
				modifiedBookName = args.BookName;
				bookChangedRaised.Set();
			};
			_collection.BookStateChange += monitorFunction;

			// sut (at least, triggers it and waits for it)
			RobustFile.WriteAllText(bloomBookPath, @"This is changed"); // no, not a zip at all

			var waitSucceeded = bookChangedRaised.WaitOne(1000);

			// To avoid messing up other tests, clean up before asserting.
			_collection.BookStateChange -= monitorFunction;
			_collection.StopMonitoring();
			RobustFile.Delete(bloomBookPath);

			Assert.That(waitSucceeded, "book changed was not raised");
			Assert.That(modifiedBookName, Is.EqualTo("put book to modify.bloom"));
		}

		[Test]
		public void PutBook_DoesNotRaiseBookChangedEvent()
		{
			var folderPath = Path.Combine(_collectionFolder.FolderPath, "put existing book");
			var bookPath = Path.Combine(folderPath, "put existing book.htm");
			Directory.CreateDirectory(folderPath);
			RobustFile.WriteAllText(bookPath, "<html><body>This is our newly put book</body></html>");
			_collection.PutBook(folderPath); // create test situation without monitoring

			_collection.StartMonitoring();
			ManualResetEvent bookChangedRaised = new ManualResetEvent(false);
			_collection.OnChangedCalled = () => { bookChangedRaised.Set(); };
			bool bookChangedWasCalled = false;
			EventHandler<BookStateChangeEventArgs> monitorFunction = (sender, args) =>
			{
				bookChangedWasCalled = true;
			};
			_collection.BookStateChange += monitorFunction;

			//sut: put it again
			_collection.PutBook(folderPath);

			var waitSucceeded = bookChangedRaised.WaitOne(1000);

			// cleanup
			_collection.BookStateChange -= monitorFunction;
			_collection.StopMonitoring();
			var bloomBookPath = Path.Combine(_sharedFolder.FolderPath, "put existing book.bloom");
			RobustFile.Delete(bloomBookPath);

			Assert.That(waitSucceeded, "OnChanged was not called");
			Assert.That(bookChangedWasCalled, Is.False, "BookChanged wrongly called");

		}

		[Test]
		public void PutBook_InLostAndFound_DoesSo()
		{
			var lfPath = Path.Combine(_sharedFolder.FolderPath, "Lost and Found", "Another Book.bloom");
			Assert.That(RobustFile.Exists(lfPath));
		}

		[Test]
		public void PutBook_InLostAndFoundTwice_KeepsBoth()
		{
			var lfPath = Path.Combine(_sharedFolder.FolderPath, "Lost and Found", "Another Book2.bloom");
			Assert.That(RobustFile.Exists(lfPath));
		}

		[Test]
		public void WhoHasBookLocked_NotLocked_ReturnsNull()
		{
			Assert.That(_collection.WhoHasBookLocked("My book"), Is.Null);
		}

		[Test]
		public void LockBook_WhoHasBookLocked_ReturnsLocker_UnlockClears()
		{
			Assert.That(_collection.AttemptLock("My book", "joe@somewhere.org"), Is.True);
			Assert.That(_collection.WhoHasBookLocked("My book"), Is.EqualTo("joe@somewhere.org"));
			Assert.That(_collection.WhatComputerHasBookLocked("My book"), Is.EqualTo(Environment.MachineName));

			// We need to combine these tests so we leave the book in the unlocked state
			_collection.UnlockBook("My book");
			Assert.That(_collection.WhoHasBookLocked("My book"), Is.Null);
		}

		[Test]
		public void WhoHasBookLocked_NotInRepoOrLocal_ReturnsNull()
		{
			Assert.That(_collection.WhoHasBookLocked("Some book that is nowhere"), Is.Null);
		}

		[Test]
		public void WhoHasBookLocked_NotInRepoButInLocal_ReturnsThisUser()
		{
			var bookFolder = Path.Combine(_collectionFolder.FolderPath, "newly created book");
			Directory.CreateDirectory(bookFolder);
			Assert.That(_collection.WhoHasBookLocked("newly created book"), Is.EqualTo(Bloom.TeamCollection.TeamCollection.FakeUserIndicatingNewBook));
		}

		[Test]
		public void AttemptLock_LockedToSame_Succeeds_Different_Fails()
		{
			Assert.That(_collection.AttemptLock("My book", "fred@somewhere.org"),Is.True);
			Assert.That(_collection.AttemptLock("My book", "fred@somewhere.org"), Is.True);
			Assert.That(_collection.AttemptLock("My book", "joe@somewhere.org"), Is.False);
		}

		[Test]
		public void WhenWasBookLocked_RetrievesTime()
		{
			// We're saving it fairly accurately, but it's just possible rounding might make the output
			// less than DateTime.Now
			var beforeLock = DateTime.Now .AddSeconds(-0.1);
			Assert.That(_collection.AttemptLock("My book", "fred@somewhere.org"), Is.True);
			var lockTime = _collection.WhenWasBookLocked("My book");
			Assert.That(lockTime <= DateTime.Now);
			Assert.That(lockTime >= beforeLock);
		}

		// This can be reinstated temporarily (with any necessary updates, including setting inputFile
		// to an appropriate book that exists locally) to investigate how long various team collection operations take.
		//[Test]
		//public void TimeZipMaking()
		//{
		//	var inputFile = "C:\\Users\\thomson\\Documents\\Bloom\\English books 1\\003 Widow’s Gift\\003 Widow’s Gift.htm";
		//	var inputFolder = Path.GetDirectoryName(inputFile);
		//	var checksumPath = Path.Combine(inputFolder, "checksum.sha");
		//	var bookPath = Path.Combine(_sharedFolder.FolderPath, Path.ChangeExtension(Path.GetFileName(inputFile), "bloom"));
		//	var watch = new Stopwatch();
		//	watch.Start();
		//	var checksum = Bloom.Book.Book.MakeVersionCode(File.ReadAllText(inputFile), inputFile);
		//	var lastTime = watch.ElapsedMilliseconds;
		//	Debug.WriteLine("making checksum took " + lastTime);

		//	RobustFile.WriteAllText(checksumPath, checksum);
		//	Debug.WriteLine("writing checksum took " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;

		//	_collection.PutBook(inputFolder);
		//	Debug.WriteLine("writing zip took " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;
		//	using (var zipFile = new ZipFile(bookPath))
		//	{
		//		var buffer = new byte[200];
		//		var length = zipFile.GetInputStream(zipFile.GetEntry("checksum.sha")).Read(buffer, 0, 200);
		//	}
		//	Debug.WriteLine("reading checksum from entry took " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;

		//	RobustFile.Copy(bookPath, bookPath+ "1");
		//	Debug.WriteLine("copying zip took " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;

		//	using (var zipFile = new ZipFile(bookPath))
		//	{
		//		zipFile.BeginUpdate();
		//		zipFile.SetComment(checksum.ToString());
		//		zipFile.CommitUpdate();
		//		//zipFile.Close();
		//	}
		//	Debug.WriteLine("updating comment " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;

		//	using (var zipFile = new ZipFile(bookPath))
		//	{
		//		var comment = zipFile.ZipFileComment;
		//		Assert.That(comment, Is.EqualTo(checksum));
		//	}
		//	Debug.WriteLine("reading comment " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;

		//	File.ReadAllText(checksumPath);
		//	Debug.WriteLine("reading checksum from file took " + (watch.ElapsedMilliseconds - lastTime));
		//	lastTime = watch.ElapsedMilliseconds;
		//}

		[Test]
		public void WhenWasBookLocked_NotLocked_ReturnsMaxTime()
		{
			var lockTime = _collection.WhenWasBookLocked("My book");
			Assert.That(lockTime, Is.EqualTo(DateTime.MaxValue));
		}

		[Test]
		public void CopySharedCollectionFilesToLocal_RetrievesFilesPut()
		{
			var collectionFilePath = Bloom.TeamCollection.TeamCollection.CollectionPath(_collectionFolder.FolderPath);
			File.WriteAllText(collectionFilePath, "This is a fake collection");
			var customStylesPath = Path.Combine(_collectionFolder.FolderPath, "customCollectionStyles.css");
			File.WriteAllText(customStylesPath, "Fake collection styles");
			_collection.CopySharedCollectionFilesFromLocal(_collectionFolder.FolderPath);
			using (var tempDest = new TemporaryFolder("CopySharedCollectionFilesToLocal_RetrievesFilePut"))
			{

				_collection.CopySharedCollectionFilesToLocal(tempDest.FolderPath);
				var destCollectionFilePath = Path.Combine(tempDest.FolderPath, Path.GetFileName(collectionFilePath));
				Assert.That(File.ReadAllText(destCollectionFilePath), Is.EqualTo("This is a fake collection"));
				var destStylesPath = Path.Combine(tempDest.FolderPath, "customCollectionStyles.css");
				Assert.That(File.ReadAllText(destStylesPath), Is.EqualTo("Fake collection styles"));
			}
		}

		//[Test]
		//public void GetPeople_NothingLocked_ReturnsEmptyArray()
		//{
		//	Assert.That(_collection.GetPeople(), Is.EqualTo(new string[0]).AsCollection);
		//}

		//[Test]
		//public void GetPeople_TwoLocked_ReturnsUsers()
		//{
		//	_collection.AttemptLock("My book", "fred@somewhere.org");
		//	_collection.AttemptLock("Another book", "joe@nowhere.org");
		//	Assert.That(_collection.GetPeople(), Is.EqualTo(new [] { "fred@somewhere.org", "joe@nowhere.org" }).AsCollection);
		//}

		//[Test]
		//public void GetPeople_TwoLockedBySame_ReturnsOneUser()
		//{
		//	_collection.AttemptLock("My book", "fred@somewhere.org");
		//	_collection.AttemptLock("Another book", "fred@somewhere.org");
		//	Assert.That(_collection.GetPeople(), Is.EqualTo(new[] { "fred@somewhere.org" }).AsCollection);
		//}
	}
}
