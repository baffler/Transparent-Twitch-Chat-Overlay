using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Reflection;
using System.Resources;
using Microsoft.Shell;
using System.IO;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        public static bool IsShuttingDown { get; set; } = false;
        static bool allowMultipleInstances = false;

        [STAThread]
        public static void Main()
        {
            allowMultipleInstances = TransparentTwitchChatWPF.Properties.Settings.Default.allowMultipleInstances;

            if (allowMultipleInstances)
            {
                var application = new App();
                application.Init();
                application.Run();
            }
            else
            {
                if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
                {
                    var application = new App();
                    application.Init();
                    application.Run();

                    // Allow single instance code to perform cleanup operations
                    SingleInstance<App>.Cleanup();
                }
            }
        }

        public void Init()
        {
            this.InitializeComponent();
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            return ((MainWindow)MainWindow).ProcessCommandLineArgs(args);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            JumpList jumplist = new JumpList();

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Toggle Borders",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/toggleborders"
            });

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Show Settings",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/settings"
            });

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Reset Window",
                CustomCategory = "Actions",
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "/resetwindow"
            });

            jumplist.ShowFrequentCategory = false;
            jumplist.ShowRecentCategory = false;

            JumpList.SetJumpList(Application.Current, jumplist);

            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            string errorMessage = $"An unhandled exception occurred: {e.ExceptionObject.ToString()}";

            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {errorMessage}{Environment.NewLine}");
            }
            catch
            {
                // Ignore any exceptions thrown while logging the error
            }

            MessageBox.Show(errorMessage, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);

            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }
    }
}
