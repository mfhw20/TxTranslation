﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.TxEditor.View;
using Unclassified.TxEditor.ViewModel;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.TxEditor
{
	public partial class App : Application
	{
		#region Static properties

		/// <summary>
		/// Gets the Settings instance used by the application.
		/// </summary>
		public static AppSettings Settings { get; private set; }

		private static SplashScreen splashScreen;
		/// <summary>
		/// Gets the application splash screen.
		/// </summary>
		public static SplashScreen SplashScreen { get { return splashScreen; } }

		#endregion Static properties

		#region Application entry point

		/// <summary>
		/// Application entry point.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			// Set up FieldLog
			FL.AcceptLogFileBasePath();
			FL.RegisterPresentationTracing();
			TaskHelper.UnhandledTaskException = ex => FL.Critical(ex, "TaskHelper.UnhandledTaskException", true);

			// Keep the setup away
			GlobalMutex.Create("Unclassified.TxEditor");

			// Set the image file's build action to "Resource" and "Never copy" for this to work.
			splashScreen = new SplashScreen("Images/TxFlag_256.png");
			splashScreen.Show(false, true);

			App app = new App();
			app.InitializeComponent();
			app.Run();
		}

		#endregion Application entry point

		#region Constructors

		public App()
		{
			// Initialise the settings system
			Settings = new AppSettings(
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Unclassified",
				"TxTranslation",
				"TxEditor.conf"));

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			//ULog.SetDataBindingLog();

			// Setup logging
			//Tx.LogFileName = "tx.log";
			//Tx.LogFileName = "";
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", "1", EnvironmentVariableTarget.User);
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", null, EnvironmentVariableTarget.User);

			// Setup translation data
			try
			{
				// Set the XML file's build action to "Embedded Resource" and "Never copy" for this to work.
				Tx.LoadFromEmbeddedResource("Unclassified.TxEditor.Dictionary.txd");
			}
			catch (ArgumentException)
			{
				// The file was not embedded, try reading the file. This enables file change notifications.
				Tx.UseFileSystemWatcher = true;
				Tx.LoadFromXmlFile("Dictionary.txd");
			}

			string appCulture = Settings.AppCulture;
			if (!string.IsNullOrWhiteSpace(appCulture))
			{
				try
				{
					Tx.SetCulture(appCulture);
				}
				catch (Exception /*ex*/)
				{
					//MessageBox.Show("Error settings configured application UI culture.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					// Doing this leads to a XAML exception in the main window! o_O
				}
			}
		}

		#endregion Constructors

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Initialise and show the main window

			CommandLineHelper cmdLine = new CommandLineHelper();
			var scanOption = cmdLine.RegisterOption("scan", 1).Alias("s");

			try
			{
				cmdLine.Parse();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Command line error", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			List<string> filesToLoad = new List<string>();
			foreach (string fileNameArg in cmdLine.FreeArguments)
			{
				if (!string.IsNullOrWhiteSpace(fileNameArg))
				{
					// File name
					if (File.Exists(fileNameArg))
					{
						// File exists, open it
						string fileName = fileNameArg;
						if (!Path.IsPathRooted(fileName))
						{
							fileName = Path.GetFullPath(fileName);
						}
						filesToLoad.Add(fileName);
					}
					else if (Directory.Exists(fileNameArg))
					{
						// Directory specified, collect all files
						foreach (string fileName in Directory.GetFiles(fileNameArg, "*.txd"))
						{
							filesToLoad.Add(fileName);
						}
						if (filesToLoad.Count == 0)
						{
							// Nothing found, try older XML file names
							foreach (string fileName in Directory.GetFiles(fileNameArg, "*.xml"))
							{
								if (FileNameHelper.GetCulture(fileName) != null)
								{
									filesToLoad.Add(fileName);
								}
							}
						}
					}
					// Ignore anything else
				}
			}

			// Scan for other files near the selected files
			// (Currently only active if a single file is specified)
			//if (filesToLoad.Count == 1)
			//{
			//    foreach (string fileName in filesToLoad.Distinct().ToArray())
			//    {
			//        if (fileName.ToLowerInvariant().EndsWith(".txd") && File.Exists(fileName))
			//        {
			//            // Existing .txd file
			//            // Scan same directory for other .txd files
			//            string[] otherFiles = Directory.GetFiles(Path.GetDirectoryName(fileName), "*.txd");
			//            // otherFiles should contain fileName and may contain additional files
			//            if (otherFiles.Length > 1)
			//            {
			//                if (MessageBox.Show(
			//                    "Other Tx dictionary files are located in the same directory as the selected file. Should they also be loaded?",
			//                    "Load other files",
			//                    MessageBoxButton.YesNo,
			//                    MessageBoxImage.Question) == MessageBoxResult.Yes)
			//                {
			//                    // Duplicates will be removed later
			//                    filesToLoad.AddRange(otherFiles);
			//                }
			//            }
			//        }
			//    }
			//}
			// NOTE: Loading multiple txd files is not supported. (Only scan for more cultures of version 1 files. - Done)

			if (!FileNameHelper.FindOtherCultures(filesToLoad))
				Application.Current.Shutdown();

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();

			if (filesToLoad.Count == 0 && scanOption.IsSet)
			{
				viewModel.ScanDirectory = scanOption.Value;
			}

			view.DataContext = viewModel;

			// Load selected files
			if (filesToLoad.Count > 0)
			{
				viewModel.LoadFiles(filesToLoad);
			}

			// Show the main window
			view.Show();
		}

		#endregion Startup

		#region Event handlers

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </remarks>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			if (Settings != null)
			{
				Settings.Dispose();
				Settings = null;
			}
		}

		#endregion Event handlers
	}
}
