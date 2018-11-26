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
    public partial class GroupChatWindow: Window
    {
        private Socket socketServer = Global.getInstance().socketServer;
        private List<User> Users = new List<User>();
        private Thread ReceiveThread;

        public GroupChatWindow()
        {
            InitializeComponent();

            ReceiveThread = new Thread(new ParameterizedThreadStart(Receive))
            {
                IsBackground = true
            };
            ReceiveThread.Start(socketServer);

            Login();
        }

        private void Receive(object obj)
        {
            Socket recv = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[2048];
                    int count = recv.Receive(buffer);
                    if (count == 0)
                        break;
                    switch (buffer[0])
                    {
                        case TEXT:
                            string str = Encoding.Default.GetString(buffer, 1, count - 1);
                            JObject jobj = (JObject)JsonConvert.DeserializeObject(str);

                            switch (jobj["action"].ToObject<int>())
                            {
                                case GROUPCHAT:
                                    string remoteIP = jobj["IP"].ToString();
                                    string msg = remoteIP + " " + jobj["time"].ToString() + "\n" + jobj["content"].ToString();
                                    Dispatcher.Invoke(new dg_AddNewMessage(AddNewMessage), msg);
                                    break;
                                case USERLISTSYNC:
                                    Users.Clear();
                                    Users = jobj["Users"].ToObject<List<User>>();
                                    Dispatcher.Invoke(new dg_SyncUserList(SyncUserList));
                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }

                }
            }
            catch (SocketException)
            {
                MessageBox.Show("服务器终止连接！");
                LoginWindow window = new LoginWindow();
                window.Show();
                Close();
            }
        }

        private void SyncUserList()
        {
            Lb_Member.ItemsSource = null;
            Lb_Member.ItemsSource = Users;
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
                TextMessage message = new TextMessage(socketServer.LocalEndPoint.ToString(), System.DateTime.Now.ToString(), GROUPCHAT, Tb_Send.Text);
                string str = JsonConvert.SerializeObject(message);
                List<byte> bytes = new List<byte>
                {
                    Convert.ToByte(TEXT)
                };
                bytes.AddRange(Encoding.UTF8.GetBytes(str));
                byte[] buffer = bytes.ToArray();
                socketServer.Send(buffer);
                Tb_Send.Text = "";
                Tb_Send.Focus();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
        }

        private void GroupChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logout();
        }

        private void Login()
        {
            LoginObj login = new LoginObj(socketServer.LocalEndPoint.ToString(),Global.getInstance().NickName,LOGIN);
            string str = JsonConvert.SerializeObject(login);
            List<byte> bytes = new List<byte>
                {
                    Convert.ToByte(TEXT)
                };
            bytes.AddRange(Encoding.UTF8.GetBytes(str));
            byte[] buffer = bytes.ToArray();
            socketServer.Send(buffer);
        }

        private void Logout()
        {
            try
            {
                if (socketServer != null)
                    socketServer.Close();
                if (ReceiveThread != null)
                    ReceiveThread.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private const int LOGIN = 0;
        private const int LOGOUT = 1;
        private const int GROUPCHAT = 2;
        private const int SINGLECHAT = 3;
        private const int USERLISTSYNC = 4;

        private const int TEXT = 0;
        private const int FILE = 1;
        private const int PICTURE = 2;
        private const int VOICE = 3;

        private void Btn_Image_Click(object sender, RoutedEventArgs e)
        {
            Logout();
            LoginWindow window = new LoginWindow();
            window.Show();
            Close();
        }

        private void ListBoxItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            User user = (User)Lb_Member.SelectedItem;
            MessageBox.Show(user.IP);
        }

    }
}
