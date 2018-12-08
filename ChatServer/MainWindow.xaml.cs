using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ChatServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread AcceptSocketThread;
        private Socket socketSend;
        private Socket socketWatch;
        private Dictionary<string, Socket> dictSocket = new Dictionary<string, Socket>();
        private List<User> Users = new List<User>();
        public MainWindow()
        {
            InitializeComponent();

            string name = Dns.GetHostName();
            foreach (IPAddress ipa in Dns.GetHostAddresses(name))
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    Lb_IP.Items.Add(ipa.ToString());
                }
            }
        }

        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            IPEndPoint port = new IPEndPoint(ip, Convert.ToInt32(Tb_Port.Text));
            socketWatch.Bind(port);
            socketWatch.Listen(10);
            AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen))
            {
                IsBackground = true
            };
            AcceptSocketThread.Start(socketWatch);
            Btn_Start.IsEnabled = false;
            Btn_Stop.IsEnabled = true;
        }

        private void StartListen(object obj)
        {
            Socket watch = obj as Socket;
            while (true)
            {
                socketSend = watch.Accept();
                string IP = socketSend.RemoteEndPoint.ToString();
                dictSocket.Add(IP, socketSend);
                string message = DateTime.Now.ToString();
                message += ": 用户" + IP + "连接";
                Dispatcher.Invoke(new dg_AddLog(AddLog), message);

                Thread ReceiveThread = new Thread(new ParameterizedThreadStart(Receive))
                {
                    IsBackground = true
                };
                ReceiveThread.Start(socketSend);
            }
        }

        private void BroadCast(byte[] buffer)
        {
            foreach (User user in Users)
            {
                Forward(buffer, user.IP);
            }
        }

        private void Forward(byte[] buffer, string IP)
        {
            Socket send = dictSocket[IP];
            if (send != null)
            {
                try
                {
                    send.Send(buffer);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new dg_AddLog(AddLog), ex.ToString());
                }
            }
        }

        private void SyncUserList()
        {
            Lb_User.ItemsSource = null;
            Lb_User.ItemsSource = Users;

            UserListObj obj = new UserListObj(Users, USERLISTSYNC);
            string str = JsonConvert.SerializeObject(obj);
            List<byte> bytes = new List<byte>
                {
                    Convert.ToByte(TEXT)
                };
            byte[] header = Encoding.UTF8.GetBytes(str);
            bytes.AddRange(BitConverter.GetBytes(header.Length));
            bytes.AddRange(header);
            byte[] buffer = bytes.ToArray();
            BroadCast(buffer);
        }

        private void Receive(object obj)
        {
            Socket recv = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 + 2048];
                    int count = recv.Receive(buffer);
                    if (count == 0)
                        break;
                    //switch (buffer[0])
                    //{
                    //    case TEXT:
                    int offset = BitConverter.ToInt32(buffer, 0);
                    string str = Encoding.Default.GetString(buffer, 4, offset);
                    JObject jobj = (JObject)JsonConvert.DeserializeObject(str);
                    string message = DateTime.Now.ToString() + ": ";
                    string remoteIP = recv.RemoteEndPoint.ToString();
                    switch (jobj["action"].ToObject<int>())
                    {
                        case LOGIN:
                            Users.Add(new User(remoteIP, jobj["NickName"].ToString()));
                            message += "用户" + remoteIP + "登录";
                            Dispatcher.Invoke(new dg_SyncUserList(SyncUserList));
                            break;
                        case LOGOUT:
                            Users.RemoveAll((user) => user.IP.Equals(remoteIP));
                            message += "用户" + remoteIP + "退出登录";
                            Dispatcher.Invoke(new dg_SyncUserList(SyncUserList));
                            break;
                        case GROUPCHAT:
                            BroadCast(buffer);
                            message += "用户" + remoteIP + "群发消息";
                            break;
                        case SINGLECHAT:
                            {
                                string target = jobj["target"].ToString();
                                Forward(buffer, target);
                                message += "用户" + remoteIP + "向用户" + target + "发送消息";
                            }
                            break;
                        case SENDFILE:
                            {
                            //    Console.WriteLine(offset);
                                string target = jobj["target"].ToString();
                                Forward(buffer, target);
                                message += "用户" + remoteIP + "向用户" + target + "发送文件";
                            }
                            break;

                        default:
                            message = "";
                            break;
                    }
                    Dispatcher.Invoke(new dg_AddLog(AddLog), message);
                    //       break;
                    //}
                }
            }
            catch (SocketException)
            {
                string remoteIP = recv.RemoteEndPoint.ToString();
                Users.RemoveAll((user) => user.IP.Equals(remoteIP));
                string message = DateTime.Now.ToString() + ": ";
                message += "用户" + remoteIP + "退出登录";
                Dispatcher.Invoke(new dg_SyncUserList(SyncUserList));
                Dispatcher.Invoke(new dg_AddLog(AddLog), message);
            }
        }

        private class UserListObj
        {
            public List<User> Users;
            public int action;

            public UserListObj(List<User> Users, int action)
            {
                this.Users = Users;
                this.action = action;
            }
        }

        public class User
        {
            public string IP { get; set; }
            public string NickName { get; set; }

            public User(string iP, string nickName)
            {
                IP = iP;
                NickName = nickName;
            }
        }

        private delegate void dg_SyncUserList();


        private void AddLog(string Message)
        {
            Tb_Log.Text = Tb_Log.Text + Message + "\n";
            Tb_Log.ScrollToEnd();
        }
        private delegate void dg_AddLog(string Message);

        private void Btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            CloseSocket();
            Btn_Start.IsEnabled = true;
            Btn_Stop.IsEnabled = false;
            Users.Clear();
            SyncUserList();
        }
        private void CloseSocket()
        {
            if (socketWatch != null)
                socketWatch.Close();
            if (socketSend != null)
                socketSend.Close();
            if (AcceptSocketThread != null)
                AcceptSocketThread.Abort();
        }
        private void Btn_Send_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseSocket();
        }

        private const int LOGIN = 0;
        private const int LOGOUT = 1;
        private const int GROUPCHAT = 2;
        private const int SINGLECHAT = 3;
        private const int USERLISTSYNC = 4;
        public const int SENDFILE = 5;

        private const int TEXT = 0;
        private const int FILE = 1;
        private const int PICTURE = 2;
        private const int VOICE = 3;
    }

}
