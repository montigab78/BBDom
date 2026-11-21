using KNXLib.Log;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace KNXLib
{
    internal class KnxReceiverRouting : KnxReceiver
    {
        private static readonly string ClassName = typeof(KnxReceiverRouting).ToString();

        private readonly IList<UdpClient> _udpClients;

        internal KnxReceiverRouting(KnxConnection connection, IList<UdpClient> udpClients)
            : base(connection)
        {
            _udpClients = udpClients;
        }

        protected override void StopReceiving()
        {
            Logger.Debug(ClassName, "Closing {0} routing UDP sockets", _udpClients.Count);
            foreach (var client in _udpClients)
            {
                client.Close();
            }
        }

        public override void ReceiverThreadFlow()
        {
            try
            {
                foreach (var client in _udpClients)
                {
                    if (IsStopping)
                    {
                        break;
                    }

                    client.BeginReceive(OnReceive, new object[] { client });
                }
            }
            catch (ObjectDisposedException) when (IsStopping)
            {
                // Stop can close the socket while receive is being registered.
            }
            catch (Exception e)
            {
                Logger.Error(ClassName, e);
            }
        }

        private void OnReceive(IAsyncResult result)
        {
            IPEndPoint endPoint = null;
            var args = (object[])result.AsyncState;
            var session = (UdpClient)args[0];

            try
            {
                var datagram = session.EndReceive(result, ref endPoint);
                if (!IsStopping)
                {
                    Logger.Debug(ClassName, "Received routing datagram: {0} bytes from {1}", datagram.Length, endPoint);
                    ProcessDatagram(datagram);
                }

                if (!IsStopping)
                {
                    session.BeginReceive(OnReceive, args);
                }
            }
            catch (ObjectDisposedException) when (IsStopping)
            {
                // Closing the socket completes the pending receive.
            }
            catch (SocketException) when (IsStopping)
            {
                // A pending receive can also complete with a socket error.
            }
            catch (Exception e)
            {
                Logger.Error(ClassName, e);
            }
        }

        private void ProcessDatagram(byte[] datagram)
        {
            try
            {
                ProcessDatagramHeaders(datagram);
            }
            catch (Exception e)
            {
                Logger.Error(ClassName, e);
            }
        }

        private void ProcessDatagramHeaders(byte[] datagram)
        {
            // HEADER
            var knxDatagram = new KnxDatagram
            {
                header_length = datagram[0],
                protocol_version = datagram[1],
                service_type = new[] { datagram[2], datagram[3] },
                total_length = datagram[4] + datagram[5]
            };

            var cemi = new byte[datagram.Length - 6];
            Array.Copy(datagram, 6, cemi, 0, datagram.Length - 6);

            ProcessCEMI(knxDatagram, cemi);
        }
    }
}
