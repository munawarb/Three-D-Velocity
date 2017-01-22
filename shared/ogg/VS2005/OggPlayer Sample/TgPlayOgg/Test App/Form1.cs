//	Copyright (c) 2003-2005 TrayGames, LLC 
//	All rights reserved. Reproduction or transmission of this file, or a portion
//  thereof, is forbidden without prior written permission of TrayGames, LLC.
//
//	Author: Perry L. Marchant
//	Date:	June 2 2005
//	 
//	Files:	Form1.cs -- plays ogg files. in effect, tests both TgPlayOgg.dll and 
//			TgPlayOgg_vorbisfile.dll
// 
//	Notes:	This solution has a 'Reference' to where the TgPlayOgg.dll is. It sets
//			'Working Directory' to where TgPlayOgg_vorbisfile.dll is, so that it
//			can easily find this required DLL. Your final application should include
//			in its directory both TgPlayOgg.dll and TgPlayOgg_vorbisfile.dll libraries.

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Threading;
using TG.Sound;

namespace TestApplication
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
        private System.Windows.Forms.TextBox statusTextbox;
        private Button playButton;
		private Button stopButton;
		private Button repeatButton;
		private Button exitButton;
        private Button aboutButton;
        private Button helpButton;
        private PictureBox pictureBox1;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
            MainForm = this;
			MainForm.Closing += new CancelEventHandler(Form1_Closing);

            oggPlay = null;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				if (components != null) 
				{
					components.Dispose();
				}

                // my dispose
                if (oggPlay != null)
                    oggPlay.Dispose();
			}
			base.Dispose(disposing);
		}

        /// <summary>
        /// Determines if an Ogg file is still playing and gives the user to cancel
        /// the close application request or stop Ogg audio file playback.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The object containing the details about the event.</param>
		protected void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Determine if an Ogg Vorbis file is still playing by checking the still-playing count.
			if (StillPlaying > 0)
			{
				// Display a MsgBox asking the user to save changes or abort.
				if (MessageBox.Show("Ogg files are still playing, are you sure you want to exit?", 
                    "TrayGames Ogg Player",	MessageBoxButtons.YesNo) ==  DialogResult.No)
				{
					// Cancel the Closing event from closing the form.
					e.Cancel = true;

					// Wait for Ogg files to finish playing. . .
				}
				else
				{
					// Kill all outstanding playbacks
                    while (PlayId > 0)
                        oggPlay.StopOggFile(PlayId--);

                    // Give stopped Ogg Vorbis files a chance to finish
                    Thread.Sleep(2000);
                    
					statusTextbox.Text = "";
				}
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.statusTextbox = new System.Windows.Forms.TextBox();
            this.playButton = new System.Windows.Forms.Button();
            this.repeatButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.aboutButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // statusTextbox
            // 
            this.statusTextbox.BackColor = System.Drawing.SystemColors.Control;
            this.statusTextbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusTextbox.Location = new System.Drawing.Point(8, 8);
            this.statusTextbox.Multiline = true;
            this.statusTextbox.Name = "statusTextbox";
            this.statusTextbox.ReadOnly = true;
            this.statusTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.statusTextbox.Size = new System.Drawing.Size(463, 376);
            this.statusTextbox.TabIndex = 1;
            // 
            // playButton
            // 
            this.playButton.BackColor = System.Drawing.SystemColors.Control;
            this.playButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playButton.Location = new System.Drawing.Point(477, 12);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(119, 32);
            this.playButton.TabIndex = 2;
            this.playButton.Text = "&Play Ogg File...";
            this.playButton.UseVisualStyleBackColor = false;
            this.playButton.Click += new System.EventHandler(this.Button1Click);
            // 
            // repeatButton
            // 
            this.repeatButton.BackColor = System.Drawing.SystemColors.Control;
            this.repeatButton.Enabled = false;
            this.repeatButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.repeatButton.Location = new System.Drawing.Point(477, 50);
            this.repeatButton.Name = "repeatButton";
            this.repeatButton.Size = new System.Drawing.Size(119, 32);
            this.repeatButton.TabIndex = 3;
            this.repeatButton.Text = "&Repeat Ogg";
            this.repeatButton.UseVisualStyleBackColor = false;
            this.repeatButton.Click += new System.EventHandler(this.button3Click);
            // 
            // stopButton
            // 
            this.stopButton.BackColor = System.Drawing.SystemColors.Control;
            this.stopButton.Enabled = false;
            this.stopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stopButton.Location = new System.Drawing.Point(477, 88);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(120, 32);
            this.stopButton.TabIndex = 4;
            this.stopButton.Text = "&Stop Last Ogg";
            this.stopButton.UseVisualStyleBackColor = false;
            this.stopButton.Click += new System.EventHandler(this.button2Click);
            // 
            // aboutButton
            // 
            this.aboutButton.BackColor = System.Drawing.SystemColors.Control;
            this.aboutButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.aboutButton.Location = new System.Drawing.Point(477, 126);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(120, 32);
            this.aboutButton.TabIndex = 5;
            this.aboutButton.Text = "&About...";
            this.aboutButton.UseVisualStyleBackColor = false;
            this.aboutButton.Click += new System.EventHandler(this.button4_Click);
            // 
            // exitButton
            // 
            this.exitButton.BackColor = System.Drawing.SystemColors.Control;
            this.exitButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exitButton.Location = new System.Drawing.Point(477, 352);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(120, 32);
            this.exitButton.TabIndex = 6;
            this.exitButton.Text = "E&xit";
            this.exitButton.UseVisualStyleBackColor = false;
            this.exitButton.Click += new System.EventHandler(this.button5_Click);
            // 
            // helpButton
            // 
            this.helpButton.BackColor = System.Drawing.SystemColors.Control;
            this.helpButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.helpButton.Location = new System.Drawing.Point(477, 164);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(120, 32);
            this.helpButton.TabIndex = 7;
            this.helpButton.Text = "&Help";
            this.helpButton.UseVisualStyleBackColor = false;
            this.helpButton.Click += new System.EventHandler(this.button6_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::TestApplication.Properties.Resources.GenericWaterMark;
            this.pictureBox1.Location = new System.Drawing.Point(477, 233);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(119, 81);
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(221)))), ((int)(((byte)(235)))));
            this.ClientSize = new System.Drawing.Size(601, 390);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.repeatButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.statusTextbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "TrayGames Ogg Player";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            string[] LibraryNames = new string[]{"TgPlayOgg_vorbisfile.dll",
                                                "TgPlayOgg.dll"};
            for (int i = 0; i < 2; i++)
            {
                if (!File.Exists(LibraryNames[i]))
                {
                    MessageBox.Show("The player can't start because you are missing the required library '" + LibraryNames[i] + "'." +
                        "\r\nPlease copy the missing library file into the same folder as the Ogg Player application.",
                        "TrayGames Ogg Player", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

           Application.Run(new Form1());
        }

		static Form1 MainForm;
		static int StillPlaying;
        string SaveOggFileFolder;
		string OggName;
		int PlayId;
		OggPlayManager oggPlay;

        /// <summary>
        /// Construct a new OggPlay object, set playback result handler,
        /// and initialize the playing count.
        /// </summary>
        private void InitializeOggPlayer()
        {
            if (oggPlay != null)
                return; // OggPlayer object already initialized!

            try
            {
                oggPlay = new OggPlayManager(this, true, OggPlayManager.SampleSize.SixteenBits);
                oggPlay.PlayOggFileResult +=  new EventHandler<OggPlayEventArgs>(PlayOggFileResult);

                statusTextbox.Text = "Ogg Play library initialization successful.\r\n";
				StillPlaying = 0;
			}
            catch(Exception ex)
            {
                statusTextbox.Text = "Ogg Play library initialization failed: " + ex.Message + Environment.NewLine;
            }
        }

        /// <summary>
        /// Handle the playback event by displaying success or error status.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The object containing the details about the event.</param>
        private void PlayOggFileResult(object sender, OggPlayEventArgs e)
        {
            try
            {
                if (e.Success)
                {
                    SetItemText("Finished playing ogg Id= " + e.PlayId + " successfully. "
                        + "HoleCount= " + e.ErrorHoleCount + ", BadLinkCount= " + e.ErrorBadLinkCount + ".\r\n");
                }
                else
                {
                    SetItemText("Stopped playing ogg Id= " + e.PlayId + " due to failure: \r\n'"
                        + e.ReasonForFailure + "'\r\n");
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine("Form1.cs: " + ex.Message);
            }
            finally
            {
                StillPlaying--;
            }
        }

        /// <summary>
        /// Delegate used to update the text control in the application's form.
        /// </summary>
        /// <param name="text">Text to add to the text control.</param>
        delegate void SetItemCallback(string text);

        /// <summary>
        /// Updates the text control with the specified text.
        /// </summary>
        /// <remarks>
        /// Notice that the signature of this function is identical to the
        /// delegate defined above.  The call to <see cref="Invoke"/> allows data
        /// to be passed from one thread to another safely so that the GUI can be
        /// updated
        /// </remarks>
        /// <param name="text">Text to add to the text control.</param>
        private void SetItemText(string text)
        {
            if (MainForm.statusTextbox.InvokeRequired)
            {
                Debug.WriteLine("Form1.cs: " + "Using the control's invoke method to marshal the status text to the proper thread.");

                // Create a new instance of the delegate with this function 
                // as the target.
                SetItemCallback callback = new SetItemCallback(SetItemText);

                //  Invoke the callback passing the text.
                Invoke(callback, new object[] { text });
            }
            else
            {
                // This is reached only when the GUI thread is active, after 
                // the Invoke function is called.
                MainForm.statusTextbox.Text += text;
            }
        }

        /// <summary>
        /// Use the OpenFileDialog to select a new Ogg audio file to play.
        /// </summary>
        private string GetOggFileNameToOpen()
        {
            OpenFileDialog ofd = new OpenFileDialog();

            // Set dialog properties
            ofd.DefaultExt = "*.ogg";
            ofd.Filter = "Ogg file (*.ogg)|*.ogg";
            ofd.RestoreDirectory = true;
            ofd.InitialDirectory = SaveOggFileFolder;
            ofd.Title = "Open an Ogg File";

            try
            {
                // Invoke the dialog
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SaveOggFileFolder = Path.GetDirectoryName(ofd.FileNames[0]);
                    return ofd.FileNames[0];
                }
            }
            catch (ArgumentException ex)
            {
                statusTextbox.Text = "Ogg Vorbis file open failed: " + ex.Message + Environment.NewLine;
            }

            return null;
        }

        /// <summary>
        /// Opens an Ogg audio file and copies it into a byte buffer. This buffer
        /// is passed to the OggPlay library for decoding.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The object containing the details about the event.</param>
        private void Button1Click(object sender, System.EventArgs e)
		{
            InitializeOggPlayer();

            if (null != oggPlay)
            {
                // Don't assign directly to OggName member in case
                // the user decides to cancel.
                string TempName = GetOggFileNameToOpen();

                if (null != TempName)
                {
                    OggName = TempName;

                    try
                    {
                        // Demonstrate using the library with a memory stream
                        using (FileStream fs = new FileStream(OggName, FileMode.Open, FileAccess.Read))
                        {
                            byte[] OggData = new byte[fs.Length];
                            BinaryReader br = new BinaryReader(fs);
                            br.Read(OggData, 0, (int)fs.Length);
                            br.Close();
                            oggPlay.PlayOggFile(OggData, ++PlayId, 0, 0);
                        }
                    }
                    catch (IOException ex)
                    {
                        statusTextbox.Text = "Ogg Vorbis file playback failed: " + ex.Message + Environment.NewLine;
                    }

                    statusTextbox.Text += "Playing '" + OggName + "' Id= " + PlayId.ToString() + Environment.NewLine;
                    StillPlaying++;

                    this.repeatButton.Enabled = true;
                    this.stopButton.Enabled = true;
                }
            }
		}

        /// <summary>
        /// Immediately stop decoding of the current Ogg audio file.
        /// </summary>
        private void button2Click(object sender, System.EventArgs e)
		{
			if (PlayId > 0)
			{
				oggPlay.StopOggFile(PlayId--);
			}
		}

        /// <summary>
        /// Selects an Ogg audio file and passes its filename to the OggPlay 
        /// library for decoding.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The object containing the details about the event.</param>
        private void button3Click(object sender, System.EventArgs e)
		{
			if (OggName != null)
			{
				// Demonstrate using the library with a file name
				oggPlay.PlayOggFile(OggName, ++PlayId, 0, 0);
				statusTextbox.Text += "Playing " + OggName + " Id= " + 
                    PlayId.ToString() + Environment.NewLine;
				StillPlaying++;
			}        
		}

        /// <summary>
        /// Show the about box.
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            AboutBox About = new AboutBox();
            About.ShowDialog(this);
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void button5_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

        private void button6_Click(object sender, EventArgs e)
        {
            Process.Start("http://developer.traygames.com/Docs/?doc=OggLib");
        }
	} 
} 
