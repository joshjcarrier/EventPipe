namespace EventPipe.Server.Lync
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO.Ports;
    using EventPipe.Common.Data;
    using Microsoft.Lync.Model;

    public class LyncPlugin
    {
        private static SerialPort serialPort;

        public LyncPlugin()
        {
            // TODO move serial port stuff out of this
            // TODO have this raise events to a central event aggregator
            serialPort = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
            serialPort.Open();

            for (int i = 0; i < ConfigurationManager.AppSettings.Count; i++)
            {
                var contact = ConfigurationManager.AppSettings["contact" + i];
                registerStatus(contact + "@micro" + "soft.com");
            }
        }

        static void registerStatus(string alias)
        {
            LyncClient.GetClient().ContactManager.BeginLookup(alias, lookupCOmplete, alias);
        }

        static void lookupCOmplete(IAsyncResult result)
        {
            var result2 = LyncClient.GetClient().ContactManager.EndLookup(result);

            var contact = result2 as Contact;
            // TODO cache instead of reading
            contact.ContactInformationChanged -= ContactContactInformationChanged;
            contact.ContactInformationChanged += ContactContactInformationChanged;

            ContactAvailability availability;
            Enum.TryParse(contact.GetContactInformation(ContactInformationType.Availability).ToString(), out availability);
            Trace.WriteLine(result.AsyncState + " - " + availability);

            switch (availability)
            {
                case ContactAvailability.Free:
                    serialPort.Write(StatusCode.Free.ToString());
                    break;
                case ContactAvailability.Busy:
                case ContactAvailability.BusyIdle:
                case ContactAvailability.DoNotDisturb:
                    serialPort.Write(StatusCode.Busy.ToString());
                    break;
                case ContactAvailability.Away:
                case ContactAvailability.FreeIdle:
                case ContactAvailability.Invalid:
                case ContactAvailability.None:
                case ContactAvailability.Offline:
                case ContactAvailability.TemporarilyAway:
                    serialPort.Write(StatusCode.Away.ToString());
                    break;
            }
        }

        private static void ContactContactInformationChanged(object sender, ContactInformationChangedEventArgs e)
        {
            foreach (var thing in e.ChangedContactInformation)
            {
                if (thing != ContactInformationType.Availability)
                {
                    return;
                }

                // repoll
                registerStatus(((Contact) sender).Uri);
            }
        }
    }
}
