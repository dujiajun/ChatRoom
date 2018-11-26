using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatRoom
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GroupChatWindow : Window
    {
        private Global instance = Global.GetInstance();
        private Socket socketServer = Global.GetInstance().socketServer;

        public GroupChatWindow()
        {
            InitializeComponent();

            instance.groupChatWindow = this;
        }

        public void NewMessage(string msg)
        {
            Dispatcher.Invoke(new dg_AddNewMessage(AddNewMessage), msg);
        }

        public void Sync()
        {
            Dispatcher.Invoke(new dg_SyncUserList(SyncUserList));
        }

        private void SyncUserList()
        {
            Lb_Member.ItemsSource = null;
            Lb_Member.ItemsSource = Global.GetInstance().Users;
        }

        private delegate void dg_SyncUserList();

        private void AddNewMessage(string message)
        {
            Lb_Msg.Items.Add(message);
        }

        private delegate void dg_AddNewMessage(string message);

        private void Btn_Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Tb_Send.Text.Equals(""))
                {
                    MessageBox.Show("请输入聊天内容！");
                    return;
                }
                TextMessage message = new TextMessage(Global.GROUPCHAT, Tb_Send.Text);
                instance.SendMessage(message, Global.TEXT);
                Tb_Send.Text = "";
                Tb_Send.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void GroupChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (SingleChatWindow window in instance.singleWindows)
                window.Close();
            instance.Logout();
        }

        private void Btn_Image_Click(object sender, RoutedEventArgs e)
        {
            instance.Logout();
            LoginWindow window = new LoginWindow();
            window.Show();
            Close();

        }

        private void ListBoxItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            User user = (User)Lb_Member.SelectedItem;
            SingleChatWindow window = instance.singleWindows.Find(x => x.RemoteIP.Equals(user.IP));
            if (window != null) 
            {
                window.Focus();
                return;
            }
            else
            {
                window = new SingleChatWindow()
                {
                    RemoteIP = user.IP
                };
                window.NickName = user.NickName;
                instance.singleWindows.Add(window);
                window.Show();
            }
        }

        public void NewSingleChatWindow(string remoteIP, string NickName, string msg)
        {
            Dispatcher.Invoke(new dg_NewSingleChatWindow(NewSingleChat), remoteIP, NickName,msg);
        }

        private delegate void dg_NewSingleChatWindow(string remoteIP, string NickName,string msg);
        private void NewSingleChat(string remoteIP, string NickName,string msg)
        {
            SingleChatWindow window = new SingleChatWindow
            {
                RemoteIP = remoteIP
            };
            window.NickName = NickName;
            instance.singleWindows.Add(window);
            window.Show();
            window.NewMessage(msg);
        }

        public void CloseAllWindow()
        {
            Dispatcher.Invoke(new dg_CloseAllWindow(CloseAll));
        }

        private delegate void dg_CloseAllWindow();

        private void CloseAll()
        {
            foreach (SingleChatWindow window in instance.singleWindows)
                window.Close();
            Close();
        }
    }
}
