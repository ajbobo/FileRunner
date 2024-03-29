﻿using System;
using System.Windows.Forms;
using System.IO;
using folderSelect;
//using HundredMilesSoftware.UltraID3Lib;

namespace FileRunner
{
	public delegate void FileFunction(FileInfo curfile, int index);
	public delegate void DirectoryFunction(DirectoryInfo curdir);
	
	public partial class MainGUI : Form
	{
		public MainGUI()
		{
			InitializeComponent();

			// Put the possible operations in the combo box
			cboOperation.Items.Add(new Operation("Show file name", ShowDirectoryName, ShowFileName));
			cboOperation.Items.Add(new Operation("Show Directory name only", ShowDirectoryName, null));
			//cboOperation.Items.Add(new Operation("Set mp3 track number", ShowDirectoryName, SetMp3Track));
			cboOperation.Items.Add(new Operation("Delete Flexsim backup files (.*!)", ShowDirectoryName, DeleteFlexsimBackupFile));
			cboOperation.Items.Add(new Operation("Delete CVS backup files (.# files)", ShowDirectoryName, DeleteCVSBackUpFile));
			cboOperation.Items.Add(new Operation("Delete CVS folders", DeleteCVSDirectory, ShowFileName));
			cboOperation.Items.Add(new Operation("Delete all CVS remnants (files & folders)", DeleteCVSDirectory, DeleteCVSBackUpFile));
            cboOperation.Items.Add(new Operation("Rename mp4 to m4v", ShowDirectoryName, RenameMP4));
			cboOperation.Items.Add(new Operation("Delete bin and obj directories", DeleteBinObjDirectories, null));
			cboOperation.Items.Add(new Operation("Delete files less than 1 day old", ShowDirectoryName, DeleteRecentFile));
			// Pick a default operation
			cboOperation.SelectedIndex = 0;
		}

        private void txtPath_DragDrop(object sender, DragEventArgs e)
		{
			// Copies the name of the file that's being dragged into the control
			string[] strdata = (string[])e.Data.GetData(DataFormats.FileDrop);
			txtPath.Text = strdata[0];
		}

		private void txtPath_DragEnter(object sender, DragEventArgs e)
		{
			// Checks to see if a file is being dragged over the control - files will be accepted, other formats won't be
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				if ((e.AllowedEffect & DragDropEffects.Copy) != 0)
				{
					e.Effect = DragDropEffects.Copy;
				}
			}
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			FolderSelect dlg = new FolderSelect();

			dlg.ShowDialog();

			txtPath.Text = dlg.fullPath;
		}

		private void btnDoIt_Click(object sender, EventArgs e)
		{
			txtResults.Clear();

			DoDirectory(txtPath.Text);
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			txtResults.Clear();
		}

		private void DoDirectory(string directory)
		{
			// Find the Operation to peform on this directory/file
			Operation op = (Operation)cboOperation.SelectedItem;
			if (op == null)
			{
				txtResults.Text = "No Operation Selected";
				return;
			}

			// Find the current directory - perform the selected operation on it
			DirectoryInfo topdir = new DirectoryInfo(directory);
			if (!topdir.Exists)
			{
				MessageBox.Show(directory, "Directory doesn't exist");
				return;
			}
			try
			{
				op.OperatateOnDirectory(topdir);
			}
			catch (Exception ex)
            {
				txtResults.Text += "\r\nEXCEPTION: " + ex.Message;
				Console.WriteLine(ex.Message);
			}
			txtResults.Text += "\r\n";
			UpdateOutput();

			// Look through the files - perform the selected operation on them
			try
			{
				if (op.hasFileOperation())
				{
					FileInfo[] files = topdir.GetFiles();
					int index = 0;
					foreach (FileInfo curfile in files)
					{
						bool issystem = HasAttribute(curfile.Attributes, FileAttributes.System);
						bool ishidden = HasAttribute(curfile.Attributes, FileAttributes.Hidden);
						bool showit = false;
						if (!issystem && !ishidden) // Show normal files
							showit = true;
						else if (issystem && btnShowSystem.Checked) // If system files are supposed to be visible, show them
							showit = true;
						else if (ishidden && !issystem && btnShowHidden.Checked) // The "Show Hidden" checkbox won't show system files
							showit = true;

						if (showit)
						{
							try
							{
								op.OperatateOnFile(curfile, index);
							}
							catch (Exception ex)
							{
								txtResults.Text += "\r\nEXCEPTION: " + ex.Message;
								Console.WriteLine(ex.Message);
							}
							txtResults.Text += "\r\n";
							UpdateOutput();
							index++;
						}
					}
				}

				// Recurse through the subdirectories
				if (btnRecurse.Checked)
				{
					DirectoryInfo[] dirs = topdir.GetDirectories();
					foreach (DirectoryInfo curdir in dirs)
					{
						bool isVCS = (curdir.Name == ".vs" || curdir.Name == ".git");

						if (!isVCS || !btnIgnoreVCS.Checked)
							DoDirectory(curdir.FullName);
					}
				}
			}
			catch (DirectoryNotFoundException ex)
			{
				// This happens if the directory was deleted
				string dummy = ex.Message;
			}
		}

