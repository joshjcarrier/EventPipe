namespace EventPipe.Server.Lync
{
    using System;
    using System.Configuration;
    using System.Linq;
    using EventPipe.Common;
    using EventPipe.Common.Data;
    using EventPipe.Common.Events;
    using EventPipe.Common.Events.Lync;
    using Microsoft.Lync.Model;

    internal class LyncService
    {
        private readonly LyncStatusEvent lyncStatusEvent;
        private readonly TraceEvent traceEvent;

        public LyncService(LyncStatusEvent lyncStatusEvent, TraceEvent traceEvent)
        {
            this.lyncStatusEvent = lyncStatusEvent;
            this.traceEvent = traceEvent;
            
            if (LyncClient.GetClient().State != ClientState.SignedIn)
            {
                throw new InvalidOperationException("Lync client not running and signed in.");
            }

            this.traceEvent.Publish(new TraceMessage { Owner = "Lync", Message = "Ready" });
        }

        public static LyncService Create(EventAggregator eventAggregator)
        {
            var publishEventMessenger = eventAggregator.GetEvent<LyncStatusEvent>();
            var traceEventMessenger = eventAggregator.GetEvent<TraceEvent>();
            return new LyncService(publishEventMessenger, traceEventMessenger);
        }

        public void Start()
        {
            foreach (var contactConfig in ConfigurationManager.AppSettings.AllKeys.Where(p => p.StartsWith("contact", StringComparison.OrdinalIgnoreCase)))
            {
                var contact = ConfigurationManager.AppSettings[contactConfig];
                var contactAddress = contact + "@micro" + "soft.com";
                registerStatus(contactAddress);
                traceEvent.Publish(new TraceMessage { Owner = "Lync", Message = "Contact registered: " + contactAddress });
            }
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

            switch (availability)
            {
                case ContactAvailability.Free:
                    this.traceEvent.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now free" });
                    this.lyncStatusEvent.Publish(new LyncStatusChange { ContactUri = contact.Uri, Status = StatusCode.Free.ToString() });
                    break;
                case ContactAvailability.Busy:
                case ContactAvailability.BusyIdle:
                case ContactAvailability.DoNotDisturb:
                    this.traceEvent.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now busy" });
                    this.lyncStatusEvent.Publish(new LyncStatusChange { ContactUri = contact.Uri, Status = StatusCode.Busy.ToString() });
                    break;
                case ContactAvailability.Away:
                case ContactAvailability.FreeIdle:
                case ContactAvailability.Invalid:
                case ContactAvailability.None:
                case ContactAvailability.Offline:
                case ContactAvailability.TemporarilyAway:
                    this.traceEvent.Publish(new TraceMessage { Owner = "Lync", Message = contact.Uri + " is now away" });
                    this.lyncStatusEvent.Publish(new LyncStatusChange { ContactUri = contact.Uri, Status = StatusCode.Away.ToString() });
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
