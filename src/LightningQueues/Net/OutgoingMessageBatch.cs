using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LightningQueues.Net.Security;

namespace LightningQueues.Net
{
    public class OutgoingMessageBatch : IDisposable
    {
        private readonly IStreamSecurity _security;
        
        public OutgoingMessageBatch(Uri destination, IEnumerable<OutgoingMessage> messages, TcpClient client, IStreamSecurity security)
        {
            _security = security;
            Destination = destination;
            var messagesList = new List<OutgoingMessage>();
            messagesList.AddRange(messages);
            Messages = messagesList;
            Client = client;
        }

        public Uri Destination { get; set; }
        public IObservable<Stream> Stream => _security.Apply(Destination, Observable.Return(Client.GetStream()));
        public TcpClient Client { get; set; }
        public IList<OutgoingMessage> Messages { get; }

        public Task ConnectAsync()
        {
            if(Dns.GetHostName() == Destination.Host)
            {
                return Client.ConnectAsync(IPAddress.Loopback, Destination.Port);
            }

            return Client.ConnectAsync(Destination.Host, Destination.Port);
        }

        public void Dispose()
        {
            using (Client)
            {
            }
            Client = null;
        }
    }
}