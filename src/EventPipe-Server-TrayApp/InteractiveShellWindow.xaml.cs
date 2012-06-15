namespace EventPipe.Server.TrayApp
{
    using System;
    using System.Reflection;
    using System.Windows.Input;
    using EventPipe.Common;
    using EventPipe.Common.Events;

    /// <summary>
    /// Interaction logic for InteractiveShellWindow.xaml
    /// </summary>
    public partial class InteractiveShellWindow
    {
        private readonly RawPublishEvent publishEvent;
        private string previousInputText = "";

        public InteractiveShellWindow()
        {
            InitializeComponent();

            this.outputTextBox.Text = Assembly.GetEntryAssembly().GetName().Name + Environment.NewLine;
            this.rawInput.Focus();
        }

        public InteractiveShellWindow(RawPublishEvent publishEvent, TraceEvent traceEvent) 
            : this()
        {
            this.publishEvent = publishEvent;

            // TODO would want to make sure loaded doesn't get called multiple times
            this.Loaded += (obj, args) => traceEvent.Subscribe(this.PublishRawMessage, true);
        }

        public static InteractiveShellWindow Create(EventAggregator eventAggregator)
        {
            return new InteractiveShellWindow(eventAggregator.GetEvent<RawPublishEvent>(), eventAggregator.GetEvent<TraceEvent>());
        }
        
        private void PublishRawMessage(TraceMessage payload)
        {
            if (this.outputTextBox.Text.Length > 1000000)
            {
                this.outputTextBox.Clear();
            }

            this.outputTextBox.AppendText(payload + Environment.NewLine);
            this.outputScrollViewer.ScrollToBottom();
        }

        private void OnInputTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.rawInput.Text = string.Empty;
                this.rawInput.CaretIndex = this.rawInput.Text.Length;
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                this.rawInput.Text = this.previousInputText;
                this.rawInput.CaretIndex = this.rawInput.Text.Length;
                return;
            }

            if (e.Key == Key.Enter)
            {
                this.SendMessage();
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrEmpty(this.rawInput.Text))
            {
                return;
            }

            this.outputTextBox.Text += this.rawInput.Text + Environment.NewLine;
            this.publishEvent.Publish(this.rawInput.Text);
            this.previousInputText = this.rawInput.Text;

            this.rawInput.Clear();
            this.outputScrollViewer.ScrollToBottom();
        }
    }
}
