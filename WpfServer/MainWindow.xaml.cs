using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        object locker = new();
        ApplicationContext db = new ApplicationContext();
        Dictionary<string, string> IDcontrol = new Dictionary<string, string>();
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Task.Run(vServerStart);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            db.Database.EnsureCreated();
            db.Information.Load();
            DataContext = db.Information.Local.ToObservableCollection();
            dataGrid.ItemsSource = db.Information.Local.ToBindingList();
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            db.Dispose();
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
                if(checkResponseAndSaveDB(responseArray))
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
        private bool checkResponseAndSaveDB(string[] responseArray)
        {
            bool bReturn = false;
            if (responseArray.Length == 6)
            {
                var message = string.Join(" ", responseArray.SkipLast(1));
                if (responseArray[responseArray.Length - 1] == SHA256Checksum.Calculate(message))
                {
                    bReturn = true;
                }
                if (sWorkWithIDcontrol(responseArray[0],"",0)=="ok")
                {
                    if (responseArray[responseArray.Length - 1] == sWorkWithIDcontrol(responseArray[0], "", 1))
                        return true;
                }
            }
            if (bReturn)
            {
                bReturn = false;
                Data data = new Data(responseArray[1], int.Parse(responseArray[2]));
                Data? dataOld = db.Information.Find(data.ProductCode);

                if (dataOld != null)
                {
                    dataOld.Amount += data.Amount;
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        db.Information.Add(data);
                    });
                }
                db.SaveChanges();
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    dataGrid.Items.Refresh();
                });
                sWorkWithIDcontrol(responseArray[0], responseArray[responseArray.Length - 1], 2);
             
                bReturn = true;
            }

            return bReturn;        
        }
        private string sWorkWithIDcontrol(string sKey,string sValue, int iCase)
        {
            lock (locker)
            {
                switch (iCase)
                {
                    case 0:
                        if (IDcontrol.ContainsKey(sKey)) return "ok";
                        break;
                    case 1:
                        return IDcontrol[sKey];
                    case 2:
                        bool bTryAdd = IDcontrol.TryAdd(sKey, sValue);
                        if (!bTryAdd) {  IDcontrol[sKey]= sValue; }
                        break;
                }
                return "";
            }
        }

    }
}