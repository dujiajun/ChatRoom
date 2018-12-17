using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
            if (instance == null)
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
            string filename = "";
            long filelength = 0;
            long recvlength = 0;
            SingleChatWindow window = null;
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 + 2048];
                    int count = recv.Receive(buffer);

                    memoryStream.Write(buffer, 0, count);
                    buffer = memoryStream.ToArray();

                    if (count == 0)
                        break;

                    int startIndex = 0;
                    while(true)
                    {
                        int totalLength = 0, headerLength = 0;
                        if (buffer.Length - startIndex < 4) 
                            totalLength = -1;
                        else
                            totalLength = BitConverter.ToInt32(buffer, startIndex);

                        if ((buffer.Length - startIndex < (totalLength + 8)) || totalLength == -1) 
                        {
                            memoryStream.Close();
                            memoryStream.Dispose();
                            memoryStream = new MemoryStream();
                            memoryStream.Write(buffer, startIndex, buffer.Length - startIndex);
                            break;
                        }

                        headerLength = BitConverter.ToInt32(buffer, startIndex + 4);

                        string header = Encoding.Default.GetString(buffer, startIndex + 8, headerLength);
                        JObject jobj = (JObject)JsonConvert.DeserializeObject(header);

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
                                    window = singleWindows.Find(win => win.RemoteIP.Equals(remoteIP));
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
                            case SENDFILE:
                                {
                                    string remoteIP = jobj["IP"].ToString();
                                    filename = jobj["filename"].ToString();
                                    filelength = jobj["filelength"].ToObject<long>();
                                    if (recvlength == 0)
                                    {
                                        if (File.Exists(filename))
                                            File.Delete(filename);
                                    }
                                    int size = totalLength - headerLength; //BitConverter.ToInt32(buffer, 4 + headerLength);
                                    using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write))
                                    {
                                        fs.Write(buffer, startIndex + 8 + headerLength, size);
                                        recvlength += size;
                                    }
                                    window = singleWindows.Find(win => win.RemoteIP.Equals(remoteIP));
                                    if (window == null)
                                    {
                                        groupChatWindow.NewSingleChatWindow(remoteIP, NickName, string.Empty);
                                    }
                                    else
                                    {
                                        window.RefreshProgress((double)recvlength / filelength);
                                    }
                                    if (recvlength == filelength)
                                    {
                                        recvlength = 0;
                                    }
                                }
                                
                                break;
                            default:
                                break;
                        }
                        startIndex += totalLength + 8;
                    }
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("服务器终止连接！");
                groupChatWindow.CloseAllWindow();
            }
        }

        public static string GetFormattedMessage(string nickname, string time, string content)
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
        public const int SENDFILE = 5;

        public const int TEXT = 0;
        public const int FILE = 1;
        public const int PICTURE = 2;
        public const int VOICE = 3;

        public bool SendMessage(object obj)
        {
            try
            {
                string str_header = JsonConvert.SerializeObject(obj);
                List<byte> bytes = new List<byte>();
                byte[] byte_header = Encoding.Default.GetBytes(str_header);
                bytes.AddRange(BitConverter.GetBytes(byte_header.Length));
                bytes.AddRange(BitConverter.GetBytes(byte_header.Length));
                bytes.AddRange(byte_header);
                byte[] buffer = bytes.ToArray();
                socketServer.Send(buffer);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool SendBinary(object fileMsg, byte[] filebyte)
        {
            try
            {
                List<byte> bytes = new List<byte>();
                string str_header = JsonConvert.SerializeObject(fileMsg);
                byte[] byte_header = Encoding.Default.GetBytes(str_header);
                bytes.AddRange(BitConverter.GetBytes(filebyte.Length + byte_header.Length));
                bytes.AddRange(BitConverter.GetBytes(byte_header.Length));
                bytes.AddRange(byte_header);
                bytes.AddRange(filebyte);
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
            LoginMessage login = new LoginMessage(GetInstance().NickName);
            SendMessage(login);
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

        public LoginMessage(string NickName)
        {
            this.IP = Global.GetInstance().socketServer.LocalEndPoint.ToString();
            this.NickName = NickName;
            this.action = Global.LOGIN;
        }
    }

    public class FileInfoMessage
    {
        public string IP;
        public string time;
        public int action;
        public string filename;
        public long filelength;
        public string target;

        public FileInfoMessage(string filename, long filelength, string target)
        {
            this.IP = Global.GetInstance().socketServer.LocalEndPoint.ToString();
            this.time = DateTime.Now.ToString();
            this.filename = filename;
            this.filelength = filelength;
            this.target = target;
            this.action = Global.SENDFILE;
        }
    }
}
