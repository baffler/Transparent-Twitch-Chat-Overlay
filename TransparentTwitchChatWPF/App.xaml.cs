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

        protected override async void OnStartup(StartupEventArgs e) {
            try {
                SimpleFileLogger.Log($"--- New App Instance Started. Args: {string.Join(" ", e.Args)} ---");
                base.OnStartup(e);

                Settings.Init();

                // Try to become the IPC server. If it fails, another instance is already running.
                if (!IpcManager.StartServer())
                {
                    // We are another instance. Always send arguments to the first instance.
                    await IpcManager.SendArgumentsToFirstInstance(e.Args);

                    // If it was a jump list action OR single-instance mode is on, we are done. Exit now.
                    if (e.Args.Length > 0 || !Settings.GeneralSettings.AllowMultipleInstances)
                    {
                        SimpleFileLogger.Log("Shutdown command issued for this instance.");
                        // Immediately shut down this new instance.
                        Application.Current.Shutdown();
                        return;
                    }
        
                    // If we get here, it means:
                    // 1. We are another instance.
                    // 2. It was NOT a jump list action.
                    // 3. Multi-instance IS allowed.
                    // Therefore, we can proceed to launch a new full instance.
                }

                // Every full instance that runs should set up the jump list and the IPC listener.
                // This ensures that even if the first instance is closed, the remaining ones still
                // have correctly configured jump lists.
                CreateJumpList();
                IpcManager.ArgumentsReceived += ProcessCommandLineArgsFromSecondInstance;
            
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
            catch (Exception ex) {
                var msg = "An exception occurred On Startup. Note you can click on this message box to focus it, then press Ctrl-C to copy the entire message.\n\n" + ex.Message.ToString();
                if (ex.InnerException != null)
                {
                    msg += "\n\n" + ex.InnerException.ToString();
                }
                if (ex.StackTrace != null)
                {
                    msg += "\n\n" + ex.StackTrace.ToString();
                }

                MessageBox.Show(msg,
                    "Click on this message box and press Ctrl-C to copy the entire message");
                Application.Current.Shutdown();
                return;
            }
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
            Application.Current.Shutdown();
            return;
        }
    }
}
