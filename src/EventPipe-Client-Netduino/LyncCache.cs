namespace EventPipe.Client.Netduino
{
    using EventPipe.Client.Netduino.Devices;
    using EventPipe.Client.Netduino.Drivers;
    using EventPipe.Common.Data;

    public class LyncCache
    {
        private string user;
        private string status = "?";

        public void QueueDisplayStatus(LcdScreen lcdScreen)
        {
            if (this.user == null)
            {
                return;
            }

            var page = lcdScreen.CreatePage();
            page.MinimumWaitTime = 3000;
            page.Write(1, this.user + " (" + this.status + ")");
            lcdScreen.PushPage(page);
        }

        public void ProcessPacket(SerialPacket packet, ShiftRegisterDriver.Session shiftRegisterSession)
        {
            this.user = packet.Data.Substring(2, packet.Data.Length - 2);

            switch (packet.Data[0])
            {
                case StatusCode.Away:
                    this.status = "away";

                    // first and third bit enabled
                    shiftRegisterSession.Write(5);
                    break;
                case StatusCode.Busy:
                    this.status = "busy";

                    // first and fourth bit enabled
                    shiftRegisterSession.Write(9);
                    break;
                case StatusCode.Free:
                    this.status = "free";

                    // first and second bit enabled
                    shiftRegisterSession.Write(3);
                    break;
            }
        }
    }
}
