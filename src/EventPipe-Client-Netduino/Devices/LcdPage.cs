namespace EventPipe.Client.Netduino.Devices
{
    using System;

    public class LcdPage
    {
        private readonly string[] rowBuffers;
        private readonly int columns;

        public LcdPage(int columns, int rows)
        {
            this.columns = columns;

            this.rowBuffers = new string[rows];
        }

        public int MinimumWaitTime { get; set; }

        public void Write(int row, string text)
        {
            this.rowBuffers[row] = text; //.Substring(0, Math.Min(text.Length, this.columns - 1));
        }

        public override string ToString()
        {
            // TODO padding etc
            return this.rowBuffers[0] + this.rowBuffers[1] + this.rowBuffers[2] + this.rowBuffers[3];
        }
    }
}
