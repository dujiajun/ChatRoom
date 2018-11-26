using System.Net.Sockets;
using System.Windows;

namespace ChatRoom
{
    /// <summary>
    /// SingleChatWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SingleChatWindow : Window
    {
        private Global instance = Global.GetInstance();
        private Socket socketServer = Global.GetInstance().socketServer;

        public string RemoteIP;
        public string NickName;

        public SingleChatWindow()
        {
            InitializeComponent();

            Title = "正在和" + NickName + "聊天";
        }

        private void Btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (Tb_Send.Text.Equals(""))
            {
                MessageBox.Show("请输入聊天内容！");
                return;
            }
            TextMessage message = new TextMessage(Global.SINGLECHAT, Tb_Send.Text, RemoteIP);
            instance.SendMessage(message, Global.TEXT);
            AddNewMessage(Global.GetFormattedMessage(instance.NickName, message.time, message.content));
            Tb_Send.Text = "";
            Tb_Send.Focus();
        }

        public void NewMessage(string msg)
        {
            Dispatcher.Invoke(new dg_AddNewMessage(AddNewMessage), msg);
        }

        private void AddNewMessage(string message)
        {
            Lb_Msg.Items.Add(message);
            Focus();
        }

        private delegate void dg_AddNewMessage(string message);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            instance.singleWindows.Remove(this);
        }
    }
}
