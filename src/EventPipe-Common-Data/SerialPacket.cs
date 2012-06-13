namespace EventPipe.Common.Data
{
    public enum PacketDataType
    {
        Error,
        Lync = 'L',
        Text = 'T',
    }

    public class SerialPacket
    {
        public SerialPacket(string payload)
        {
            if (payload.Length < 2)
            {
                this.DataType = PacketDataType.Error;
                return;
            }

            this.DataType = (PacketDataType)payload[0];
            this.Data = payload.Substring(2, payload.Length - 2);
        }

        public string Data { get; private set; }

        public PacketDataType DataType { get; private set; }
    }
}
