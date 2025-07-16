using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(vServerStart);
        }

        async private void vServerStart()
        {
            using (TcpListener server = new TcpListener(IPAddress.Any, 8888))
            {
                try
                {
                    server.Start();
                    while (true)
                    {
                        var tcpClient = await server.AcceptTcpClientAsync();
                        Task.Run(async () => await ProcessClientAsync(tcpClient));
                    }
                }
                catch (Exception e) { }
            }
        }
        async Task ProcessClientAsync(TcpClient tcpClient)
        {
            try
            {
                var stream = tcpClient.GetStream();
                int bytes = 0;
                var responseByte = new List<byte>();
                while ((bytes = stream.ReadByte()) != '\n') 
                {
                    responseByte.Add((byte)bytes);
                    if (bytes == -1) break;
                }

                var response = Encoding.UTF8.GetString(responseByte.ToArray());

                var responseArray = response.ToString().Split(" ");
                if(checkResponse(responseArray))
                {
                    string message = "ok"+ '\n';
                    byte[] requestData = Encoding.UTF8.GetBytes(message);
                    stream.Write(requestData, 0, requestData.Length);
                }
                stream.Close();
            }
            catch (Exception e) { }
            tcpClient.Close();
        }
        private bool checkResponse(string[] responseArray)
        {
            bool bReturn = false;
            if (responseArray.Length == 4)
            {
                string[] response = new string[3];
                var message = string.Join(" ", responseArray.SkipLast(1));
                if (responseArray[responseArray.Length - 1] == SHA256Checksum.Calculate(message))
                {
                    bReturn = true;
                } 
            }

            return bReturn;        
        }


    }
}