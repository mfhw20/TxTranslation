﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unclassified.TxEditor.ViewModels;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
{
	public partial class SelectFileWindow : Window
	{
		#region Static constructor

		static SelectFileWindow()
		{
			ViewCommandManager.SetupMetadata<SelectFileWindow>();
		}

		#endregion Static constructor

		#region Constructor

		public SelectFileWindow()
		{
			InitializeComponent();
			this.HideIcon();
		}

		#endregion Constructor

		#region Control event handlers

		private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			OKButton.IsEnabled = FileList.SelectedItems.Count > 0;
		}

		private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs args)
		{
			if (!progressSpinner.IsVisible)
			{
				OKButton_Click(null, null);
			}
		}

		private void OKButton_Click(object sender, RoutedEventArgs args)
		{
			SelectFileViewModel vm = DataContext as SelectFileViewModel;
			string[] files = new string[FileList.SelectedItems.Count];
			for (int i = 0; i < FileList.SelectedItems.Count; i++)
			{
				files[i] = System.IO.Path.Combine(vm.BaseDir, FileList.SelectedItems[i] as string);
			}
			vm.SelectedFileNames = files;

			DialogResult = true;
			Close();
		}

		private void AllButton_Click(object sender, RoutedEventArgs args)
		{
			SelectFileViewModel vm = DataContext as SelectFileViewModel;
			string[] files = new string[FileList.Items.Count];
			for (int i = 0; i < FileList.Items.Count; i++)
			{
				files[i] = System.IO.Path.Combine(vm.BaseDir, FileList.Items[i] as string);
			}
			vm.SelectedFileNames = files;

			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs args)
		{
			DialogResult = false;
			Close();
		}

		#endregion Control event handlers

		#region View commands

		[ViewCommand]
		public void LoadSingleFile()
		{
			// Second check for only file
			if (FileList.Items.Count == 1)
			{
				SelectFileViewModel vm = DataContext as SelectFileViewModel;
				string[] files = new string[1];
				files[0] = System.IO.Path.Combine(vm.BaseDir, FileList.Items[0] as string);
				vm.SelectedFileNames = files;

				DialogResult = true;
				Close();
			}
		}

		#endregion View commands
	}
}
