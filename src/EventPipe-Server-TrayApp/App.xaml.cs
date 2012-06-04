namespace EventPipe.Server.TrayApp
{
    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using EventPipe.Common;
    using EventPipe.Server.EventTransformer;
    using MessageBox = System.Windows.MessageBox;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly EventAggregator eventAggregator;
        private readonly NotifyIcon tray;

        public App()
        {
            this.eventAggregator = new EventAggregator(Dispatcher);

            DispatcherUnhandledException += this.OnDispatcherUnhandledException;

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            this.tray = new NotifyIcon();
            this.tray.Icon = new Icon(SystemIcons.Application, 40, 40);
            this.tray.Text = "EventPipe";
            this.tray.ContextMenu = trayMenu;

            this.tray.Visible = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var window = InteractiveShellWindow.Create(this.eventAggregator);
            window.Show();

            NetduinoEventTransformer.Create(this.eventAggregator);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            tray.Visible = false;
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace, "What have you done?!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnExit(object sender, EventArgs e)
        {
            this.Shutdown();
        }
    }
}
