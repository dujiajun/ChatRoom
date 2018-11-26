using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace ChatRoom
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private Global instance = Global.GetInstance();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Btn_Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                instance.socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress IP = IPAddress.Parse(Tb_ServerAddr.Text.Trim());
                instance.socketServer.Connect(IP, Convert.ToInt32(Tb_ServerPort.Text.Trim()));
                instance.NickName = Tb_Nickname.Text;
                instance.Login();
                instance.StartReceive();
                GroupChatWindow window = new GroupChatWindow();
                window.Show();
                Close();
            }
            catch(Exception)
            {
                MessageBox.Show("服务器连接失败！");
            }
        }
    }
}
