using GHIElectronics.TinyCLR.Devices.Can;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SpeedCAN
{
    class Program
    {
        public static Socket sock;
        public static CanController can;
        public static Network Network;

        public static IPAddress serverAddr = IPAddress.Parse("192.168.181.209");

        public static IPEndPoint endPoint = new IPEndPoint(serverAddr, 5051);

        public static CanMessage msg;
        public static int msgCount = 0;

        static void Main()
        {
            //Instantiate the network class
            Network = new Network("192.168.181.210", "255.255.255.0", "192.168.181.1", "192.168.181.1", new byte[] { 0xA1, 0xA6, 0xB9, 0x3E, 0xD0, 0x1F });

            //Initialize the network
            Network.InitializeNetwork();

            //Create a UDP socket
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Start the CAN at 250kbps
            can = CanController.FromName(SC20100.CanBus.Can1);
            can.SetNominalBitTiming(new CanBitTiming(13, 2, 12, 1, false));
            can.ErrorReceived += Can_ErrorReceived; //Attach to the error event

            can.Enable();//Enable the CAN peripheral

            new Thread(Print).Start(); //Start a thread that will print how many messages we processed last second

            //A byte array used in the loop
            byte[] data = new byte[8];

            while (true)
            {
                //If we have messages to read
                if (can.MessagesToRead > 0)
                {
                    //Read the message
                    can.ReadMessage(out msg);
                    msgCount++; //Increment the count
                    Array.Copy(msg.Data, data, 8); //Copy the data so we only take the first 8 received bytes instead of all 64
                    //sock.SendTo(data, endPoint); //Send the data to the endpoint over the socket. REMOVE THIS LINE TO BE ABLE TO PROCESS AS MUCH MESSAGES AS CAN ALLOWS
                }
            }
        }

        //A static function that runs as a seperate thread
        //It prints the amount of messages every second and resets the counter
        public static void Print()
        {
            while(true)
            {
                Debug.WriteLine(msgCount.ToString());
                msgCount = 0;
                Thread.Sleep(1000);
            }
        }

        //Triggered when an error on the CAN bus occurs
        private static void Can_ErrorReceived(CanController sender, ErrorReceivedEventArgs e)
        => System.Diagnostics.Debug.WriteLine("error " + ((object)e.Error).ToString());
    }
}
