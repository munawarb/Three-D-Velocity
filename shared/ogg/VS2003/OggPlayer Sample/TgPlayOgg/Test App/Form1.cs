//	Copyright (c) 2003-2005 TrayGames Corp. 
//	All rights reserved. Reproduction or transmission of this file, or a portion
//  thereof, is forbidden without prior written permission of TrayGames Corp.
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
using TG.Sound;

namespace TestApplication
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
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

            InitTestOfOggPlayer();
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
                if (oplay != null)
                    oplay.Dispose();
			}
			base.Dispose(disposing);
		}

		protected void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Determine if an Ogg file is still playing by checking the still-playing count.
			if (StillPlaying > 0)
			{
				// Display a MsgBox asking the user to save changes or abort.
				if (MessageBox.Show("Ogg files are still playing, are you sure you want to exit?", "TrayGames Ogg Player",
					MessageBoxButtons.YesNo) ==  DialogResult.No)
				{
					// Cancel the Closing event from closing the form.
					e.Cancel = true;

					// Wait for Ogg files to finish playing. . .
				}
				else
				{
					// Kill all outstanding playbacks
					while (PlayId > 0)
						oplay.StopOggFile(PlayId--);
	
					textBox1.Text = "";
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Form1));
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
			this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBox1.Location = new System.Drawing.Point(8, 8);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBox1.Size = new System.Drawing.Size(576, 320);
			this.textBox1.TabIndex = 0;
			this.textBox1.TabStop = false;
			this.textBox1.Text = "";
			// 
			// button1
			// 
			this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.button1.Location = new System.Drawing.Point(24, 344);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(128, 32);
			this.button1.TabIndex = 1;
			this.button1.Text = "Play Ogg File. . .";
			this.button1.Click += new System.EventHandler(this.Button1Click);
			// 
			// button2
			// 
			this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.button2.Location = new System.Drawing.Point(312, 344);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(120, 32);
			this.button2.TabIndex = 2;
			this.button2.Text = "Stop Last Ogg";
			this.button2.Click += new System.EventHandler(this.button2Click);
			// 
			// button3
			// 
			this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.button3.Location = new System.Drawing.Point(168, 344);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(128, 32);
			this.button3.TabIndex = 3;
			this.button3.Text = "Repeat Ogg";
			this.button3.Click += new System.EventHandler(this.button3Click);
			// 
			// button4
			// 
			this.button4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.button4.Location = new System.Drawing.Point(448, 344);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(120, 32);
			this.button4.TabIndex = 4;
			this.button4.Text = "Close";
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.AutoScroll = true;
			this.ClientSize = new System.Drawing.Size(592, 390);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "TrayGames Ogg Player";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

		static Form1 MainForm;
		static int StillPlaying;
        string SaveOggFileFolder;
		string OggName;
		int PlayId;
		OggPlay oplay;

        private void InitTestOfOggPlayer()
        {
            try
            {
                oplay = new OggPlay(this, OggSampleSize.SixteenBits);
                oplay.PlayOggFileResult += new PlayOggFileEventHandler(PlayOggFileResult);

                textBox1.Text = "Initialization of Ogg Play library successful.\r\n";
				StillPlaying = 0;
			}
            catch(Exception e)
            {
                textBox1.Text = "Initialization failed: " + e.Message + "\r\n";
            }
        }

        private static void PlayOggFileResult(object sender, PlayOggFileEventArgs e)
        {
			if (e.Success)
			{
				MainForm.textBox1.Text += "Finished playing ogg Id= " + e.PlayId + " successfully. "
					+ "HoleCount= " + e.ErrorHoleCount + ", BadLinkCount= " + e.ErrorBadLinkCount + ".\r\n";
			}
			else
			{
				MainForm.textBox1.Text += "Stopped playing ogg Id= " + e.PlayId + " due to failure: \r\n'" 
					+ e.ReasonForFailure + "'\r\n";
			}

			StillPlaying--;
        }

        private string GetOggFileNameToOpen()
        {
            OpenFileDialog ofd = new OpenFileDialog();

            // set dialog properties
            ofd.DefaultExt = "*.ogg";
            ofd.Filter = "Ogg file (*.ogg)|*.ogg";
            ofd.RestoreDirectory = true;
            ofd.InitialDirectory = SaveOggFileFolder;
            ofd.Title = "Open an Ogg File";

            // invoke the dialog
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                SaveOggFileFolder = Path.GetDirectoryName(ofd.FileNames[0]);

                return ofd.FileNames[0];
            }        

            return null;
        }

		private void Button1Click(object sender, System.EventArgs e)
		{
			OggName = GetOggFileNameToOpen();

			if (null != OggName)
			{
				// Demonstrate using the library with a memory stream
				using (FileStream fs = new FileStream(OggName, FileMode.Open, FileAccess.Read))
				{
					byte[] OggData = new byte[fs.Length];
					BinaryReader br = new BinaryReader(fs);
					br.Read(OggData, 0, (int)fs.Length);
					br.Close();
					oplay.PlayOggFile(OggData, ++PlayId);
				}
				
				textBox1.Text += "Playing '" + OggName + "' Id= " + PlayId.ToString() + "\r\n";
				StillPlaying++;
			}        
		}

		private void button2Click(object sender, System.EventArgs e)
		{
			if (PlayId > 0)
			{
				oplay.StopOggFile(PlayId--);
			}
		}

		private void button3Click(object sender, System.EventArgs e)
		{
			if (OggName != null)
			{
				// Demonstrate using the library with a file name
				oplay.PlayOggFile(OggName, ++PlayId);
				textBox1.Text += "Playing " + OggName + " Id= " + PlayId.ToString() + "\r\n";
				StillPlaying++;
			}        
		}

		private void button4_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	} 
} 
