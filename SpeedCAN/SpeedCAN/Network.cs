using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace SpeedCAN
{
    public class Network
    {
        string IP;
        string SubnetMask;
        string Gateway;
        string DNS;
        byte[] MacAddress;

        private readonly GpioPin chipSelectPin;
        private readonly GpioPin ethResetPin;
        private readonly GpioPin ethInterruptPin;

        private string SpiAPI;

        public bool HasLink { get; set; }

        public Network(string ip, string subnetmask, string gateway, string dns, byte[] mac, int chipSelect = SC20100.GpioPin.PB8, int ethReset = SC20100.GpioPin.PB9, int ethInterrupt = SC20100.GpioPin.PD6, string spi = SC20260.SpiBus.Spi3)
        {
            IP = ip;
            SubnetMask = subnetmask;
            Gateway = gateway;
            DNS = dns;
            MacAddress = mac;

            chipSelectPin = GpioController.GetDefault().OpenPin(chipSelect);
            ethResetPin = GpioController.GetDefault().OpenPin(ethReset);
            ethInterruptPin = GpioController.GetDefault().OpenPin(ethInterrupt);

            SpiAPI = spi;
        }

        public void InitializeNetwork()
        {
            var networkController = NetworkController.FromName("GHIElectronics.TinyCLR.NativeApis.ENC28J60.NetworkController");

            var networkInterfaceSetting = new EthernetNetworkInterfaceSettings();

            var networkCommunicationInterfaceSettings = new SpiNetworkCommunicationInterfaceSettings();

            var settings = new GHIElectronics.TinyCLR.Devices.Spi.SpiConnectionSettings()
            {
                ChipSelectLine = chipSelectPin,
                ClockFrequency = 4000000,
                Mode = GHIElectronics.TinyCLR.Devices.Spi.SpiMode.Mode0,
                ChipSelectType = GHIElectronics.TinyCLR.Devices.Spi.SpiChipSelectType.Gpio,
                ChipSelectHoldTime = TimeSpan.FromTicks(10),
                ChipSelectSetupTime = TimeSpan.FromTicks(10)
            };

            networkCommunicationInterfaceSettings.SpiApiName = SpiAPI;
            networkCommunicationInterfaceSettings.GpioApiName = SC20100.GpioPin.Id;
            networkCommunicationInterfaceSettings.SpiSettings = settings;

            networkCommunicationInterfaceSettings.InterruptPin = ethInterruptPin;
            networkCommunicationInterfaceSettings.InterruptEdge = GpioPinEdge.FallingEdge;
            networkCommunicationInterfaceSettings.InterruptDriveMode = GpioPinDriveMode.InputPullUp;

            networkCommunicationInterfaceSettings.ResetPin = ethResetPin;
            networkCommunicationInterfaceSettings.ResetActiveState = GpioPinValue.Low;

            networkInterfaceSetting.Address = new IPAddress(StringToIP(IP));
            networkInterfaceSetting.SubnetMask = new IPAddress(StringToIP(SubnetMask));
            networkInterfaceSetting.GatewayAddress = new IPAddress(StringToIP(Gateway));
            networkInterfaceSetting.DnsAddresses = new IPAddress[] { new IPAddress(StringToIP(DNS)) };

            networkInterfaceSetting.MacAddress = MacAddress;
            networkInterfaceSetting.IsDhcpEnabled = false;

            networkController.SetInterfaceSettings(networkInterfaceSetting);
            networkController.SetCommunicationInterfaceSettings(networkCommunicationInterfaceSettings);

            networkController.SetAsDefaultController();

            networkController.NetworkAddressChanged += NetworkController_NetworkAddressChanged;
            networkController.NetworkLinkConnectedChanged += NetworkController_NetworkLinkConnectedChanged;

            networkController.Enable();

            //Wait until we get a link
            while (!HasLink)
            {
                Debug.WriteLine("Waiting for network link...");
                Thread.Sleep(1000);
            }

            Debug.WriteLine("Network link achieved!");
        }
        private void NetworkController_NetworkLinkConnectedChanged(NetworkController sender, NetworkLinkConnectedChangedEventArgs e)
        {
        }

        private void NetworkController_NetworkAddressChanged(NetworkController sender, NetworkAddressChangedEventArgs e)
        {
            var ipProperties = sender.GetIPProperties();
            var address = ipProperties.Address.GetAddressBytes();

            HasLink = address[0] != 0;
        }

        private static byte[] StringToIP(string stringIP)
        {
            byte[] IP = new byte[4];
            string[] splitIP = stringIP.Split('.');

            for (int i = 0; i < 4; i++)
                IP[i] = byte.Parse(splitIP[i]);

            return IP;
        }


    }
}
