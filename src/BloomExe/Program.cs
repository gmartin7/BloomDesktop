﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Bloom.Collection.BloomPack;
using Bloom.Properties;
using Localization;
using Palaso.IO;
using Palaso.Reporting;

namespace Bloom
{
	static class Program
	{

		//static HttpListener listener = new HttpListener();

		/// <summary>
		/// We have one project open at a time, and this helps us bootstrap the project and
		/// properly dispose of various things when the project is closed.
		/// </summary>
		private static ProjectContext _projectContext;
		private static ApplicationContainer _applicationContainer;
		public static bool StartUpWithFirstOrNewVersionBehavior;
		private static Mutex _oneInstancePerProjectMutex;

		[STAThread]
		[HandleProcessCorruptedStateExceptions]
		static void Main(string[] args)
		{
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				TimeBomb();

				//bring in settings from any previous version
				if (Settings.Default.NeedUpgrade)
				{
					//see http://stackoverflow.com/questions/3498561/net-applicationsettingsbase-should-i-call-upgrade-every-time-i-load
					Settings.Default.Upgrade();
					Settings.Default.NeedUpgrade = false;
					Settings.Default.Save();
					StartUpWithFirstOrNewVersionBehavior = true;
				}

				SetUpErrorHandling();
				SetUpLocalization();
				Logger.Init();


				if (args.Length == 1 && args[0].ToLower().EndsWith(".bloompack"))
				{
					using (var dlg = new BloomPackInstallDialog(args[0]))
					{
						dlg.ShowDialog();
					}
					return;
				}

				Splasher.Show();
				SetUpReporting();
				Settings.Default.Save();

				_applicationContainer = new ApplicationContainer();

				Browser.SetUpXulRunner();

#if !DEBUG
			SetUpErrorHandling();
#endif

				StartUpShellBasedOnMostRecentUsedIfPossible();
				Application.Idle += new EventHandler(Application_Idle);

				Localization.LocalizationManager.SetUILanguage(Settings.Default.UserInterfaceLanguage,false);
				try
				{
					Application.Run();
				}
				catch (System.AccessViolationException nasty)
				{
					Logger.ShowUserATextFileRelatedToCatastrophicError(nasty);
					return;
				}

				Settings.Default.Save();

				Logger.ShutDown();

				if (_projectContext != null)
					_projectContext.Dispose();
			}
			finally
			{
				ReleaseMutexForThisProject();
			}
		}



		private static void Application_Idle(object sender, EventArgs e)
		{
			Application.Idle -= new EventHandler(Application_Idle);
			Splasher.Close();
		}


		private static bool GrabTokenForThisProject(string pathToProject)
		{
			//ok, here's how this complex method works...
			//First, we try to get the mutex quickly and quitely.
			//If that fails, we put up a dialog and wait a number of seconds,
			//while we wait for the mutex to come free.


			string mutexId = pathToProject;
			mutexId = mutexId.Replace(Path.DirectorySeparatorChar, '-');
			mutexId = mutexId.Replace(Path.VolumeSeparatorChar, '-');
			bool mutexAcquired = false;
			try
			{
				_oneInstancePerProjectMutex = Mutex.OpenExisting(mutexId);
				mutexAcquired = _oneInstancePerProjectMutex.WaitOne(TimeSpan.FromMilliseconds(1 * 1000), false);
			}
			catch (WaitHandleCannotBeOpenedException e)//doesn't exist, we're the first.
			{
				_oneInstancePerProjectMutex = new Mutex(true, mutexId, out mutexAcquired);
				mutexAcquired = true;
			}
			catch (AbandonedMutexException e)
			{
				//that's ok, we'll get it below
			}

			using (var dlg = new SimpleMessageDialog("Waiting for other Bloom to finish..."))
			{
				dlg.Show();
				try
				{
					_oneInstancePerProjectMutex = Mutex.OpenExisting(mutexId);
					mutexAcquired = _oneInstancePerProjectMutex.WaitOne(TimeSpan.FromMilliseconds(10 * 1000), false);
				}
				catch (AbandonedMutexException e)
				{
					_oneInstancePerProjectMutex = new Mutex(true, mutexId, out mutexAcquired);
					mutexAcquired = true;
				}
				catch (Exception e)
				{
					ErrorReport.NotifyUserOfProblem(e,
						"There was a problem starting Bloom which might require that you restart your computer.");
				}
			}

			if (!mutexAcquired) // cannot acquire?
			{
				_oneInstancePerProjectMutex = null;
				ErrorReport.NotifyUserOfProblem("Another copy of Bloom is already open with " + pathToProject + ". If you cannot find that Bloom, restart your computer.");
				return false;
			}
			return true;
		}

