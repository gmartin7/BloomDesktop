using Bloom.web.controllers;
using System;
using System.Threading;
using SIL.Reporting;
using System.Windows.Forms;

namespace Bloom
{
	// Review: Does this even need to be descended from SIL.Reporting.ExceptionHandler now?
	internal class FatalExceptionHandler : ExceptionHandler
	{
		internal static Control ControlOnUIThread { get; private set; }

		internal static bool InvokeRequired => !ControlOnUIThread.IsDisposed && ControlOnUIThread.InvokeRequired;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set exception handler. Needs to be done before we create splash screen (don't
		/// understand why, but otherwise some exceptions don't get caught).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FatalExceptionHandler()
		{

			// We need to create a control on the UI thread so that we have a control that we
			// can use to invoke the error reporting dialog on the correct thread.
			ControlOnUIThread = new Control();
			ControlOnUIThread.CreateControl();

			// Using Application.ThreadException rather than
			// AppDomain.CurrentDomain.UnhandledException has the advantage that the
			// program doesn't necessarily end - we can ignore the exception and continue.
			Application.ThreadException += HandleTopLevelError;

			// We also want to catch the UnhandledExceptions for all the cases that
			// ThreadException don't catch, e.g. in the startup.
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
			var _problemReportObject = ProblemReportApi(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches and displays a otherwise unhandled exception.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="e">Exception</param>
		/// <remarks>previously <c>AfApp::HandleTopLevelError</c></remarks>
		/// ------------------------------------------------------------------------------------
		protected void HandleTopLevelError(object sender, ThreadExceptionEventArgs e)
		{
			if (!GetShouldHandleException(sender, e.Exception))
				return;

			if (DisplayError(e.Exception))
			{
				//Are we inside a Application.Run() statement?
				if (Application.MessageLoop)
					Application.Exit();
				else
					Environment.Exit(1); //the 1 here is just non-zero
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches and displays otherwise unhandled exception, especially those that happen
		/// during startup of the application before we show our main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (!GetShouldHandleException(sender, e.ExceptionObject as Exception))
				return;

			var exception = e.ExceptionObject as Exception;
			DisplayError(exception ?? new ApplicationException("Got unknown exception"));
		}

		protected override bool ShowUI => false;

		protected override bool DisplayError(Exception exception)
		{
			ProblemReportApi.ShowProblemDialog(Form.ActiveForm, exception, "", "fatal");
			return true;
		}
	}
}
