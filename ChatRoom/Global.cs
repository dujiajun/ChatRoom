using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ChatRoom
{
    class Global
    {
        private static Global instance;

        private Global()
        {
            
        }

        public static Global GetInstance()
        {
            if(instance==null)
                instance = new Global();
            return instance;
        }

        public Socket socketServer;

        public string NickName;

        public Thread ReceiveThread;

        public void StartReceive()
        {
            ReceiveThread = new Thread(new ParameterizedThreadStart(Receive))
            {
                IsBackground = true
            };
            ReceiveThread.Start(socketServer);
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
                                    {
                                        string remoteIP = jobj["IP"].ToString();
                                        string nickname = Users.Find(x => x.IP.Equals(remoteIP)).NickName;
                                        string msg = GetFormattedMessage(nickname, jobj["time"].ToString(), jobj["content"].ToString());
                                        groupChatWindow.NewMessage(msg);
                                    }
                                    break;
                                case USERLISTSYNC:
                                    {
                                        Users.Clear();
                                        Users = jobj["Users"].ToObject<List<User>>();
                                        groupChatWindow.Sync();
                                    }
                                    break;
                                case SINGLECHAT:
                                    {
                                        string remoteIP = jobj["IP"].ToString();
                                        string nickname = Users.Find(x => x.IP.Equals(remoteIP)).NickName;
                                        string content = jobj["content"].ToString();
                                        string msg = GetFormattedMessage(nickname, jobj["time"].ToString(), jobj["content"].ToString());
                                        SingleChatWindow window = singleWindows.Find(win => win.RemoteIP.Equals(remoteIP));
                                        if (window == null)
                                        {
                                            groupChatWindow.NewSingleChatWindow(remoteIP, NickName, msg);
                                        }
                                        else
                                        {
                                            window.NewMessage(msg);
                                        }
                                    }   
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
                groupChatWindow.CloseAllWindow();
            }
        }

        public static string GetFormattedMessage(string nickname,string time,string content)
        {
            string msg = nickname + " " + time + "\n" + content;
            return msg;
        }

        public GroupChatWindow groupChatWindow;
        public List<SingleChatWindow> singleWindows = new List<SingleChatWindow>();

        public List<User> Users = new List<User>();

        public const int LOGIN = 0;
        public const int LOGOUT = 1;
        public const int GROUPCHAT = 2;
        public const int SINGLECHAT = 3;
        public const int USERLISTSYNC = 4;

        public const int TEXT = 0;
        public const int FILE = 1;
        public const int PICTURE = 2;
        public const int VOICE = 3;

        public bool SendMessage(object obj, int type)
        {
            try
            {
                string str = JsonConvert.SerializeObject(obj);

                List<byte> bytes = new List<byte>
                {
                    Convert.ToByte(type)
                };
                bytes.AddRange(Encoding.UTF8.GetBytes(str));
                byte[] buffer = bytes.ToArray();
                socketServer.Send(buffer);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void Logout()
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

        public void Login()
        {
            LoginMessage login = new LoginMessage(socketServer.LocalEndPoint.ToString(), GetInstance().NickName, LOGIN);
            SendMessage(login, TEXT);
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

    public class TextMessage
    {
        public string IP;
        public string time;
        public int action;
        public string content;
        public string target;

        public TextMessage(int action, string content)
        {
            this.IP = Global.GetInstance().socketServer.LocalEndPoint.ToString();
            this.time = DateTime.Now.ToString();
            this.action = action;
            this.content = content;
        }

        public TextMessage(int action, string content, string target) : this(action, content)
        {
            this.target = target;
        }
    }

    public class LoginMessage
    {
        public string IP;
        public string NickName;
        public int action;

        public LoginMessage(string iP, string NickName, int action)
        {
            IP = iP;
            this.NickName = NickName;
            this.action = action;
        }
    }
}
