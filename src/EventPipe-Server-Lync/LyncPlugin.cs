namespace EventPipe.Server.Lync
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using EventPipe.Common.Data;
    using EventPipe.Server.EventMessaging;
    using Microsoft.Lync.Model;

    public class LyncPlugin
    {
        private readonly RawPublishEventMessenger publishEventMessenger;
        private readonly TraceEventMessenger traceEventMessenger;

        public LyncPlugin(RawPublishEventMessenger publishEventMessenger, TraceEventMessenger traceEventMessenger)
        {
            this.publishEventMessenger = publishEventMessenger;
            this.traceEventMessenger = traceEventMessenger;
            
            foreach (var contactConfig in ConfigurationManager.AppSettings.AllKeys.Where(p => p.StartsWith("contact", StringComparison.OrdinalIgnoreCase)))
            {
                var contact = ConfigurationManager.AppSettings[contactConfig];
                var contactAddress = contact + "@micro" + "soft.com";
                registerStatus(contactAddress);
                traceEventMessenger.Publish(new TraceMessage { Owner = "Lync", Message = "Contact registered: " + contactAddress });
            }

            this.traceEventMessenger.Publish(new TraceMessage { Owner = "Lync", Message = "Ready" });
        }
        
        public static LyncPlugin Create(EventAggregator eventAggregator)
        {
            var publishEventMessenger = eventAggregator.GetEvent<RawPublishEventMessenger>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEventMessenger>();
            return new LyncPlugin(publishEventMessenger, traceEventMessenger);
        }

        void registerStatus(string alias)
        {
            LyncClient.GetClient().ContactManager.BeginLookup(alias, lookupCOmplete, alias);
        }

        void lookupCOmplete(IAsyncResult result)
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
                    this.traceEventMessenger.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now free" });
                    this.publishEventMessenger.Publish(StatusCode.Free.ToString());
                    break;
                case ContactAvailability.Busy:
                case ContactAvailability.BusyIdle:
                case ContactAvailability.DoNotDisturb:
                    this.traceEventMessenger.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now busy" });
                    this.publishEventMessenger.Publish(StatusCode.Busy.ToString());
                    break;
                case ContactAvailability.Away:
                case ContactAvailability.FreeIdle:
                case ContactAvailability.Invalid:
                case ContactAvailability.None:
                case ContactAvailability.Offline:
                case ContactAvailability.TemporarilyAway:
                    this.traceEventMessenger.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now away" });
                    this.publishEventMessenger.Publish(StatusCode.Away.ToString());
                    break;
            }
        }

        private void ContactContactInformationChanged(object sender, ContactInformationChangedEventArgs e)
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
