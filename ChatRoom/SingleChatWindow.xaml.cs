using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
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
        }

        public void SetNickName(string nickname)
        {
            NickName = nickname;
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
            instance.SendMessage(message);
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

        public void RefreshProgress(double progress)
        {
            Dispatcher.Invoke(new dg_RecvFileProgress(RecvFileProgress), progress);
        }

        private void RecvFileProgress(double progress)
        {
            if (progress >= 1.0) 
            {
                MessageBox.Show("文件传输完成！");
                Pb_File.Visibility = Visibility.Hidden;
                return;
            }
            Pb_File.Visibility = Visibility.Visible;
            Pb_File.Value = progress;
        }

        private delegate void dg_RecvFileProgress(double progress);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            instance.singleWindows.Remove(this);
        }

        private void Btn_File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "All Files(*.*)|*.*",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true) 
            {
                string strPath = openFileDialog.FileName;
                Thread t = new Thread(new ParameterizedThreadStart(SendFile))
                {
                    IsBackground = true
                };
                t.Start(strPath);
            }
        }

        private void SendFile(object obj)
        {
            string strPath = obj as string;
            FileInfo file = new FileInfo(strPath);
            long filelength = file.Length;
            /*if (filelength > 1024 * 1024) 
            {
                MessageBox.Show("仅可传输小于1MiB的文件！");
                return;
            }*/
            long sendlength = 0;
            string filename = file.Name;
            FileInfoMessage fileMsg = new FileInfoMessage(filename, filelength, RemoteIP);
            using (FileStream fs = new FileStream(strPath, FileMode.Open, FileAccess.Read))
            {
                while (sendlength < filelength) 
                {
                    long size = Math.Min(filelength - sendlength, 1024 * 1024);
                    byte[] buffer = new byte[size];
                    fs.Read(buffer, 0, buffer.Length);
                    instance.SendBinary(fileMsg, buffer);
                    sendlength += buffer.Length;
                    //Console.WriteLine((double)sendlength / filelength);
                    RefreshProgress((double)sendlength / filelength);
                }
            }
        }

        private void Btn_Image_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("尚未开发，敬请期待！");
        }

        private void Btn_Talk_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("尚未开发，敬请期待！");
        }
    }
    
}
