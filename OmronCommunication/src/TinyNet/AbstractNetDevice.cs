﻿using OmronCommunication.Internal.Logging;
using OmronCommunication.src.TinyNet;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OmronCommunication.TinyNet
{
    public abstract class AbstractNetDevice : INetDevice
    {
        protected readonly string? _deviceID;
        protected readonly EndPoint? _deviceAddress;
        protected Socket? _coresocket;
        protected int _receiveBufferSize;

        public AbstractNetDevice(EndPoint deviceAddress)
        {
            _deviceAddress = deviceAddress;
        }
        public AbstractNetDevice(EndPoint deviceAddress, string deviceID)
        {
            _deviceID = deviceID;
            _deviceAddress = deviceAddress;
        }

        public virtual string? DeviceID => _deviceID;
        public virtual EndPoint? DeviceAddress => _deviceAddress;
        public Socket? CoreSocket => _coresocket;
        public int ReceiveBufferSize { get => _receiveBufferSize; set => _receiveBufferSize = value; }

        /// <summary>
        /// Bind to a specified socket
        /// </summary>
        public virtual void InitWithBind(Socket socket)
        {
            ArgumentNullException.ThrowIfNull(socket);
            _coresocket = socket;     
        }
        public virtual void InitWithBind(AddressFamily family,SocketType socketType, ProtocolType protocolType, IPEndPoint localAddress)
        {
            _coresocket = new Socket(family,socketType,protocolType);
            _coresocket.Bind(localAddress);
        }
        public virtual void InitWithNoBind(AddressFamily family, SocketType socketType, ProtocolType protocolType)
        {
            _coresocket = new Socket(family, socketType, protocolType);
        }

        public virtual Task ConnectAsync()
        {
            ArgumentNullException.ThrowIfNull(DeviceAddress);
            return ConnectAsync(DeviceAddress);
        }

        public virtual Task ConnectAsync(EndPoint deviceAddress)
        {
            ArgumentNullException.ThrowIfNull(CoreSocket);
           return CoreSocket.ConnectAsync(deviceAddress);
        }
  
        public virtual void Close()
        {
            CoreSocket!.Close();
        }

        public virtual async Task<byte[]> ResqusetWaitResponse(byte[] send)
        {
            await CoreSocket!.SendAsync(send);

            var buffer = new byte[ReceiveBufferSize];
            var rev = CoreSocket!.ReceiveAsync(buffer);

            await rev;
            var newbuffer = new byte[rev.Result];
            Array.Copy(buffer, 0, newbuffer, 0, rev.Result);
            return newbuffer;
        }

    }
}
