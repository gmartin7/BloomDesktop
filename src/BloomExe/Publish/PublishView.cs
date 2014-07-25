﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Bloom.Book;
using Bloom.CollectionTab;
using Bloom.Edit;
using Bloom.WebLibraryIntegration;
using DesktopAnalytics;
using L10NSharp;
using Palaso.Reporting;
using Gecko;
using Palaso.IO;

namespace Bloom.Publish
{
	public partial class PublishView : UserControl, IBloomTabArea
	{
		private readonly PublishModel _model;
		private readonly ComposablePartCatalog _extensionCatalog;
		private bool _activated;
		private BloomLibraryPublishControl _publishControl;
		private BookTransfer _bookTransferrer;
		private LoginDialog _loginDialog;

		public delegate PublishView Factory();//autofac uses this

		public PublishView(PublishModel model,
			SelectedTabChangedEvent selectedTabChangedEvent, BookTransfer bookTransferrer, LoginDialog login)
		{
			_bookTransferrer = bookTransferrer;
			_loginDialog = login;

			InitializeComponent();
#if __MonoCS__ // Hide booklet options on Linux until we get a port of PdfDroplet
			tableLayoutPanel1.Controls.Remove(_bookletCoverRadio);
			tableLayoutPanel1.Controls.Remove(_bookletBodyRadio);
#endif

			if (this.DesignMode)
				return;

			_model = model;
			_model.View = this;

			_makePdfBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(_makePdfBackgroundWorker_RunWorkerCompleted);

			//NB: just triggering off "VisibilityChanged" was unreliable. So now we trigger
			//off the tab itself changing, either to us or away from us.
			selectedTabChangedEvent.Subscribe(c=>
												{
													if (c.To == this)
													{
														Activate();
													}
													else if (c.To!=this && IsMakingPdf)
														_makePdfBackgroundWorker.CancelAsync();
												});

			//TODO: find a way to call this just once, at the right time:

			//			DeskAnalytics.Track("Publish");

//#if DEBUG
//        	var linkLabel = new LinkLabel() {Text = "DEBUG"};
//			linkLabel.Click+=new EventHandler((x,y)=>_model.DebugCurrentPDFLayout());
//        	tableLayoutPanel1.Controls.Add(linkLabel);
//#endif

			_menusToolStrip.Renderer = new EditingView.FixedToolStripRenderer();
			GeckoPreferences.Default["pdfjs.disabled"] = false;
		}


		private void Activate()
		{
			_activated = false;

			Logger.WriteEvent("Entered Publish Tab");
			if (IsMakingPdf)
				return;


//			_model.BookletPortion = PublishModel.BookletPortions.BookletPages;


			_model.RefreshValuesUponActivation();

			//reload items from extension(s), as they may differ by book (e.g. if the extension comes from the template of the book)
			var toolStripItemCollection = new List<ToolStripItem>(from ToolStripItem x in _contextMenuStrip.Items select x);
			foreach (ToolStripItem item in toolStripItemCollection)
			{
				if (item.Tag == "extension")
					_contextMenuStrip.Items.Remove(item);
			}
			foreach (var item in _model.GetExtensionMenuItems())
			{
				item.Tag = "extension";
				_contextMenuStrip.Items.Add(item);
			}

			// We choose not to remember the last state this tab might have been in.
			// Also since we don't know if the pdf is out of date, we assume it is, and don't show the prior pdf.
			// SetModelFromButtons takes care of both of these things for the model
			_bookletCoverRadio.Checked = _bookletBodyRadio.Checked = _simpleAllPagesRadio.Checked = _uploadRadio.Checked = false;
			SetModelFromButtons();
			_model.DisplayMode = PublishModel.DisplayModes.WaitForUserToChooseSomething;

			UpdateDisplay();

			_activated = true;
		}

		internal bool IsMakingPdf
		{
			get { return _makePdfBackgroundWorker.IsBusy; }
		}


		public Control TopBarControl
		{
			get { return _topBarPanel; }
		}

