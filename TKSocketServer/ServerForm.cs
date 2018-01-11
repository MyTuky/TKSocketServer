using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TKSocketServer
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            InitializeComponent();
        }
        void ShowMsg(string str)//给消息文本框加文本的方法
        {
            txtLog.AppendText(str + "\r\n");
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = IPAddress.Any;//创建IP//Any 字段等效于以点分隔的四部分表示法格式的 0.0.0.0 这个IP地址，实际是一个广播地址。//对于SOCKET而言，使用 any ，表示，侦听本机的所有IP地址的对应的端口（本机可能有多个IP或只有一个IP）
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(txtPort.Text));//创建终结点（EndPoint）             
                Socket socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//建立监视的Socket
                socketWatch.Bind(point);//使得Socket绑定Bind()端口，参数为EndPoint//监听
                Console.WriteLine("监听成功");
                ShowMsg("监听成功");
                socketWatch.Listen(0);//参数是监听的最大长度，0是无限
                Thread th = new Thread(Listen);//新建线程,运行soketWatch(),这里的Listen是自定义的方法
                th.IsBackground = true;//线程为后台属性
                th.Start(socketWatch);//提供线程要执行的方法的要使用的数据（参数）的对象
            }
            catch
            { }
        }
        Socket socketSend;//声明 socketSend 用于等待客户端的连接 并且创建与之通信用的SocketSend，//等客户端连接//接受到client连接，为此连接建立新的socket，并接受信息
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();//将远程连接的客户端的IP地址和Socket存入集合中
        void Listen(object o)//被线程执行的函数，用于Accept()新建Socket，把每次的新建Socket，添加RemoteEndPoint到Dic集合，添加到cbo下拉列表框，有参数的话，必须是object类型
        {
            Socket socketWatch = o as Socket;//as 强转语句,object o参数强转为Socket类型
            while (true)
            {
                try
                {
                    socketSend = socketWatch.Accept();//等待客户端的连接 并且创建一个负责通信的Socket             
                    dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);//将远程连接的客户端的IP地址和Socket存入集合中
                    cboUsers.Items.Add(socketSend.RemoteEndPoint.ToString());//将远程连接的客户端的IP地址和端口号存储下拉框中
                    ShowMsg(socketSend.RemoteEndPoint.ToString() + ":" + "连接成功");//192.168.11.78：连接成功
                    Thread th = new Thread(Recive);//开启 一个新线程不停的接受客户端发送过来的消息
                    th.IsBackground = true;
                    th.Start(socketSend);
                }
                catch
                { }
            }
        }
        void Recive(object o)//被线程执行的函数，用于服务器端不停的接受客户端发送过来的消息，并打印出来
        {
            Socket socketSend = o as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器应该接受客户端发来的消息
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    //实际接受到的有效字节数
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    ShowMsg(socketSend.RemoteEndPoint + ":" + str);
                }
                catch
                { }
            }
        }
        private void btnSelect_Click(object sender, EventArgs e)// 选择要发送的文件
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = @"C:\Users\SpringRain\Desktop";
            ofd.Title = "请选择要发送的文件";
            ofd.Filter = "所有文件|*.*";
            ofd.ShowDialog();

            txtPath.Text = ofd.FileName;
        }
        //自定义协议，在传递的字节数组前面加上一个字节作为标识，0表示文字，1表示文件,2表示震动
        private void btnSend_Click(object sender, EventArgs e)//服务器给客户端发送消息
        {
            string str = txtMsg.Text;//消息框文本
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
            List<byte> list = new List<byte>();//存入list
            list.Add(0);//是自定义协议，在传递的字节数组前面加上一个字节作为标识，0表示文字，1表示文件
            list.AddRange(buffer);//添加buffer数组进集合list
            byte[] newBuffer = list.ToArray();//将泛型集合转换为数组
            string ip = cboUsers.SelectedItem.ToString(); //获得用户在下拉框中选中的IP地址
            dicSocket[ip].Send(newBuffer);//Socket.Send()将数据发送到连接的Socket//socketSend.Send(buffer); 
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            //获得要发送文件的路径
            string path = txtPath.Text;
            using (FileStream fsRead = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024 * 1024 * 5];
                int r = fsRead.Read(buffer, 0, buffer.Length);
                List<byte> list = new List<byte>();
                list.Add(1);
                list.AddRange(buffer);
                byte[] newBuffer = list.ToArray();
                dicSocket[cboUsers.SelectedItem.ToString()].Send(newBuffer, 0, r + 1, SocketFlags.None);
            }
        }


        /// <summary>
        /// 发送震动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnZD_Click(object sender, EventArgs e)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 2;
            dicSocket[cboUsers.SelectedItem.ToString()].Send(buffer);
        }

    }
}
