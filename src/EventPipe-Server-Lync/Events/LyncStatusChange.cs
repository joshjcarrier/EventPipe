namespace EventPipe.Server.Lync.Events
{
    public class LyncStatusChange
    {
        public string ContactUri { get; internal set; }
        public string Status { get; internal set; }

        public override string ToString()
        {
            return this.ContactUri + " (" + this.Status + ")";
        }
    }
}
