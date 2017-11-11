# TCP-IP
<p>对于.NET的TCP服务端和客户端的封装</p>
<p>这是一个基于.Net Core的c#类库项目</p>
<p>他封装了.Net的System.Net.Socket.Socket类和System.Net.Socket.Client类</p>
<p>实现了TCP通信简单化</p>
<p><p>Server端的创建只要New一个TCP_IP.Server类的对象，构造函数输入IP和端口，然后绑定收到客户端连接事件、收到消息事件、接收消息报错事件和客户端终止连接事件，同时也可以通过TCP_IP.Server类对象.SocketIPEndPointDict[监听的网络终结点].[套接字ID]直接操作套接字</p>
<p>Client端亦是如此</p>
<p>Server端使用方法：</p>
<code>
using System;
using TCP_IP;
namespace app
  {
    public class Class1
    {
      TCP_IP.Server TcpServer;
      public void StartServer()
      {
      IPAddress[] IpArray = { IPAddress.Parse("127.0.0.1") };
      TcpServer = new TCP_IP.Server(IpArray,25565);
      TcpServer.StartListen();
      }
    }
  }
</code>
