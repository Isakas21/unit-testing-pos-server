﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace PosServer
{
    public class Message
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Msg { get; set; }
        public string Stamp { get; set; }

        public override string ToString()
        {
            return $"From: {From}\nTo: {To}\n{Msg}\nStamp: {Stamp}";
        }
    }

    public class Server
    {
        public static int PORT = 14300;
        public static int TAM = 1024;

        public static Dictionary<string, List<Message>> repo = new Dictionary<string, List<Message>>();

        public static IPAddress GetLocalIpAddress()
        {
            List<IPAddress> ipAddressList = new List<IPAddress>();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            int t = ipHostInfo.AddressList.Length;
            string ip;
            for (int i = 0; i < t; i++)
            {
                ip = ipHostInfo.AddressList[i].ToString();
                if (ip.Contains(".") && !ip.Equals("127.0.0.1")) ipAddressList.Add(ipHostInfo.AddressList[i]);
            }
            if (ipAddressList.Count == 1)
            {
                return ipAddressList[0];
            }
            else
            {
                int i = 0;
                foreach (IPAddress ipa in ipAddressList)
                {
                    Console.WriteLine($"[{i++}]: {ipa}");
                }
                System.Console.Write($"Opción [0-{t - ipAddressList.Count}]: ");
                string s = Console.ReadLine();
                if (Int32.TryParse(s, out int j))
                {
                    if ((j >= 0) && (j <= t))
                    {
                        return ipAddressList[j];
                    }
                }
                return null;
            }
        }

        public static void StartListening()
        {
            byte[] bytes = new Byte[TAM];

            IPAddress ipAddress = GetLocalIpAddress();
            if (ipAddress == null) return;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine("Waiting for a connection at {0}:{1} ...", ipAddress, PORT);
                    Socket handler = listener.Accept();

                    Message request = Receive(handler);

                    Console.WriteLine(request);//Print it

                    Message response = Process(request);

                    Send(handler, response);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void Send(Socket socket, Message message)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Message));
            Stream stream = new MemoryStream();
            serializer.Serialize(stream, message);
            byte[] byteData = ((MemoryStream)stream).ToArray();
            // string xml = Encoding.ASCII.GetString(byteData, 0, byteData.Length);
            // Console.WriteLine(xml);//Imprime el texto enviado
            int bytesSent = socket.Send(byteData);
        }

        public static Message Receive(Socket socket)
        {
            byte[] bytes = new byte[TAM];
            int bytesRec = socket.Receive(bytes);
            string xml = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            // Console.WriteLine(xml);//Imprime el texto recibido
            byte[] byteArray = Encoding.ASCII.GetBytes(xml);
            MemoryStream stream = new MemoryStream(byteArray);
            Message response = (Message)new XmlSerializer(typeof(Message)).Deserialize(stream);
            return response;
        }

        public static void AddMessage(Message message)
        {
            //TODO: Add Message
            if(repo.ContainsKey(message.To)){
                repo[message.To].Add(message);
            }else {
                List<Message> lst = new List<Message>();
                lst.Add(message);
                repo.Add(message.To, lst);
            }
        }

        public static Message ListMessages(string toClient)
        {
            //TODO: List Messages
            try
            {
                StringBuilder sb = new StringBuilder();
                List<Message> numCorreos = repo[toClient];
                int cont = 0;
                foreach (var i in repo[toClient])
                {
                    //Console.WriteLine(i);
                    sb.AppendFormat("[{0}] From: {1}", cont, i.From);
                    sb.AppendLine();
                    cont++;
                }
                if(cont == 0){
                    return new Message { From = "0", To = toClient, Msg = "No hay mensajes", Stamp = "Server" };
                }else{
                    return new Message { From = "0", To = toClient, Msg = sb.ToString(), Stamp = "Server" };  
                }
            }
            catch (System.Exception)
            {
                return new Message { From = "0", To = toClient, Msg = "El usuario no existe", Stamp = "Server" };
            }

            
        }

        public static Message RetrMessage(string toClient, int index)
        {

            //TODO: Retr Message
            try
            {
                List<Message> msg = repo[toClient];
                Message rep = new Message { From = "0", To = toClient, Msg = msg[index].Msg, Stamp = "Server" };
                msg.RemoveAt(index);
                return rep;  

            }
            catch (System.Exception)
            {
                return new Message { From = "0", To = toClient, Msg = "No existe", Stamp = "Server" };
            }
        }

        public static Message Process(Message request)
        {
            //TODO: Process
            Message response;
            try{
                if(request.Msg == "LIST"){
                    response = ListMessages(request.From);
                }else if(request.Msg.Contains("RETR")){
                    var pos = request.Msg.Split(" ");
                    response = RetrMessage(request.From, Int32.Parse(pos[1]));
                }else{
                    //Añadir mensage
                    AddMessage(request);
                    response = new Message { From = "0", To = request.From, Msg = "OK", Stamp = "Server" };
                }
            } catch {
                response = new Message { From = "0", To = request.From, Msg = "ERROR", Stamp = "Server" };
            }
            return response;
        }

        public static int Main(String[] args)
        {
            StartListening();
            return 0;
        }
    }
}