		void _makePdfBackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			_model.PdfGenerationSucceeded = false;
			if (!e.Cancelled)
			{
				if (e.Result is Exception)
				{
					var error = e.Result as Exception;
					if (error is ApplicationException)
					{
						//For common exceptions, we catch them earlier (in the worker thread) and give a more helpful message
						//note, we don't want to include the original, as it leads to people sending in reports we don't
						//actually want to see. E.g., we don't want a bug report just because they didn't have Acrobat
						//installed, or they had the PDF open in Word, or something like that.
						ErrorReport.NotifyUserOfProblem(error.Message);
					}
					else // for others, just give a generic message and include the original exception in the message
					{
						ErrorReport.NotifyUserOfProblem(error, "Sorry, Bloom had a problem creating the PDF.");
					}
					// We CAN upload even without a preview.
					_model.DisplayMode = (_uploadRadio.Checked
						? PublishModel.DisplayModes.Upload
						: PublishModel.DisplayModes.WaitForUserToChooseSomething);
					UpdateDisplay();
					return;
				}
				_model.PdfGenerationSucceeded = true;
					// should be the only place this is set, when we generated successfully.
				if (IsHandleCreated) // May not be when bulk uploading
				{
					_model.DisplayMode = (_uploadRadio.Checked
						? PublishModel.DisplayModes.Upload
						: PublishModel.DisplayModes.ShowPdf);
					Invoke((Action) (UpdateDisplay));
				}
			}
			if(e.Cancelled || _model.BookletPortion != (PublishModel.BookletPortions) e.Result )
			{
				MakeBooklet();
			}
		}



		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
		}

		private void UpdateDisplay()
		{
			if (_model == null || _model.BookSelection.CurrentSelection==null)
				return;

			_layoutChoices.Text = _model.PageLayout.ToString();

			_bookletCoverRadio.Checked = _model.BookletPortion == PublishModel.BookletPortions.BookletCover && !_model.UploadMode;
			_bookletBodyRadio.Checked = _model.BookletPortion == PublishModel.BookletPortions.BookletPages && !_model.UploadMode;
			_simpleAllPagesRadio.Checked = _model.BookletPortion == PublishModel.BookletPortions.AllPagesNoBooklet && !_model.UploadMode;
			_uploadRadio.Checked = _model.UploadMode;

			if (!_model.AllowUpload)
			{
			   //this doesn't actually show when disabled		        _superToolTip.GetSuperStuff(_uploadRadio).SuperToolTipInfo.BodyText = "This creator of this book, or its template, has marked it as not being appropriate for upload to BloomLibrary.org";
			}
			_uploadRadio.Enabled = _model.AllowUpload;
			_bookletBodyRadio.Enabled = _model.ShowBookletOption;
			_bookletCoverRadio.Enabled = _model.ShowCoverOption;

			// No reason to update from model...we only change the model when the user changes the check box,
			// or when uploading...and we do NOT want to update the check box when uploading temporarily changes the model.
			//_showCropMarks.Checked = _model.ShowCropMarks;

			var layoutChoices = _model.BookSelection.CurrentSelection.GetLayoutChoices();
			_layoutChoices.DropDownItems.Clear();
//			_layoutChoices.Items.AddRange(layoutChoices.ToArray());
//			_layoutChoices.SelectedText = _model.BookSelection.CurrentSelection.GetLayout().ToString();
			foreach (var l in layoutChoices)
			{
				ToolStripMenuItem item = (ToolStripMenuItem)_layoutChoices.DropDownItems.Add(l.ToString());
				item.Tag = l;
				item.Text = l.ToString();
				item.Checked = l.ToString() == _model.PageLayout.ToString();
				item.CheckOnClick = true;
				item.Click += new EventHandler(OnLayoutChosen);
			}
			_layoutChoices.Text = _model.PageLayout.ToString();
		}

		private void OnLayoutChosen(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			_model.PageLayout = ((Layout)item.Tag);
			_layoutChoices.Text = _model.PageLayout.ToString();
			ControlsChanged();
		}

		public void SetDisplayMode(PublishModel.DisplayModes displayMode)
		{
			if (displayMode != PublishModel.DisplayModes.Upload && _publishControl != null)
			{
				Controls.Remove(_publishControl);
				_publishControl = null;
				_pdfViewer.Visible = true;
			}
			switch (displayMode)
			{
				case PublishModel.DisplayModes.WaitForUserToChooseSomething:
					_printButton.Enabled = _saveButton.Enabled = false;
					Cursor = Cursors.Default;
					_workingIndicator.Visible = false;
					_pdfViewer.Visible = false;
					break;
				case PublishModel.DisplayModes.Working:
					_printButton.Enabled = _saveButton.Enabled = false;
					_workingIndicator.Cursor = Cursors.WaitCursor;
					Cursor = Cursors.WaitCursor;
					_workingIndicator.Visible = true;
					break;
				case PublishModel.DisplayModes.ShowPdf:
					if (File.Exists(_model.PdfFilePath))
					{
						_pdfViewer.Visible = true;
						_workingIndicator.Visible = false;
						Cursor = Cursors.Default;
						_saveButton.Enabled = true;
						_printButton.Enabled = _pdfViewer.ShowPdf(_model.PdfFilePath);
					}
					break;
				case PublishModel.DisplayModes.Upload:
				{
					_workingIndicator.Visible = false; // If we haven't finished creating the PDF, we will indicate that in the progress window.
					_saveButton.Enabled = _printButton.Enabled = false; // Can't print or save in this mode...wouldn't be obvious what would be saved.
					_pdfViewer.Visible = false;
					Cursor = Cursors.Default;

					if (_publishControl == null)
					{
						SetupPublishControl();
					}

					break;
				}
			}
		}

		private void SetupPublishControl()
		{
			if (_publishControl != null)
			{
				//we currently rebuild it to update contents, as currently the constructor is where setup logic happens (we could change that)
				Controls.Remove(_publishControl); ;
			}

			_publishControl = new BloomLibraryPublishControl(this, _bookTransferrer, _loginDialog,
				_model.BookSelection.CurrentSelection);
			_publishControl.SetBounds(_pdfViewer.Left, _pdfViewer.Top,
				_pdfViewer.Width, _pdfViewer.Height);
			_publishControl.Dock = _pdfViewer.Dock;
			_publishControl.Anchor = _pdfViewer.Anchor;
			var saveBackColor = _publishControl.BackColor;
			Controls.Add(_publishControl); // somehow this changes the backcolor
			_publishControl.BackColor = saveBackColor; // Need a normal back color for this so links and text can be seen
			// Typically this control is dock.fill. It has to be in front of tableLayoutPanel1 (which is Left) for Fill to work.
			_publishControl.BringToFront();
		}

		private void OnBookletRadioChanged(object sender, EventArgs e)
		{
			if (!_activated)
				return;

			var oldPortion = _model.BookletPortion;
			var oldCrop = _model.ShowCropMarks; // changing to or from cloud radio CAN change this.
			SetModelFromButtons();
			if (oldPortion == _model.BookletPortion && oldCrop == _model.ShowCropMarks)
			{
				// no changes detected
				if (_uploadRadio.Checked)
				{
					_model.DisplayMode = PublishModel.DisplayModes.Upload;
				}
				else if (_model.DisplayMode == PublishModel.DisplayModes.Upload)
				{
					// no change because the PREVIOUS button was the cloud one. Need to restore the appropriate
					// non-cloud display
					_model.DisplayMode = _model.PdfGenerationSucceeded
						? PublishModel.DisplayModes.ShowPdf
						: PublishModel.DisplayModes.WaitForUserToChooseSomething;
				}
				else if (_model.DisplayMode == PublishModel.DisplayModes.WaitForUserToChooseSomething)
				{
					// This happens if user went directly to Upload and then chooses Simple layout
					// We haven't actually built a pdf yet, so do it.
					ControlsChanged();
				}
				return;
			}

			ControlsChanged();
		}

		private void OnShowCropMarks_CheckedChanged(object sender, EventArgs e)
		{
			if (!_activated)
				return;

			var oldSetting = _model.ShowCropMarks;
			SetModelFromButtons();
			if (oldSetting == _model.ShowCropMarks)
				return; // no changes detected

			ControlsChanged();
		}

		private void ControlsChanged()
		{
			if (IsMakingPdf || _model.BookletPortion == PublishModel.BookletPortions.None)
			{
				_makePdfBackgroundWorker.CancelAsync();
				UpdateDisplay(); // We may need to uncheck a layout item here
			}
			else
				MakeBooklet();
		}

		private void SetModelFromButtons()
		{
			if (_bookletCoverRadio.Checked)
				_model.BookletPortion = PublishModel.BookletPortions.BookletCover;
			else if (_bookletBodyRadio.Checked)
				_model.BookletPortion = PublishModel.BookletPortions.BookletPages;
			// The version we want to upload for web previews is the one that is shown for
			// the _simpleAllPagesRadio button, so pick AllPagesNoBooklet for both of these.
			else if (_simpleAllPagesRadio.Checked || _uploadRadio.Checked)
				_model.BookletPortion = PublishModel.BookletPortions.AllPagesNoBooklet;
			// otherwise, we don't yet know what version to show, so we don't show one.
			else
				_model.BookletPortion = PublishModel.BookletPortions.None;
			_model.UploadMode = _uploadRadio.Checked;
			_model.ShowCropMarks = _showCropMarks.Checked && !_uploadRadio.Checked; // don't want crop-marks on upload PDF
		}

		internal string PdfPreviewPath { get { return _model.PdfFilePath; } }

		public void MakeBooklet()
		{
			if (IsMakingPdf)
			{
				// Can't start again until this one finishes
				return;
			}
			_model.PdfGenerationSucceeded = false; // and so it stays unless we generate it successfully.
			if (_uploadRadio.Checked)
			{
				// We aren't going to display it, so don't bother generating it unless the user actually uploads.
				// Unfortunately, the completion of the generation process is normally responsible for putting us into
				// the right display mode for what we generated (or failed to), after this routine puts us into the
				// mode that shows generation is pending. For the upload button case, we want to go straight to the Upload
				// mode, so the upload control appears. This is a bizarre place to do it, but I can't find a better one.
				SetDisplayMode(PublishModel.DisplayModes.Upload);
				return;
			}

			SetDisplayMode(PublishModel.DisplayModes.Working);
			_makePdfBackgroundWorker.RunWorkerAsync();
		}

		private void OnPrint_Click(object sender, EventArgs e)
		{
			_pdfViewer.Print();
			Logger.WriteEvent("Calling Print on PDF Viewer");
			Analytics.Track("Print PDF");
		}

		private void _makePdfBackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			e.Result = _model.BookletPortion; //record what our parameters were, so that if the user changes the request and we cancel, we can detect that we need to re-run
			_model.LoadBook(sender as BackgroundWorker, e);
		}

		private void OnSave_Click(object sender, EventArgs e)
		{
			_model.Save();
		}
		public string HelpTopicUrl
		{
			get { return "/Tasks/Publish_tasks/Publish_tasks_overview.htm"; }
		}

		private void _openinBrowserMenuItem_Click(object sender, EventArgs e)
		{
			_model.DebugCurrentPDFLayout();
		}

		private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
		{

		}

		/// <summary>
		/// Make the preview required for publishing the book.
		/// </summary>
		internal void MakePublishPreview()
		{
			if (IsMakingPdf)
			{
				// Can't start another until current attempt finishes.
				_makePdfBackgroundWorker.CancelAsync();
				while (IsMakingPdf)
					Thread.Sleep(100);
			}
			// Usually these will have been set by SetModelFromButtons, but the publish button might already be showing when we go to this page.
			_model.ShowCropMarks = false; // don't want in online preview
			_model.BookletPortion = PublishModel.BookletPortions.AllPagesNoBooklet; // has all the pages and cover in form suitable for online use
			_makePdfBackgroundWorker.RunWorkerAsync();
			// We normally generate PDFs in the background, but this routine should not return until we actually have one.
			while (IsMakingPdf)
			{
				Thread.Sleep(100);
				Application.DoEvents(); // Wish we didn't need this, but without it bulk upload freezes making 'preview' which is really the PDF to upload.
			}
		}
	}
}
