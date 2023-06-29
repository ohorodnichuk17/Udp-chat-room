using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace chat_client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient? client = null;
        public bool IsConnected => client != null && client.Connected;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Listen()
        {
            while (IsConnected)
            {
                try
                {
                    var stream = client.GetStream();
                    var formatter = new BinaryFormatter();
                    var request = (ClientCommand)formatter.Deserialize(stream);

                    // invoke method in main thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = $"[{request.Timestamp.ToShortTimeString()}] {request.UserName}: {request.Text}";

                        if (request.Type == CommandType.Like)
                            msgList.Items.Add("👍 " + message);
                        else if (request.Type == CommandType.Dislike)
                            msgList.Items.Add("👎 " + message);
                        else
                            msgList.Items.Add(message);
                    });
                }
                catch { }
            }
        }

        private void SendBtnClick(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                MessageBox.Show("You must connect to the chat before!");
                return;
            }
            string text = msgTxtBox.Text;
            SendMessage(new ClientCommand(text));
        }

        private void LikeBtnClick(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                MessageBox.Show("You must connect to the chat before!");
                return;
            }
            SendMessage(new ClientCommand(CommandType.Like));
        }

        private void DislikeBtnClick(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                MessageBox.Show("You must connect to the chat before!");
                return;
            }
            SendMessage(new ClientCommand(CommandType.Dislike));
        }


        private void JoinBtnClick(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                MessageBox.Show("You are already connected to the chat!");
                return;
            }

            client = new();
            IPEndPoint serverIp = new(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
            client.Connect(serverIp);

            SendMessage(new ClientCommand(CommandType.Join));

            Task.Run(() => Listen());
        }

        private void LeaveBtnClick(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                MessageBox.Show("You must connect to the chat before!");
                return;
            }
            SendMessage(new ClientCommand(CommandType.Leave));

            client.Close();
            client = null;
        }

        private void SendMessage(ClientCommand command)
        {
            NetworkStream ns = client.GetStream();

            // ns.Write() - send data to receiver
            // ns.Read()  - receive data from sender

            command.UserName = Environment.UserName;
            command.Timestamp = DateTime.Now;

            var formatter = new BinaryFormatter();
            formatter.Serialize(ns, command);   // send data to buffer

            ns.Flush();                         // clear buffer and send all data
        }
    }
}
