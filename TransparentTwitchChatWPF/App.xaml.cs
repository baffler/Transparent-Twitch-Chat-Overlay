using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Shell;
using TransparentTwitchChatWPF.Twitch;
using TransparentTwitchChatWPF.View.Settings;
using TwitchLib.EventSub.Websockets.Extensions;
using Velopack;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings = new AppSettings();
        public static bool IsShuttingDown { get; set; } = false;

        // The Host object that manages the application's services and lifetime
        public static IHost Host { get; private set; }

        public App()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        [STAThread]
        private static void Main(string[] args)
        {
            // Velopack needs to be able to bootstrap your application and handle updates
            VelopackApp.Build().Run();

            var application = new App();
            application.InitializeComponent(); // loads App.xaml resources
            application.Run(); // Triggers the OnStartup event
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            SimpleFileLogger.Log($"--- New App Instance Started. Args: {string.Join(" ", e.Args)} ---");

            // ALWAYS call the base method first
            base.OnStartup(e);

            Settings.Init();

            // --- SINGLE-INSTANCE LOGIC ---
            // First, check if we should enforce single-instance behavior.
            if (!Settings.GeneralSettings.AllowMultipleInstances)
            {
                SimpleFileLogger.Log("Single-instance mode is active.");

                SimpleFileLogger.Log("Attempting to start IPC server...");

                // Try to become the IPC server. If it fails, another instance is already running.
                if (!IpcManager.StartServer())
                {
                    SimpleFileLogger.Log("IPC server start failed. This must be a SECOND instance.");


                    // We are a subsequent instance.
                    // Show a message if the user just double-clicked the .exe
                    if (e.Args.Length == 0)
                    {
                        SimpleFileLogger.Log("No args detected, showing 'already running' message.");
                        MessageBox.Show("The application is already running. Check your system tray or taskbar.", "Application Running", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // Send arguments to the first instance so it can process them (e.g., from a JumpList).
                    await IpcManager.SendArgumentsToFirstInstance(e.Args);

                    SimpleFileLogger.Log("Shutdown command issued for this instance.");
                    // Immediately shut down this new instance.
                    Application.Current.Shutdown();
                    return; // IMPORTANT: Stop all further execution in this instance.
                }

                SimpleFileLogger.Log("IPC server started successfully. This must be the FIRST instance.");
                // If we've reached here, we are the FIRST instance.
                // Set up the listener for arguments from future instances.
                IpcManager.ArgumentsReceived += ProcessCommandLineArgsFromSecondInstance;
                // Create the JumpList, as we are the primary instance.
                CreateJumpList();
            }
            else
            {
                SimpleFileLogger.Log("Multi-instance mode is active. Skipping IPC checks.");
            }
            // If AllowMultipleInstances is true, we simply skip all the logic above.

            SimpleFileLogger.Log("Proceeding with full application startup.");

            // --- COMMON STARTUP LOGIC for all instances that are allowed to run ---

            // Hook the global unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Build the Dependency Injection Host
            Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>();
                
                services.AddTwitchLibEventSubWebsockets();
                services.AddSingleton<TwitchService>();
                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TwitchService>());
                services.AddSingleton<ITwitchAuthService, TwitchAuthService>();

                // Settings pages
                services.AddTransient<ConnectionSettingsPage>();
                services.AddTransient<ChatSettingsPage>();
                services.AddTransient<GeneralSettingsPage>();
                services.AddTransient<WidgetSettingsPage>();
                services.AddTransient<AboutSettingsPage>();

                // Main settings window
                services.AddTransient<SettingsWindow>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                // can add other providers here, like Serilog, NLog, or a file logger.
            })
            .Build();

            await Host.StartAsync();

            // Create and show the main window
            var mainWindow = Host.Services.GetRequiredService<MainWindow>();

            // Let the main window process its own startup arguments
            mainWindow.ProcessCommandLineArgs(e.Args);
            mainWindow.Show();
        }

        private void CreateJumpList()
        {
            // First, create a new empty list and set it. This clears all old items.
            JumpList.SetJumpList(Application.Current, new JumpList());

            JumpList jumplist = new JumpList();

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Toggle Borders",
                CustomCategory = "Actions",
                Arguments = "/toggleborders"
            });

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Show Settings",
                CustomCategory = "Actions",
                Arguments = "/settings"
            });

            jumplist.JumpItems.Add(new JumpTask
            {
                Title = "Reset Window",
                CustomCategory = "Actions",
                Arguments = "/resetwindow"
            });

            jumplist.ShowFrequentCategory = false;
            jumplist.ShowRecentCategory = false;

            JumpList.SetJumpList(Application.Current, jumplist);
        }

        private void ProcessCommandLineArgsFromSecondInstance(string[] args)
        {
            // Find the running MainWindow and ask it to process the arguments.
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.ProcessCommandLineArgs(args);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            IpcManager.StopServer();
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception occurred. Note you can click on this message box to focus it, then press Ctrl-C to copy the entire message.\n\n" + e.ExceptionObject.ToString(),
                "Click on this message box and press Ctrl-C to copy the entire message");
        }
    }
}