		private void UpdateOutput()
		{
			if (btnAutoScroll.Checked)
			{
				txtResults.SelectionStart = txtResults.Text.Length;
				txtResults.ScrollToCaret();
			}

			if (txtResults.Text.Length > 10000)
				txtResults.Clear();
		}

		bool HasAttribute(FileAttributes value, FileAttributes check)
		{
			if ((value & check) == check)
				return true;

			return false;
		}

		/****************************************************
		 * Functions that can be put in the Operations list *
		 ****************************************************/
		private void ShowDirectoryName(DirectoryInfo curdir)
		{
			txtResults.Text += "Directory: " + curdir.FullName;
		}

		private void ShowFileName(FileInfo curfile, int index)
		{
			txtResults.Text += "  " + index + " - " + curfile.Name;
			if (HasAttribute(curfile.Attributes,FileAttributes.System))
				txtResults.Text += " - System";
			if (HasAttribute(curfile.Attributes, FileAttributes.Hidden))
				txtResults.Text += " - Hidden";
		}

		//private void SetMp3Track(FileInfo curfile, int index)
		//{
		//	ShowFileName(curfile, index);

		//	// Set the new track number for the file
		//	UltraID3 ultraid3 = new UltraID3();
		//	int trackcnt = index + 1;
		//	try
		//	{
		//		ultraid3.Read(curfile.FullName);
		//		int oldnum = (int)ultraid3.TrackNum; // Get the old track number
		//		txtResults.Text += "\tOldTrack: " + oldnum + "  NewTrack: " + trackcnt;
		//		ultraid3.TrackNum = (short)trackcnt; // Set the new track number
		//		ultraid3.Write(); // Write the changes to the file
		//	}
		//	catch (Exception ex)
		//	{
		//		txtResults.Text += "    ************* EXCEPTION *************\r\n    ";
		//		txtResults.Text += ex.Message;
		//	}
		//}

		private void DeleteFlexsimBackupFile(FileInfo curfile, int index)
		{
			ShowFileName(curfile, index);

			string filename = curfile.Name;
			if (filename[filename.Length - 1] == '!') // It's a backup file
			{
				txtResults.Text += " -> Backup file DELETED #####";
				curfile.Delete();
			}
		}

		private void DeleteCVSBackUpFile(FileInfo curfile, int index)
		{
			ShowFileName(curfile, index);

			string filename = curfile.Name;
			if (filename.StartsWith(".#")) // It's a CVS backup file
			{
				txtResults.Text += " -> Backup file DELETED #####";
				curfile.Delete();
			}
		}

		private void DeleteCVSDirectory(DirectoryInfo curdir)
		{
			ShowDirectoryName(curdir);

			string dirname = curdir.Name;
			if (dirname == "CVS")
			{
				txtResults.Text += " -> Directory DELETED #####";
				curdir.Delete(true); // Deletes sub-directories and files
			}
		}

		private void RenameMP4(FileInfo curfile, int index)
		{
			ShowFileName(curfile, index);

			if (curfile.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
			{
				curfile.MoveTo(curfile.FullName.Replace("mp4", "m4v")); // Needs to strip the old extension and keep the old directory

				txtResults.Text += " -> renamed to " + curfile.FullName;
			}
		}

		private void DeleteBinObjDirectories(DirectoryInfo curdir)
        {
			ShowDirectoryName(curdir);

			string dirname = curdir.Name;
			if (dirname == "bin" || dirname == "obj")
            {
				txtResults.Text += " -> Directory DELETED #####";
				curdir.Delete(true); // Delete sub-directories and files
            }
		}

		private void DeleteRecentFile(FileInfo curfile, int index)
		{
			ShowFileName(curfile, index);

			DateTime creationTime = curfile.CreationTime;

			if (DateTime.Now.Subtract(creationTime) < TimeSpan.FromHours(24) )
            {
				txtResults.Text += " -> Recent file deleted";
				curfile.Delete();
            }
		}
	}

	/*********************************************************************
	 * Represents an operation that can be performed on a file/directory *
	 *********************************************************************/
	public class Operation
	{
		private string name;
		private FileFunction filefunc;
		private DirectoryFunction dirfunc;

		public Operation(string name, DirectoryFunction dirfunc, FileFunction filefunc)
		{
			this.name = name;
			this.filefunc = filefunc;
			this.dirfunc = dirfunc;
		}

		public override string ToString()
		{
			return name;
		}

		public void OperatateOnFile(FileInfo curfile, int index)
		{
			if (filefunc != null)
				filefunc(curfile, index);
		}

		public void OperatateOnDirectory(DirectoryInfo curdir)
		{
			if (dirfunc != null)
				dirfunc(curdir);
		}

		public bool hasFileOperation()
        {
			return filefunc != null;
        }

		public bool hasDirOperation()
        {
			return dirfunc != null;
        }
	}
}
