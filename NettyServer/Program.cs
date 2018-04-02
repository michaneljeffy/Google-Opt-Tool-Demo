using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NettyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Task t = RunServerAsync();
            t.Wait();
            Console.WriteLine("Hello World!");
        }

        static async Task RunServerAsync()
        {
            var bossGroup = new MultithreadEventLoopGroup();
            var workerGroup = new MultithreadEventLoopGroup();

            X509Certificate2 tlsCertificate = null;

            if(ServerSettings.IsSsl)
            {
                tlsCertificate = new X509Certificate2("filename","password");
            }

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                  .Group(bossGroup, workerGroup) // 设置主和工作线程组
                  .Channel<TcpServerSocketChannel>() // 设置通道模式为TcpSocket
                  .Option(ChannelOption.SoBacklog, 100) // 设置网络IO参数等，这里可以设置很多参数，当然你对网络调优和参数设置非常了解的话，你可以设置，或者就用默认参数吧
                  .Handler(new LoggingHandler("SRV-LSTN")) //在主线程组上设置一个打印日志的处理器
                  .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                  { //工作线程连接器 是设置了一个管道，服务端主线程所有接收到的信息都会通过这个管道一层层往下传输//同时所有出栈的消息 也要这个管道的所有处理器进行一步步处理
                        IChannelPipeline pipeline = channel.Pipeline; if (tlsCertificate != null) //Tls的加解密
                        {
                          pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                      }                        //日志拦截器
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));//出栈消息，通过这个handler 在消息顶部加上消息的长度
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));//入栈消息通过该Handler,解析消息的包长信息，并将正确的消息体发送给下一个处理Handler，该类比较常用，后面单独说明
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));//业务handler ，这里是实际处理Echo业务的Handler
                        pipeline.AddLast("echo", new EchoServerHandler());
                  }));
                // bootstrap绑定到指定端口的行为 就是服务端启动服务，同样的Serverbootstrap可以bind到多个端口
                IChannel boundChannel = await bootstrap.BindAsync(ServerSettings.Port);

                Console.ReadLine();//关闭服务                

                await boundChannel.CloseAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            finally
            {
                //释放工作组线程              
                await Task.WhenAll(
                                  bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                                  workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }
    }
}
