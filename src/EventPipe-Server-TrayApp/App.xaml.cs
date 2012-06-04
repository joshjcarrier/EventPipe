namespace EventPipe.Server.TrayApp
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using EventPipe.Server.EventMessaging;
    using EventPipe.Server.Lync;
    using EventPipe.Server.SerialPort;
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

        private void DebugRawPublishEventMessages(string payload)
        {
            Trace.WriteLine("RawPublishEventMessenger sending payload: " + payload);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.eventAggregator.GetEvent<RawPublishEventMessenger>().Subscribe(DebugRawPublishEventMessages);
            
            var window = InteractiveShellWindow.Create(this.eventAggregator);
            window.Show();

            // TODO initialize and manage this properly
            var summary = "Serial port integration: ";
            try
            {
                SerialPortPlugin.Create(this.eventAggregator);
                summary += "enabled";
            }
            catch (Exception ex)
            {
                summary += "disabled" + Environment.NewLine + "    " + ex.Message;
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                this.eventAggregator.GetEvent<TraceEventMessenger>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Serial port plugin disabled. " + ex.Message });
            }

            summary += Environment.NewLine;
            summary += "Lync integration: ";
            try
            {
                LyncPlugin.Create(this.eventAggregator);
                summary += "enabled";
            }
            catch (Exception ex)
            {
                summary += "disabled" + Environment.NewLine + "    " + ex.Message;
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                this.eventAggregator.GetEvent<TraceEventMessenger>().Publish(new TraceMessage { Owner = "SYSTEM", Message = "Lync integration disabled. " + ex.Message });
            }
            
            this.tray.ShowBalloonTip(500, "EventPipe", summary, ToolTipIcon.Info);
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
