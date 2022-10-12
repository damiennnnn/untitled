using System;
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using System.Threading;

namespace untitled.Server
{
    public class LocalServer : IServer
    {
        private ILogger _log;
        private NetManager _server;
        private EventBasedNetListener _listener;
        private Timer _tickTimer;
        private double _tickrate;
        public LocalServer(int port, double tickrate = (1000.0 / 60.0))
        {
            _log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            
            if (port <= 0)
                throw new ArgumentException("Server port must be >= 0");
            
            // Initialise LiteNetLib server
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);

            _log.Information("Server started on port {Port}: {Started}", port, _server.Start(port));

            // Create events to handle client connections
            _listener.ConnectionRequestEvent += request => OnConnectionRequest(request);
            _listener.PeerConnectedEvent += peer => OnPeerConnect(peer);

            _tickTimer = new Timer(DoServerTick, _tickTimer, System.TimeSpan.Zero, TimeSpan.FromMilliseconds(tickrate));
        }

        public IServer StartServer()
        {
            return new LocalServer(9500);
        }

        private void DoServerTick(Object stateInfo)
        {
            _server.PollEvents();
            _log.Information("Did server tick {State}", stateInfo);
        }
        
        private void OnConnectionRequest(ConnectionRequest request){
            if (_server.ConnectedPeersCount < 10)
                request.AcceptIfKey("untitled");
            else request.Reject();
        }
        private void OnPeerConnect(NetPeer peer){
            _log.Information("New connection: {Endpoint}", peer.EndPoint);
            NetDataWriter writer = new NetDataWriter();
            writer.Put("connection test");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}