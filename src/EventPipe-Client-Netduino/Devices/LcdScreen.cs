namespace EventPipe.Client.Netduino.Devices
{
    using System;
    using System.Collections;
    using System.Threading;
    using MicroLiquidCrystal;

    public class LcdScreen
    {
        private readonly Lcd lcd;
        private readonly Queue pageQueue;
        private readonly int columns;
        private readonly int rows;
        
        public LcdScreen(int columns, int rows, Lcd lcd)
        {
            this.lcd = lcd;
            this.columns = columns;
            this.rows = rows;
            this.pageQueue = new Queue();

            this.lcd.Begin((byte)columns, (byte)rows);
        }

        public bool HasWaitingPages
        {
            get { return this.pageQueue.Count > 0; }
        }

        public LcdPage CreatePage()
        {
            return new LcdPage(this.columns, this.rows);
        }

        public void PushPage(LcdPage page)
        {
            this.pageQueue.Enqueue(page);
        }

        public void WriteNextPage()
        {
            if (this.pageQueue.Count == 0)
            {
                return;
            }

            var page = (LcdPage)this.pageQueue.Dequeue();
            LcdHelper.Write20X4(this.lcd, page.ToString());
            if (page.MinimumWaitTime > 0)
            {
                Thread.Sleep(page.MinimumWaitTime);
            }
        }
    }

    public static class LcdHelper
    {
        private const string RowPadding = "                    ";  // 20 chars

        /// <summary>
        /// 20x4 LCDs wrap rows in order 0-&gt;2-&gt;1-&gt;3 due to its memory configuration. This compensates by modifiying the string to write.
        /// </summary>
        /// <param name="lcd">
        /// The lcd module.
        /// </param>
        /// <param name="text">
        /// The text to write to the LCD.
        /// </param>
        public static void Write20X4(Lcd lcd, string text)
        {
            lcd.Clear();
            lcd.Home();

            var memoryMappedText = text.Substring(0, Math.Min(text.Length, 20));
            if (text.Length > 40)
            {
                memoryMappedText += PadRight(text.Substring(40, Math.Min(text.Length - 40, 20)));
                memoryMappedText += PadRight(text.Substring(20, Math.Min(text.Length - 20, 20)));

                if (text.Length > 60)
                {
                    memoryMappedText += text.Substring(60, Math.Min(text.Length - 60, 20));
                }
            }
            else if (text.Length > 20)
            {
                memoryMappedText += RowPadding;
                memoryMappedText += PadRight(text.Substring(20, Math.Min(text.Length - 20, 20)));
            }

            lcd.Write(memoryMappedText);
        }

        private static string PadRight(string text)
        {
            return text + RowPadding.Substring(0, 20 - text.Length);
        }
    }
}
