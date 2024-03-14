﻿using System.Net.Sockets;
using System.Net;
using OmronCommunication;
using System.IO;
using OmronCommunication.TinyNet;
using OmronCommunication.DataTypes;
using OmronCommunication.src.TinyNet;

namespace OmronCommunication.Profinet
{
    public class FinsTcp : FinsCommand, IProfinet
    {
        private readonly INetDevice _netdevice;

        private TcpClient? _tcpClient;
        private IPAddress? _localIP;
        private IPAddress? _remoteIP;
        private bool _isActive;
        private readonly byte[] _handSignal =
        {
            0x46, 0x49, 0x4E, 0x53, // FINS
            0x00, 0x00, 0x00, 0x0C, // 后面的命令长度
            0x00, 0x00, 0x00, 0x00, // 命令码
            0x00, 0x00, 0x00, 0x00, // 错误码
            0x00, 0x00, 0x00, 0x01  // 节点号
        };

        public FinsTcp(IPAddress remoteIP, int remotePort)
        {

            RemoteIP = remoteIP;
            RemotePort = remotePort;
            var remoteAddress = new IPEndPoint(RemoteIP, RemotePort);
            _netdevice = new NetUdpDevice(remoteAddress);
        }
        public FinsTcp(int localPort, IPAddress remoteIP, int remotePort)
        {
            LocalPort = localPort;
            RemoteIP = remoteIP;
            RemotePort = remotePort;
            var remoteAddress = new IPEndPoint(RemoteIP, RemotePort);
            _netdevice = new NetUdpDevice(remoteAddress);
        }

        /// <summary>
        /// 上位机网络地址
        /// </summary>
        public IPAddress? LocalIP
        {
            get => _localIP;
            private set
            {
                _localIP = value;
                SA1 = value.GetAddressBytes()[3];
            }
        }
        /// <summary>
        /// 上位机的端口
        /// </summary>
        public int LocalPort { get; set; }
        /// <summary>
        /// 目标网络设备的IP地址
        /// </summary>
        public IPAddress? RemoteIP
        {
            get => _remoteIP;
            private set
            {
                _remoteIP = value;
                DA1 = value.GetAddressBytes()[3];
            }
        }
        /// <summary>
        /// 目标网络设备的端口, omron plc 默认 9600
        /// </summary>
        public int RemotePort { get; private set; } = 9600;
        public int ReceiveTimeout { get; set; } = 3000;
        public int ReceiveBufferSize { get; set; } = 1024;
        public INetDevice NetDevice => _netdevice;
        /// <summary>
        /// Fins/Tcp握手信号，每次传输都要加上
        /// </summary>
        public byte[] HandSignal { get => _handSignal; }

        #region Connect
        /// <summary>
        /// 建立连接
        /// </summary>
        public Task ConnectAsync()
        {
            NetDevice.InitWithNoBind(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NetDevice.ReceiveBufferSize = 1024;
            return NetDevice.ConnectAsync();
        }

        /// <summary>
        /// 关闭并释放连接
        /// </summary>
        public void Close()
        {
            NetDevice.Close();
        }

        #endregion

        #region Network


        #endregion

        #region Read Write

        public async Task Write(string address, byte[] data, bool isBit)
        {
            //BuildWriteCommand
            var command = BuildFinsWriteCommand(address, data, isBit);

            //ReadFromPLC
            var response = await NetDevice.ResqusetWaitResponse(command);

            //DataAnalysis
            AnalyzeFinsResponse(response);
        }

        public async Task<byte[]> Read(string address, ushort length, bool isBit)
        {
            //BuildReadCommand
            var command = BuildFinsReadCommand(address, length, isBit);

            //ReadFromPLC
            var response = await NetDevice.ResqusetWaitResponse(command);

            //DataAnalysis
            var result = AnalyzeFinsResponse(response);
            return result.Data;
        }

        #endregion

        /// <summary>
        /// 按 TCP 传输组合完整的 FINS 数据包
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override byte[] BuildCompleteCommand(byte[] command)
        {
            //基于UDP Command构建 并在开头加上TCP握手信号16字节
            var udpCommand = base.BuildCompleteCommand(command);
            var completeCommand = new byte[udpCommand.Length + 16];
            Array.Copy(HandSignal, 0, completeCommand, 0, 16);
            Array.Copy(udpCommand, 0, completeCommand, 16, udpCommand.Length);
            var dataLength = BitConverter.GetBytes(completeCommand.Length - 8);
            Array.Reverse(dataLength);
            Array.Copy(dataLength, 0, completeCommand, 4, dataLength.Length);
            completeCommand[11] = 0x02;
            return completeCommand;
        }

        public override FinsResponse AnalyzeFinsResponse(byte[] result)
        {
            //TODO 错误码
            //正确的返回，至少包含 fins tcp header.fins command code. end code 共 30字节 
            if (result!.Length > 30)
            {
                //有返回数据,拆分出数据
                var buffer = new byte[result.Length - 30];
                Array.Copy(result, 30, buffer, 0, buffer.Length);
                return new FinsResponse() { Data = buffer };
            }
            //无返回数据
            if (result!.Length == 30)
            {
                return FinsResponse.NormalSuccess;
            }
            return FinsResponse.NormalSuccess;
        }
    }
}
