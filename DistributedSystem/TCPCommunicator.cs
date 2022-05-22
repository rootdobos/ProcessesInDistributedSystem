using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Communication;
using System.IO;
using MultiLayerCommunication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem
{
    public class TCPCommunicator:ICommunicable
    {
        public void AddSystemExecutor(string system, PipelineExecutor executor)
        {
            _Executors.Add(system, executor);
        }

        public TCPCommunicator(int index,string hubIP, int hubPort,string myIp, int myPort)
        {
            _Executors = new Dictionary<string, PipelineExecutor>();
            _Index = index;

            _MyIP = System.Net.IPAddress.Parse(myIp);
            _MyEndPoint = new IPEndPoint(_MyIP, myPort);
            _ListeningSocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            List<object> listeningThreadParameters = new List<object>();
            listeningThreadParameters.Add(_ListeningSocket);
            listeningThreadParameters.Add(_MyEndPoint);

            Thread t = new Thread(new ParameterizedThreadStart(TCPDeliver));
            t.Start(listeningThreadParameters);
            _HubIP = System.Net.IPAddress.Parse(hubIP);
            _HubEndPoint = new IPEndPoint(_HubIP, hubPort);

        }
        public  void TCPDeliver(object parameters)
        {
            List<object> parameterList = (List<object>)parameters;
            Socket listeningSocket = (Socket)parameterList[0];
            IPEndPoint ip = (IPEndPoint)parameterList[1];

            listeningSocket.Bind(ip);
            listeningSocket.Listen(100);
            Console.WriteLine("{0} Listening to a new messages ... ", _Index);
            while (true)
            {

                Socket clientSocket = listeningSocket.Accept();
                int offset = 0;
                byte[] buffer = new byte[32768];
                int size = 32768;
                int readBytes;
                do
                {
                    readBytes = clientSocket.Receive(buffer, offset, buffer.Length - offset,
                                               SocketFlags.None);

                    offset += readBytes;
                    if(offset>4)
                        size = GetSizeFromByteArray(buffer);
                } while ((readBytes > 0 && offset < buffer.Length) && offset<size);

                byte[] data = GetDataFromByteArray(buffer, size);

                Message m = Message.Parser.ParseFrom(data);

                Thread.Sleep(2);
                MessageEventArgs args = new MessageEventArgs();
                    args.Message = m;

                    string pipelineId=m.ToAbstractionId;
                string systemID = m.SystemId;
                if (m.NetworkMessage.Message.Type == Message.Types.Type.ProcInitializeSystem)
                    systemID = "base";
                PipelineExecutor systemExecutor = _Executors[systemID];

                systemExecutor.ProcessMessageBottomUp(pipelineId, args);

                try
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
                catch (Exception e) { }
            }
        }

        public void Send(object sender,IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            message.NetworkMessage.SenderListeningPort = _MyEndPoint.Port;
            message.NetworkMessage.SenderHost = _MyEndPoint.Address.ToString();
            int endPort = ((MessageEventArgs)messageArgs).EndPort;

            IPAddress destIP = System.Net.IPAddress.Parse(((MessageEventArgs)messageArgs).EndHost);

            IPEndPoint endPoint = new IPEndPoint(destIP, endPort);

            Socket sendingsocket = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sendingsocket.Connect(endPoint);

            byte[] messageArray = message.ToByteArray();
            byte[] messageLengthBytes = BitConverter.GetBytes(messageArray.Length);
            byte[] fullMessage = CombineLengthAndData(messageLengthBytes, messageArray);

                int byteSent = sendingsocket.Send(fullMessage);

            sendingsocket.Shutdown(SocketShutdown.Both);
            sendingsocket.Close();


        }

        private Socket _ListeningSocket;
        private Socket _SendingSocket;
        private IPEndPoint _HubEndPoint;
        private IPEndPoint _MyEndPoint;
        private IPAddress _MyIP;
        private IPAddress _HubIP;
        private int _Index;
        private Dictionary<string,PipelineExecutor> _Executors;
        private static byte[] CombineLengthAndData(byte[] length, byte[] data)
        {
            byte[] output = new byte[length.Length + data.Length];
            for (int i = 0; i < 4; i++)
            {
                output[3 - i] = length[i];
            }
            Array.Copy(data, 0, output, length.Length, data.Length);
            return output;
        }
        private static int GetSizeFromByteArray(byte[] data)
        {
            byte[] output = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                output[3 - i] = data[i];
            }
            return BitConverter.ToInt32(output, 0);
        }
        private static byte[] GetDataFromByteArray(byte[] data, int size)
        {
            byte[] output = new byte[size];
            Array.Copy(data, 4, output, 0, output.Length);
            return output;
        }
        private static string DataBytesString(byte[] bytes)
        {
            string output = bytes[0].ToString();
            for (int i = 1; i < bytes.Length; i++)
                output += ":" + bytes[i].ToString();
            return output;
        }

    }
}