		public static void ReleaseMutexForThisProject()
		{
			if (_oneInstancePerProjectMutex != null)
			{
				_oneInstancePerProjectMutex.ReleaseMutex();
				_oneInstancePerProjectMutex = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		private static void StartUpShellBasedOnMostRecentUsedIfPossible()
		{
			if (Settings.Default.MruProjects.Latest == null  ||
				!OpenProjectWindow(Settings.Default.MruProjects.Latest))
			{
				//since the message pump hasn't started yet, show the UI for choosing when it is
				Application.Idle += ChooseAnotherProject;
			}
		}

		/// ------------------------------------------------------------------------------------
		private static bool OpenProjectWindow(string projectPath)
		{
			Debug.Assert(_projectContext == null);

			try
			{
				if (!GrabTokenForThisProject(projectPath))
				{
					return false;
				}

				_projectContext = _applicationContainer.CreateProjectContext(projectPath);
				_projectContext.ProjectWindow.Closed += HandleProjectWindowClosed;
				_projectContext.ProjectWindow.Activated += HandleProjectWindowActivated;

				_projectContext.ProjectWindow.Show();

				return true;
			}
			catch (Exception e)
			{
				HandleErrorOpeningProjectWindow(e, projectPath);
			}

			return false;
		}

		private static void HandleProjectWindowActivated(object sender, EventArgs e)
		{
			_projectContext.ProjectWindow.Activated -= HandleProjectWindowActivated;

			// Sometimes after closing the splash screen the project window
			// looses focus, so do this.
			_projectContext.ProjectWindow.Activate();
		}



		/// ------------------------------------------------------------------------------------
		private static void HandleErrorOpeningProjectWindow(Exception error, string projectPath)
		{
			if (_projectContext != null)
			{
				if (_projectContext.ProjectWindow != null)
				{
					_projectContext.ProjectWindow.Closed -= HandleProjectWindowClosed;
					_projectContext.ProjectWindow.Close();
				}

				_projectContext.Dispose();
				_projectContext = null;
			}

			Palaso.Reporting.ErrorReport.NotifyUserOfProblem(
				new Palaso.Reporting.ShowAlwaysPolicy(), error,
				"{0} had a problem loading the {1} project. Please report this problem to the developers by clicking 'Details' below.",
				Application.ProductName, Path.GetFileNameWithoutExtension(projectPath));
		}

		/// ------------------------------------------------------------------------------------
		static void ChooseAnotherProject(object sender, EventArgs e)
		{
			Application.Idle -= ChooseAnotherProject;

			while (true)
			{
				//If it looks like the 1st time, put up the create collection with the welcome.
				//The user can cancel that if they want to go looking for a collection on disk.
				if(Settings.Default.MruProjects.Latest == null)
				{
					var path=OpenAndCreateCollectionDialog.CreateNewCollection();
					if (!string.IsNullOrEmpty(path) && File.Exists(path))
					{
						OpenCollection(path);
						return;
					}
				}

				using (var dlg = _applicationContainer.OpenAndCreateCollectionDialog())
				{
					if (dlg.ShowDialog() != DialogResult.OK)
					{
						Application.Exit();
						return;
					}

					if (OpenCollection(dlg.SelectedPath)) return;
				}
			}
		}

		private static bool OpenCollection(string path)
		{
			if (OpenProjectWindow(path))
			{
				Settings.Default.MruProjects.AddNewPath(path);
				Settings.Default.Save();
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		static void HandleProjectWindowClosed(object sender, EventArgs e)
		{
			_projectContext.Dispose();
			_projectContext = null;

			if (((Shell)sender).UserWantsToOpenADifferentProject)
			{
				Application.Idle += ChooseAnotherProject;
			}
			else
			{
				Application.Exit();
			}
		}
		public static void SetUpLocalization()
		{
			var installedStringFileFolder = FileLocator.GetDirectoryDistributedWithApplication("localization");
			installedStringFileFolder = Path.GetDirectoryName(installedStringFileFolder);

			LocalizationManager.Create(Settings.Default.UserInterfaceLanguage,
				"Bloom", "Bloom", Application.ProductVersion,
				installedStringFileFolder, Path.Combine(ProjectContext.GetBloomAppDataFolder(),"Localizations"), "Bloom");

			Settings.Default.UserInterfaceLanguage = LocalizationManager.UILanguageId;
		}



		/// ------------------------------------------------------------------------------------
		private static void SetUpErrorHandling()
		{
			Palaso.Reporting.ErrorReport.EmailAddress = "issues@bloom.palaso.org";
			Palaso.Reporting.ErrorReport.AddStandardProperties();
			Palaso.Reporting.ExceptionHandler.Init();
		}


		private static void SetUpReporting()
		{
			if (Settings.Default.Reporting == null)
			{
				Settings.Default.Reporting = new ReportingSettings();
				Settings.Default.Save();
			}
			UsageReporter.Init(Settings.Default.Reporting, "bloom.palaso.org", "UA-22170471-2",
#if DEBUG
				true
#else
				false
#endif
				);
		}

		public static void TimeBomb()
		{
			var asm = Assembly.GetExecutingAssembly();
			var file = asm.CodeBase.Replace("file:", string.Empty);
			file = file.TrimStart('/');
			var fi = new FileInfo(file);
			if(DateTime.UtcNow.Subtract(fi.LastWriteTimeUtc).Days > 90)// nb: "create time" is stuck at may 2011, for some reason. Arrrggghhhh
				//if (DateTime.UtcNow.Subtract(fi.CreationTimeUtc).Seconds > 100)
				{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(
					"Sorry, this beta version of Bloom is now over 90 days old.  Please get a new version at bloom.palaso.org");
					Process.GetCurrentProcess().Kill();
			}

		}
	}


}
