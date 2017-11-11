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
        public Dictionary<long, Socket> SocketIdDict = new Dictionary<long, Socket>();
        /// <summary>
        /// 以网络终结点为key的套接字储存字典的储存字典
        /// </summary>
        public Dictionary<IPEndPoint, Dictionary<int, Socket>> SocketIPEndPointDict = new Dictionary<IPEndPoint, Dictionary<int, Socket>>();
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
        public  ClientMessagesDelegate ClientMessages;
        /// <summary>
        /// Tcp套接字终止连接事件委托
        /// </summary>
        /// <param name="ListenIPEndPoint">连接的网络终结点</param>
        /// <param name="SocketDictID">套接字ID</param>
        public delegate void ConnentStopDelegate(IPEndPoint ListenIPEndPoint, int SocketDictID);
        /// <summary>
        /// Tcp套接字终止连接事件委托
        /// </summary>
        public ConnentStopDelegate ConnentStop;
        /// <summary>
        /// Tcp客户端连接事件委托
        /// </summary>
        /// <param name="ListenIPEndPoint"></param>
        /// <param name="SocketDictID"></param>
        public delegate void ClientConnentDelegate(IPEndPoint ListenIPEndPoint, int SocketDictID);
        /// <summary>
        /// Tcp客户端连接事件委托
        /// </summary>
        public ClientConnentDelegate ClientConnent;
        #endregion
        #region 字典
        /// <summary>
        /// 侦听线程
        /// </summary>
        Dictionary<IPEndPoint, Thread> ListenThreadDict = new Dictionary<IPEndPoint, Thread>();
        /// <summary>
        /// 端口侦听套接字
        /// </summary>
        Dictionary<IPEndPoint, Socket> ListenSocketDict = new Dictionary<IPEndPoint, Socket>();
        #endregion
        #region 逻辑变量
        IPAddress[] ListenIP;                                                                      // 侦听IP地址集
        int[] ListenPort;                                                                          // 侦听端口集
        ListenMode ServerMode;                                                                     // 侦听模式
        int MessageSize;                                                                           // 接收消息缓存区大小
        #endregion
        #endregion
        #region 构造函数
        /// <summary>
        /// 单端口多IP模式
        /// 输出无连接端口标识
        /// </summary>
        /// <param name="IpArray">IP地址数组</param>
        /// <param name="Port">端口1-65535</param>
        /// <param name="ServerMessageSize">接收消息缓存区大小</param>
        public Server(IPAddress[] IpArray, int Port, int ServerMessageSize)
        {
            if (Port < 1 && Port > 65535)
            {
                throw new ArgumentOutOfRangeException("端口" + Port + "不符合规则");
            }
            else if (ServerMessageSize < 10)
            {
                throw new ArgumentOutOfRangeException("缓冲区过小，小于10");
            }
            else
            {
                ServerMode = ListenMode.Mode1;
                ListenIP = IpArray;
                ListenPort[0] = Port;
                MessageSize = ServerMessageSize;
            }
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
            List<int> ErrorList = new List<int>();
            for (int i = 0; i < Port.Length; i = i + 1)
            {
                if (Port[i] < 1 && Port[i] > 65535)
                {
                    ErrorList.Add(Port[i]);
                    throw new ArgumentOutOfRangeException("端口" + Port[i] + "有误");
                }
            }
            if (ServerMessageSize < 10)
            {
                throw new ArgumentOutOfRangeException("缓冲区过小，小于10");
            }
            else if (ErrorList.Count < 0)
            {
                ServerMode = ListenMode.Mode2;
                ListenIP = IpArray;
                ListenPort = Port;
            }
        }
        #endregion
        #endregion
        #region 端口侦听
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
                    try
                    {
                        ListenSocket.Bind(ListenEndPoint);                                             // 绑定网络终结点
                        ListenSocket.Listen(1000);                                                     // 设为监听模式 设置连接队列上限
                        ListenThreadDict.Add(ListenEndPoint, new Thread(new ThreadStart(() => Listening(ListenEndPoint, ListenSocket))));// 创建监听线程并加入字典
                        ListenThreadDict[ListenEndPoint].IsBackground = true;                          // 设为后台线程
                        ListenThreadDict[ListenEndPoint].Start();                                      // 启动线程
                        ListenSocketDict.Add(ListenEndPoint, ListenSocket);                            // 将监听套接字加入字典
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentOutOfRangeException("在创立套接字时出错：\r\n" + e.Message + "\r\n位于：\r\n" + e.Source);
                    }
                }
            }
            else if (ServerMode == ListenMode.Mode2)
            {
                for (int ia = 0; ia < ListenPort.Length; ia = ia + 1)
                {
                    for (int i = 0; i < ListenIP.Length; i = i + 1)                                    // 循环遍历操作每个IP合上端口的网络终结点
                    {
                        IPEndPoint ListenEndPoint = new IPEndPoint(ListenIP[i], ListenPort[ia]);       // 根据对应IP和端口创立网络终结点
                        SocketIPEndPointDict.Add(ListenEndPoint, new Dictionary<int, Socket>());       // 在套接字数组内添加对应 网络终结点 键和 套接字字典 值
                        Socket ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// 创立监听套接字
                        try
                        {
                            ListenSocket.Bind(ListenEndPoint);                                             // 绑定网络终结点
                            ListenSocket.Listen(1000);                                                     // 设为监听模式 设置连接队列上限
                            ListenThreadDict.Add(ListenEndPoint, new Thread(new ThreadStart(() => Listening(ListenEndPoint, ListenSocket))));// 创建监听线程并加入字典
                            ListenThreadDict[ListenEndPoint].IsBackground = true;                          // 设为后台线程
                            ListenThreadDict[ListenEndPoint].Start();                                      // 启动线程
                            ListenSocketDict.Add(ListenEndPoint, ListenSocket);                            // 将监听套接字加入字典
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentOutOfRangeException("在创立套接字时出错：\r\n" + e.Message + "\r\n位于：\r\n" + e.Source);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 添加端口侦听
        /// </summary>
        /// <param name="IP">IP地址</param>
        /// <param name="Port">端口</param>
        public void AddListen(IPAddress IP, int Port)
        {
            if (Port < 1 && Port > 65535)
            {
                throw new ArgumentOutOfRangeException("端口" + Port + "不符合规则");
            }
            else if (ListenIP.Contains(IP) && ListenPort.Contains(Port))
            {
                throw new ArgumentOutOfRangeException(IP + ":" + Port + "已经有了");
            }
            else
            {
                List<IPAddress> Ip = new List<IPAddress>();
                for (int i = 0; i < ListenIP.Length + 1; i = i + 1)
                    if (i != ListenIP.Length + 1)
                        Ip.Add(ListenIP[i]);
                    else
                        Ip.Add(IP);
                ListenIP = Ip.ToArray();
                List<int> port = new List<int>();
                for (int i = 0; i < ListenPort.Length + 1; i = i + 1)
                    if (i != ListenPort.Length + 1)
                        port.Add(ListenPort[i]);
                    else
                        port.Add(Port);
                ListenPort = port.ToArray();
                IPEndPoint ListenEndPoint = new IPEndPoint(IP, Port);                          // 根据对应IP和端口创立网络终结点
                SocketIPEndPointDict.Add(ListenEndPoint, new Dictionary<int, Socket>());       // 在套接字数组内添加对应 网络终结点 键和 套接字字典 值
                Socket ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// 创立监听套接字
                try
                {
                    ListenSocket.Bind(ListenEndPoint);                                             // 绑定网络终结点
                    ListenSocket.Listen(1000);                                                     // 设为监听模式 设置连接队列上限
                    ListenThreadDict.Add(ListenEndPoint, new Thread(new ThreadStart(() => Listening(ListenEndPoint, ListenSocket))));// 创建监听线程并加入字典
                    ListenThreadDict[ListenEndPoint].IsBackground = true;                          // 设为后台线程
                    ListenThreadDict[ListenEndPoint].Start();                                      // 启动线程
                    ListenSocketDict.Add(ListenEndPoint, ListenSocket);                            // 将监听套接字加入字典
                }
                catch (Exception e)
                {
                    throw new ArgumentOutOfRangeException("在创立套接字时出错：\r\n" + e.Message + "\r\n位于：\r\n" + e.Source);
                }
            }
        }
        #endregion
        #region 关闭服务端
        public void Stop()
        {
            int SocketNumber = ListenSocketDict.Keys.Count;                                        // 获取监听套接字数量
            for (int i = 0; i < SocketNumber; i = i + 1)                                           // 遍历监听套接字
            {
                ListenSocketDict[ListenSocketDict.Keys.ToArray()[i]].Dispose();                    // 关闭连接释放资源
                ListenSocketDict.Remove(ListenSocketDict.Keys.ToArray()[i]);                       // 从字典中移除
            }
            int ThreadNumber = ListenThreadDict.Keys.Count;                                        // 获取监听线程数量
            for (int i = 0; i < ThreadNumber; i = i + 1)                                           // 遍历监听线程
            {
                if (ListenThreadDict[ListenThreadDict.Keys.ToArray()[i]].ThreadState == ThreadState.Running)// 判断是否还在运行
                {
                    ListenThreadDict[ListenThreadDict.Keys.ToArray()[i]].Join();                   // 关闭线程
                }
            }
            SocketNumber = SocketIPEndPointDict.Keys.Count;                                        // 获取当前Tcp连接套接字字典数量
            for (int i = 0; i < SocketNumber; i = i + 1)                                           // 遍历Tcp套接字字典
            {
                int socketNumber = SocketIPEndPointDict[SocketIPEndPointDict.Keys.ToArray()[i]].Keys.Count;// 获取当前Tcp连接套接字数量
                for (int ia = 0; ia < socketNumber; ia = ia + 1)                                   // 遍历Tcp套接字
                {
                    SocketIPEndPointDict[SocketIPEndPointDict.Keys.ToArray()[i]][SocketIPEndPointDict[SocketIPEndPointDict.Keys.ToArray()[i]].Keys.ToArray()[ia]].Dispose();// 关闭释放套接字
                }
            }
        }
        #endregion
        #region Tcp服务端代码
        /// <summary>
        /// 端口监听方法
        /// </summary>
        /// <param name="ListenIPEndPoint"></param>
        /// <param name="ListenSocket"></param>
        private void Listening(IPEndPoint ListenIPEndPoint, Socket ListenSocket)
        {
            while (ListenSocket.IsBound)                                                           // 确认套接字是否未被停止监听
            {
                try
                {
                    Socket ConnectSocket = ListenSocket.Accept();                                      // 阻塞线程 开始侦听 收到连接 执行下句
                    ThreadPool.QueueUserWorkItem(ConnectDeal(ListenIPEndPoint, ConnectSocket));        // 创建客户端连接处理线程进行处理
                }
                catch (Exception e)
                {
                    throw new ArgumentOutOfRangeException("在监听时出错：\r\n" + e.Message + "\r\n位于：\r\n" + e.Source);
                }
                /*Thread thread = new Thread(new ThreadStart(() => ConnectDeal(ListenIPEndPoint, ConnectSocket)));
                thread.IsBackground = true;                                                        // 设为后台线程
                thread.Start();                                                                    // 启动线程*/
            }
        }
        /// <summary>
        /// 收到连接套接字处理方法
        /// </summary>
        /// <param name="ListenIPEndPoint">监听的网络终结点</param>
        /// <param name="ConnectSocket">连接的套接字</param>
        private WaitCallback ConnectDeal(IPEndPoint ListenIPEndPoint, Socket ConnectSocket)
        {
            //               套接字储存字典字典   键对应的字典      的键    的数组             的最后一个键的数值
            int SocketID = SocketIPEndPointDict[ListenIPEndPoint].Keys.ToArray()[SocketIPEndPointDict[ListenIPEndPoint].Keys.Count] + 1;// 获取处理键值
            ClientConnent(ListenIPEndPoint, SocketID);                                             // 执行客户端连接委托事件
            //套接字储存字典字典 键对应的字典 添加（   套接字储存字典字典   键对应的字典      的键    的数组             的最后一个键的数值                     + 1 ,Socket套接字）
            SocketIPEndPointDict[ListenIPEndPoint].Add(SocketID, ConnectSocket);                   // 把套接字ID和套接字加入数组
            Thread thread = new Thread(new ThreadStart(() => MessagesReceive(ListenIPEndPoint, ConnectSocket, SocketID)));// 创建消息接收线程
            thread.IsBackground = true;                                                            // 设为后台线程
            thread.Start();                                                                        // 启动线程
            return null;
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
            ConnentStop(ListenIPEndPoint, SocketDictID);                                           // 执行客户端终止连接信息委托事件
            GC.Collect();
        }
        #endregion
        #region 主动执行功能
        public void Send(IPEndPoint ListenIPEndPoint, int SocketID, byte[] SendContext)
        {
            SocketIPEndPointDict[ListenIPEndPoint][SocketID].Send(SendContext);
        }
        public void Close(IPEndPoint ListenIPEndPoint, int SocketID)
        {
            SocketIPEndPointDict[ListenIPEndPoint][SocketID].Dispose();
            SocketIPEndPointDict[ListenIPEndPoint].Remove(SocketID);
        }
        #endregion
        /// <summary>
        /// 枚举
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
