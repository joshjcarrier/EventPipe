namespace EventPipe.Common.Events.Lync
{
    public class LyncStatusChange
    {
        public string ContactUri { get; set; }
        public string Status { get; set; }

        public override string ToString()
        {
            return this.ContactUri + " (" + this.Status + ")";
        }
    }
}
