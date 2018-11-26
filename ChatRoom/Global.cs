using System.Net.Sockets;

namespace ChatRoom
{
    class Global
    {
        private static Global instance;

        private Global()
        {
            
        }

        public static Global getInstance()
        {
            if(instance==null)
                instance = new Global();
            return instance;
        }

        public Socket socketServer;

        public string NickName;
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
        public string targetIP;

        public TextMessage(string iP, string time, int action, string content)
        {
            IP = iP;
            this.time = time;
            this.action = action;
            this.content = content;
        }

        public TextMessage(string iP, string time, int action, string content, string targetIP) : this(iP, time, action, content)
        {
            this.targetIP = targetIP;
        }
    }

    public class LoginObj
    {
        public string IP;
        public string NickName;
        public int action;

        public LoginObj(string iP, string NickName, int action)
        {
            IP = iP;
            this.NickName = NickName;
            this.action = action;
        }
    }
}
