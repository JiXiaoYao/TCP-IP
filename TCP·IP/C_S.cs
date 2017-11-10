using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace TCP_IP
{
    //

    /// <summary>
    /// 服务端
    /// </summary>
    public class Server
    {
        #region 变量
        #region Tcp连接套接字储存字典
        // Tcp连接套接字储存字典结构示意图：
        // SocketIPEndPointDict(Dictionary)
        // ----IP:Port(IPEndPoint)
        // ----SocketIdDict(Dictionary)
        //     ----SocketID(int)
        //     ----Sockrt(TcpSocket)
        /// <summary>
        /// 套接字储存字典
        /// </summary>
        Dictionary<int, Socket> SocketIdDict = new Dictionary<int, Socket>();
        /// <summary>
        /// 以网络终结点为key的套接字储存字典的储存字典
        /// </summary>
        Dictionary<IPEndPoint, Dictionary<int, Socket>> SocketIPEndPointDict = new Dictionary<IPEndPoint, Dictionary<int, Socket>>();
        #endregion
        #region 委托
        /// <summary>
        /// 客户端消息事件委托
        /// </summary>
        /// <param name="ListenIPEndPoint">连接的网络终结点</param>
        /// <param name="SocketDictID">套接字ID</param>
        /// <param name="MessagesArray">消息内容数组</param>
        /// <param name="MessagesLength">消息的长度</param>
        public delegate void ClientMessagesDelegate(IPEndPoint ListenIPEndPoint, int SocketDictID, byte[] MessagesArray, int MessagesLength);
        /// <summary>
        /// 客户端消息事件委托
        /// </summary>
        ClientMessagesDelegate ClientMessages;
        /// <summary>
        /// Tcp套接字终止连接事件委托
        /// </summary>
        /// <param name="ListenIPEndPoint">连接的网络终结点</param>
        /// <param name="SocketDictID">套接字ID</param>
        public delegate void ConnentStopDelegate(IPEndPoint ListenIPEndPoint, int SocketDictID);
        /// <summary>
        /// 客户端消息事件委托
        /// </summary>
        ConnentStopDelegate ConnentStop;
        #endregion
        /// <summary>
        /// 侦听线程
        /// </summary>
        Dictionary<IPEndPoint, Thread> ListenThreadDict = new Dictionary<IPEndPoint, Thread>();
        /// <summary>
        /// 端口侦听套接字
        /// </summary>
        Dictionary<IPEndPoint, Socket> ListenSocketDict = new Dictionary<IPEndPoint, Socket>();
        IPAddress[] ListenIP;                                                                      // 侦听IP地址集
        int[] ListenPort;                                                                          // 侦听端口集
        ListenMode ServerMode;                                                                     // 侦听模式
        int MessageSize;                                                                           // 接收消息缓存区大小
        #endregion
        #region 构造函数
        /// <summary>
        /// 单端口多IP模式
        /// 输出无连接端口标识
        /// </summary>
        /// <param name="IpArray">IP地址数组</param>
        /// <param name="Port">端口</param>
        /// <param name="ServerMessageSize">接收消息缓存区大小</param>
        public Server(IPAddress[] IpArray, int Port, int ServerMessageSize)
        {
            ServerMode = ListenMode.Mode1;
            ListenIP = IpArray;
            ListenPort[0] = Port;
            MessageSize = ServerMessageSize;
        }
        #region 重载
        /// <summary>
        /// 多端口多IP模式
        /// 输出标识为：监听IP:监听端口
        /// </summary>
        /// <param name="IpArray">IP地址数组</param>
        /// <param name="Port">端口集</param>
        /// <param name="ServerMessageSize">接收消息缓存区大小</param>
        public Server(IPAddress[] IpArray, int[] Port, int ServerMessageSize)
        {
            ServerMode = ListenMode.Mode2;
            ListenIP = IpArray;
            ListenPort = Port;
        }
        #endregion
        #endregion
        /// <summary>
        /// 开始侦听端口
        /// </summary>
        public void StartListen()
        {
            if (ServerMode == ListenMode.Mode1)
            {
                for (int i = 0; i < ListenIP.Length; i = i + 1)                                    // 循环遍历操作每个IP合上端口的网络终结点
                {
                    IPEndPoint ListenEndPoint = new IPEndPoint(ListenIP[i], ListenPort[0]);        // 根据对应IP和端口创立网络终结点
                    SocketIPEndPointDict.Add(ListenEndPoint, new Dictionary<int, Socket>());       // 在套接字数组内添加对应 网络终结点 键和 套接字字典 值
                    Socket ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// 创立监听套接字
                    ListenSocket.Bind(ListenEndPoint);                                             // 绑定网络终结点
                    ListenSocket.Listen(1000);                                                     // 设为监听模式 设置连接队列上限
                    ListenThreadDict.Add(ListenEndPoint, new Thread(new ThreadStart(() => Listening(ListenEndPoint, ListenSocket))));// 创建监听线程并加入字典
                    ListenThreadDict[ListenEndPoint].IsBackground = true;                          // 设为后台线程
                    ListenThreadDict[ListenEndPoint].Start();                                      // 启动线程
                    ListenSocketDict.Add(ListenEndPoint, ListenSocket);                            // 将监听套接字加入字典
                }
            }
        }
        /// <summary>
        /// 端口监听方法
        /// </summary>
        /// <param name="ListenIPEndPoint"></param>
        /// <param name="ListenSocket"></param>
        private void Listening(IPEndPoint ListenIPEndPoint, Socket ListenSocket)
        {
            while (ListenSocket.IsBound)                                                           // 确认套接字是否未被停止监听
            {
                Socket ConnectSocket = ListenSocket.Accept();                                      // 阻塞线程 开始侦听 收到连接 执行下句
                Thread thread = new Thread(new ThreadStart(() => ConnectDeal(ListenIPEndPoint, ConnectSocket)));// 创建客户端连接处理线程进行处理
                thread.IsBackground = true;                                                        // 设为后台线程
                thread.Start();                                                                    // 启动线程
            }
        }
        /// <summary>
        /// 收到连接套接字处理方法
        /// </summary>
        /// <param name="ListenIPEndPoint">监听的网络终结点</param>
        /// <param name="ConnectSocket">连接的套接字</param>
        private void ConnectDeal(IPEndPoint ListenIPEndPoint, Socket ConnectSocket)
        {
            //               套接字储存字典字典   键对应的字典      的键    的数组             的最后一个键的数值
            int SocketID = SocketIPEndPointDict[ListenIPEndPoint].Keys.ToArray()[SocketIPEndPointDict[ListenIPEndPoint].Keys.Count] + 1;// 获取处理键值
            //套接字储存字典字典 键对应的字典 添加（   套接字储存字典字典   键对应的字典      的键    的数组             的最后一个键的数值                     + 1 ,Socket套接字）
            SocketIPEndPointDict[ListenIPEndPoint].Add(SocketID, ConnectSocket);                   // 吧套接字ID和套接字加入数组
            Thread thread = new Thread(new ThreadStart(() => MessagesReceive(ListenIPEndPoint, ConnectSocket, SocketID)));// 创建消息接收线程
            thread.IsBackground = true;                                                            // 设为后台线程
            thread.Start();                                                                        // 启动线程
        }
        /// <summary>
        /// 消息接收方法
        /// </summary>
        /// <param name="ListenIPEndPoint">对应的网络终结点</param>
        /// <param name="ConnectSocket">连接的套接字</param>
        /// <param name="SocketDictID">套接字ID</param>
        private void MessagesReceive(IPEndPoint ListenIPEndPoint, Socket ConnectSocket, int SocketDictID)
        {
            while (ConnectSocket.Poll(1, SelectMode.SelectWrite))                                  // 确认套接字连接状态
            {
                bool NoError = true;                                                               // 错误标识
                byte[] MessagesArray = new byte[MessageSize];                                      // 创立缓冲区
                int Length = -1;                                                                   // 创立消息长度
                try
                {
                    Length = ConnectSocket.Receive(MessagesArray);                                 // 接收消息写入缓冲区，并获取长度
                }
                catch (SocketException se)
                { NoError = false; }
                catch (Exception se)
                { NoError = false; }
                if (NoError == true)
                {
                    ClientMessages(ListenIPEndPoint, SocketDictID, MessagesArray, Length);         // 执行委托的事件，输出消息
                }
            }
            // 当结束循环时就证明连接已中断
            // 现在处理连接中断后事
            ConnectSocket.Dispose();                                                               // 释放当前套接字
            SocketIPEndPointDict[ListenIPEndPoint].Remove(SocketDictID);                           // 从字典中移除该套接字
        }
        /// <summary>
        /// 
        /// </summary>
        private enum ListenMode
        {
            /// <summary>
            /// 单端口多IP模式
            /// </summary>
            Mode1,
            /// <summary>
            /// 多端口多IP模式
            /// </summary>
            Mode2
        }
    }
    public class Client
    {

    }
}
