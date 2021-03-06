using Mature.Socket.Compression;
using Mature.Socket.ContentBuilder;
using Mature.Socket.DataFormat;
using Mature.Socket.Notify;
using Mature.Socket.Validation;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mature.Socket.Client.SuperSocket
{
    public class TCPClient : ITCPClient
    {
        IContentBuilder contentBuilder;
        IDataFormat dataFormat;
        IDataValidation dataValidation;
        ICompression compression;
        ConcurrentDictionary<string, TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo>> task = new System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo>>();

        public TCPClient(IDataFormat dataFormat, IDataValidation dataValidation, ICompression compression)
        {
            this.contentBuilder = new ContentBuilder.ContentBuilder(compression, dataValidation, dataFormat);
            this.dataFormat = dataFormat;
            this.dataValidation = dataValidation;
            this.compression = compression;
            easyClient = new EasyClient<global::SuperSocket.ProtoBase.StringPackageInfo>();
            easyClient.KeepAliveTime = 60;//单位：秒
            easyClient.KeepAliveInterval = 5;//单位：秒
            easyClient.Initialize(new MyFixedHeaderReceiveFilter());
            easyClient.NewPackageReceived += EasyClient_NewPackageReceived;
            easyClient.Connected += EasyClient_Connected;
            easyClient.Closed += EasyClient_Closed;
        }

        private void EasyClient_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("EasyClient_Closed");
            if (Closed != null)
            {
                Closed(this, null);
            }
        }

        private void EasyClient_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("EasyClient_Connected");
            if (Connected != null)
            {
                Connected(this, null);
            }
        }

        private void EasyClient_NewPackageReceived(object sender, PackageEventArgs<global::SuperSocket.ProtoBase.StringPackageInfo> e)
        {
            Console.WriteLine($"Key:{e.Package.Key}  Body:{e.Package.Body}");

            if (task.TryGetValue(e.Package.GetFirstParam(), out TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo> tcs))
            {
                tcs.TrySetResult(e.Package);
            }
            else
            {
                NotifyContainer.Instance.Raise(e.Package.Key);
            }
        }

        EasyClient<global::SuperSocket.ProtoBase.StringPackageInfo> easyClient;

        public bool IsConnected => easyClient == null ? false : easyClient.Socket.Connected;

        public string SessionId => sessionId;
        public EndPoint RemoteEndPoint => endPoint;

        public event EventHandler Connected;
        public event EventHandler Closed;
        EndPoint endPoint;
        string sessionId;
        public async Task<bool> ConnectAsync(string ip, ushort port)
        {
            endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var isConnect = await easyClient.ConnectAsync(endPoint);
            sessionId = Guid.NewGuid().ToString();
            return isConnect;
        }
        public async Task<TResponse> SendAsync<TRequest, TResponse>(string key, TRequest request, int timeout)
        {
            string body = dataFormat.Serialize<TRequest>(request);

            if (string.IsNullOrEmpty(key) || key.Length >= 20)
            {
                throw new Exception("The key length is no more than 20.");
            }
            else
            {
                key = key.PadRight(20, ' ');
            }
            TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo> taskCompletionSource = new TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo>();
            //超时处理
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Token.Register(() => taskCompletionSource.TrySetException(new TimeoutException()));
            cts.CancelAfter(timeout);
            string messageId = Guid.NewGuid().ToString().Replace("-", "");
            task.TryAdd(messageId, taskCompletionSource);
            global::SuperSocket.ProtoBase.StringPackageInfo result = null;
            try
            {
                Console.WriteLine($"发送消息，消息ID：{messageId} 消息命令标识：{key} 消息内容：{body}");
                easyClient.Send(contentBuilder.Builder(key, body, messageId));
                result = await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
                throw ex;
            }
            finally
            {
                cts.Dispose();
                task.TryRemove(messageId, out TaskCompletionSource<global::SuperSocket.ProtoBase.StringPackageInfo> tcs);
            }
            return dataFormat.Deserialize<TResponse>(result?.Body);
        }

        public void Close()
        {
            if (Closed != null)
            {
                Closed(this, null);
            }
        }

        public void RegisterNotify<TResponse>(string key, Action<TResponse> action)
        {
            NotifyContainer.Instance.Register<TResponse>(key, action);
        }

        public void UnRegisterNotify<TResponse>(string key, Action<TResponse> action)
        {
            NotifyContainer.Instance.UnRegister<TResponse>(key, action);
        }
    }
}
