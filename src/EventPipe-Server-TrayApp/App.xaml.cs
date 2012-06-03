namespace EventPipe.Server.TrayApp
{
    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using EventPipe.Server.Lync;
    using MessageBox = System.Windows.MessageBox;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private NotifyIcon tray;

        public App()
        {
            DispatcherUnhandledException += this.OnDispatcherUnhandledException;

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            tray = new NotifyIcon();
            tray.Icon = new Icon(SystemIcons.Application, 40, 40);
            tray.Text = "EventPipe: COM3 probably";
            tray.ContextMenu = trayMenu;

            tray.Visible = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // TODO initialize and manage this, recieving events from an event aggregator
            new LyncPlugin();

            tray.ShowBalloonTip(1000, "EventPipe", "Lync integration: enabled", ToolTipIcon.Info);
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
