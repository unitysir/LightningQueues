using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using LightningQueues.Logging;

namespace LightningQueues.Net.Tcp
{
    public class Receiver : IDisposable
    {
        readonly TcpListener _listener;
        readonly IReceivingProtocol _protocol;
        private readonly ILogger _logger;
        bool _disposed;
        IObservable<Message> _stream;
        private readonly object _lockObject;
        
        public Receiver(IPEndPoint endpoint, IReceivingProtocol protocol, ILogger logger)
        {
            Endpoint = endpoint;
            Timeout = TimeSpan.FromSeconds(5);
            _protocol = protocol;
            _logger = logger;
            _listener = new TcpListener(Endpoint);
            _lockObject = new object();
        }

        public TimeSpan Timeout { get; set; }

        public IPEndPoint Endpoint { get; }

        public IObservable<Message> StartReceiving()
        {
            lock (_lockObject)
            {
                if (_stream != null)
                    return _stream;

                _listener.Start();

                _logger.Debug($"TcpListener started listening on port: {Endpoint.Port}");
                _stream = Observable.While(IsNotDisposed, ContinueAcceptingNewClients())
                    .Using(x => _protocol.ReceiveStream(Observable.Return(x.GetStream())))
                    .Publish()
                    .RefCount();
            }
            return _stream;
        }

        private bool IsNotDisposed()
        {
            return !_disposed;
        }

        private IObservable<TcpClient> ContinueAcceptingNewClients()
        {
            return Observable.FromAsync(() => _listener.AcceptTcpClientAsync())
                .Do(x => _logger.Debug($"Client at {x.Client.RemoteEndPoint} connection established."))
                .Repeat();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _listener.Stop();
            }
        }
    }
}