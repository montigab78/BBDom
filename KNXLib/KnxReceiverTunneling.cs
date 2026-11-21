using KNXLib.Log;
using System;
using System.Net;
using System.Net.Sockets;

namespace KNXLib
{
    internal class KnxReceiverTunneling : KnxReceiver
    {
        private static readonly string ClassName = typeof(KnxReceiverTunneling).ToString();

        private readonly UdpClient _udpClient;
        private IPEndPoint _localEndpoint;

        private readonly object _rxSequenceNumberLock = new object();
        private byte _rxSequenceNumber;

        internal KnxReceiverTunneling(KnxConnection connection, UdpClient udpClient, IPEndPoint localEndpoint)
            : base(connection)
        {
            _udpClient = udpClient;
            _localEndpoint = localEndpoint;
        }

        private KnxConnectionTunneling KnxConnectionTunneling
        {
            get { return (KnxConnectionTunneling)KnxConnection; }
        }

        protected override void StopReceiving()
        {
            Logger.Debug(ClassName, "Closing tunneling UDP socket to unblock reception");
            _udpClient.Close();
        }

        public override void ReceiverThreadFlow()
        {
            try
            {
                while (!IsStopping)
                {
                    var datagram = _udpClient.Receive(ref _localEndpoint);
                    if (!IsStopping)
                    {
                        ProcessDatagram(datagram);
                    }
                }
            }
            catch (SocketException e)
            {
                if (!IsStopping)
                {
                    Logger.Error(ClassName, e);
                    KnxConnectionTunneling.Disconnected();
                }
            }
            catch (ObjectDisposedException) when (IsStopping)
            {
                Logger.Debug(ClassName, "Pending UDP reception ended because the socket was closed");
            }
        }

        private void ProcessDatagram(byte[] datagram)
        {
            try
            {
                if (datagram.Length >= 4)
                {
                    Logger.Debug(ClassName, "Received {0} bytes from {1}; service 0x{2:X2}{3:X2}", datagram.Length, _localEndpoint, datagram[2], datagram[3]);
                }
                else
                {
                    Logger.Warn(ClassName, "Received short datagram: {0} bytes from {1}", datagram.Length, _localEndpoint);
                }
                switch (KnxHelper.GetServiceType(datagram))
                {
                    case KnxHelper.SERVICE_TYPE.CONNECT_RESPONSE:
                        ProcessConnectResponse(datagram);
                        break;
                    case KnxHelper.SERVICE_TYPE.CONNECTIONSTATE_RESPONSE:
                        ProcessConnectionStateResponse(datagram);
                        break;
                    case KnxHelper.SERVICE_TYPE.TUNNELLING_ACK:
                        ProcessTunnelingAck(datagram);
                        break;
                    case KnxHelper.SERVICE_TYPE.DISCONNECT_REQUEST:
                        ProcessDisconnectRequest(datagram);
                        break;
                    case KnxHelper.SERVICE_TYPE.DISCONNECT_RESPONSE:
                        ProcessDisconnectResponse(datagram);
                        break;
                    case KnxHelper.SERVICE_TYPE.TUNNELLING_REQUEST:
                        ProcessDatagramHeaders(datagram);
                        break;
                }
            }
            catch (ObjectDisposedException) when (IsStopping)
            {
                // A consumer callback can close the socket before the ACK is sent.
            }
            catch (SocketException) when (IsStopping)
            {
                // A send already in progress can be interrupted by shutdown.
            }
            catch (Exception e)
            {
                Logger.Error(ClassName, e);

                // ignore, missing warning information
            }
        }

        private void ProcessDatagramHeaders(byte[] datagram)
        {
            // HEADER
            // TODO: Might be interesting to take out these magic numbers for the datagram indices
            var knxDatagram = new KnxDatagram
            {
                header_length = datagram[0],
                protocol_version = datagram[1],
                service_type = new[] { datagram[2], datagram[3] },
                total_length = datagram[4] + datagram[5]
            };

            var channelId = datagram[7];
            if (channelId != KnxConnectionTunneling.ChannelId)
            {
                Logger.Debug(ClassName, "Ignoring telegram for channel {0}; active channel is {1}", channelId, KnxConnectionTunneling.ChannelId);
                return;
            }

            var sequenceNumber = datagram[8];
            var process = true;
            lock (_rxSequenceNumberLock)
            {
                if (sequenceNumber <= _rxSequenceNumber)
                    process = false;

                _rxSequenceNumber = sequenceNumber;
            }

            if (process)
            {
                // TODO: Magic number 10, what is it?
                var cemi = new byte[datagram.Length - 10];
                Array.Copy(datagram, 10, cemi, 0, datagram.Length - 10);

                ProcessCEMI(knxDatagram, cemi);
            }
            else
            {
                Logger.Debug(ClassName, "Skipping already processed telegram: channel {0}, sequence {1}", channelId, sequenceNumber);
            }

            ((KnxSenderTunneling)KnxConnectionTunneling.KnxSender).SendTunnelingAck(sequenceNumber);
            Logger.Debug(ClassName, "Tunneling ACK sent: channel {0}, sequence {1}", channelId, sequenceNumber);
        }

        private void ProcessDisconnectRequest(byte[] datagram)
        {
            Logger.Info(ClassName, "Gateway requested disconnection");
            KnxConnectionTunneling.DisconnectRequest();
        }

        private void ProcessDisconnectResponse(byte[] datagram)
        {
            var channelId = datagram[6];
            if (channelId != KnxConnectionTunneling.ChannelId)
                return;

            KnxConnectionTunneling.Disconnect();
        }

        private void ProcessTunnelingAck(byte[] datagram)
        {
            // do nothing
        }

        private void ProcessConnectionStateResponse(byte[] datagram)
        {
            // HEADER
            // 06 10 02 08 00 08 -- 48 21
            var knxDatagram = new KnxDatagram
            {
                header_length = datagram[0],
                protocol_version = datagram[1],
                service_type = new[] { datagram[2], datagram[3] },
                total_length = datagram[4] + datagram[5],
                channel_id = datagram[6]
            };

            var response = datagram[7];
            Logger.Debug(ClassName, "Connection state response: channel {0}, status 0x{1:X2}", knxDatagram.channel_id, response);

            if (response != 0x21)
                return;

            Logger.Warn(ClassName, "Gateway reports no active connection with channel {0}; disconnecting", knxDatagram.channel_id);

            KnxConnection.Disconnect();
        }

        private void ProcessConnectResponse(byte[] datagram)
        {
            // HEADER
            var knxDatagram = new KnxDatagram
            {
                header_length = datagram[0],
                protocol_version = datagram[1],
                service_type = new[] { datagram[2], datagram[3] },
                total_length = datagram[4] + datagram[5],
                channel_id = datagram[6],
                status = datagram[7]
            };

            Logger.Debug(ClassName, "Connection response: channel {0}, status 0x{1:X2}", knxDatagram.channel_id, knxDatagram.status);
            if (knxDatagram.channel_id == 0x00 && knxDatagram.status == 0x24)
            {
                Logger.Info(ClassName, "KNXLib received connect response - No more connections available");                
            }
            else
            {
                KnxConnectionTunneling.ChannelId = knxDatagram.channel_id;
                KnxConnectionTunneling.ResetSequenceNumber();

                KnxConnectionTunneling.Connected();
            }
        }
    }
}
