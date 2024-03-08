﻿using System.Net.Sockets;
using System.Net;
using OmronCommunication;
using System.IO;

namespace OmronCommunication.Profinet
{
    internal class FinsTcp : FinsCommand, IProfinet
    {
        private readonly TcpClient _tcpClient;
        private IPAddress? _localIP;
        private IPAddress? _remoteIP;

        private readonly byte[] _handSignal =
        {
            0x46, 0x49, 0x4E, 0x53, // FINS
            0x00, 0x00, 0x00, 0x0C, // 后面的命令长度
            0x00, 0x00, 0x00, 0x00, // 命令码
            0x00, 0x00, 0x00, 0x00, // 错误码
            0x00, 0x00, 0x00, 0x01  // 节点号
        };

        public FinsTcp(string remoteIP, int remotePort)
        {
            _tcpClient = new TcpClient();
            RemoteIP = IPAddress.Parse(remoteIP);
            RemotePort = remotePort;
            LocalIP = ((IPEndPoint)TcpClient.Client.LocalEndPoint!).Address;
        }

        public TcpClient TcpClient { get => _tcpClient; } 

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

        /// <summary>
        /// Fins/Tcp握手信号，每次传输都要加上
        /// </summary>
        public byte[] HandSignal { get => _handSignal; }

        #region Connect

        public OperationResult ConnectPLC()
        {
            try
            {
                TcpClient.Connect(RemoteIP, RemotePort);
                return new OperationResult(true);
                //TODO 第一次连接需要与PLC握手

            }
            catch (Exception ex)
            {
                return new OperationResult(false)
                {
                    Message = ex.Message,
                };
            }
        }

        public OperationResult ClosePLC()
        {
            try
            {
                TcpClient.Close();
                return new OperationResult(true);

            }
            catch (Exception ex)
            {
                return new OperationResult(false)
                {
                    Message = ex.Message,
                };
            }
        }
  
        #endregion

        #region Networkstream

        //TODO
        public OperationResult Send(byte[] data)
        {
            var stream = new NetworkStream(TcpClient.Client);

            stream.Write(data, 0, data.Length);

            return new OperationResult();
        }

        //TODO
        public OperationResult<byte[]> Receive()
        {
            var stream = new NetworkStream(TcpClient.Client);
            var buff = new byte[2048];
            stream.Read(buff);

            var result = new OperationResult<byte[]>() { Value = buff };
            return result;
        }

        //TODO
        public OperationResult<byte[]> SendAndReceive(byte[] data)
        {
            Send(data);

            var result = Receive();

            return result;
        }

        #endregion

        #region Read Write

        public OperationResult Write(string address, byte[] data, bool isBit)
        {
            //BuildWriteCommand
            var command = BuildFinsWriteCommand(address, data, isBit);


            //ReadFromPLC
            var response = SendAndReceive(command.Value);


            //DataAnalysis
            var result = AnalyzeFinsResponse(response);


            return result;
        }

        public OperationResult<byte[]> Read(string address, ushort length, bool isBit)
        {
            //BuildReadCommand
            var command = BuildFinsReadCommand(address, length, isBit);


            //ReadFromPLC
            var response = SendAndReceive(command.Value);


            //DataAnalysis
            var result = AnalyzeFinsResponse(response);

            return result;
        }

        #endregion

        /// <summary>
        /// 按 TCP 传输组合完整的 FINS 数据包
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override byte[] BuildCompleteCommand(byte[] command)
        {
            //TODO 基于UDP构建 并在开头加上TCP握手信号
            var udpCommand= base.BuildCompleteCommand(command);
            var completeCommand = new byte[udpCommand.Length + HandSignal.Length];
            HandSignal.CopyTo(completeCommand, 0);
                


            udpCommand.CopyTo(completeCommand, HandSignal.Length - 1);
            return completeCommand;
        }

        public override OperationResult<byte[]> AnalyzeFinsResponse(OperationResult<byte[]> result)
        {
            //TODO 
            return base.AnalyzeFinsResponse(result);
        }

    }
}
