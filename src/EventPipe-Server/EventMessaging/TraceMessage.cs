namespace EventPipe.Server.EventMessaging
{
    public class TraceMessage
    {
        public string Owner { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return "[ " + this.Owner.PadRight(10, ' ') + " ] " + this.Message;
        }
    }
}
