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
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Btn_Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Global.getInstance().socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress IP = IPAddress.Parse(Tb_ServerAddr.Text.Trim());
                Global.getInstance().socketServer.Connect(IP, Convert.ToInt32(Tb_ServerPort.Text.Trim()));
                Global.getInstance().NickName = Tb_Nickname.Text;
                GroupChatWindow window = new GroupChatWindow();
                window.Show();
                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Connectint Error: " + ex.ToString());
            }
        }
    }
}
