using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace HttpAsyncServ1
{
    class Program
    {
        static ReaderWriterLock locker = new ReaderWriterLock();
        private static bool isWorking = true;

        private static List<Task> ListOfTasks;
        static void Main(string[] args)
        {

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            var server = new HttpListener();

            Console.CancelKeyPress += delegate {
                isWorking = false;
                server.Stop();
                Task.WaitAll(ListOfTasks.ToArray());
                Environment.Exit(0);
            };
            server.IgnoreWriteExceptions = true;

            server.Prefixes.Add("http://127.0.0.1:8080/");

            server.Start(); // начинаем прослушивать входящие подключения
            int s = 0;
            ListOfTasks = new List<Task>();
            var t = WaitForRequestAndProcess(server);
            ListOfTasks.Add(t);
            Task.WaitAll(t);

        }




        static async public Task WaitForRequestAndProcess(HttpListener serv)
        {
            while (isWorking) {
                var context = await serv.GetContextAsync();
                ProccesContext(context);
            }
        }



        static public async void ProccesContext(HttpListenerContext context)
        {

           // await Task.Delay(1000);
            var request = context.Request;
            StringBuilder sb = new StringBuilder();
            var url = request.Url;
            var ip = request.RemoteEndPoint;
            string filepath = "goodweb/" + request.Url.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
            HttpListenerResponse response = context.Response;

            if (!File.Exists(filepath))
            {
                Console.WriteLine(filepath);
                sb.Append(DateTime.Now + " File not exist; " + url + "; " + ip + "; Status code:404\n");
                locker.AcquireWriterLock(5000);
                File.AppendAllText("log.txt", sb.ToString());
                locker.ReleaseWriterLock();
                sb.Clear();
                response.StatusCode = 404;
                response.OutputStream.Close();
                return;
            }


            System.IO.Stream input = File.OpenRead(filepath);
            byte[] buffer = new byte[1024];

            int bytesRead = input.Read(buffer, 0, buffer.Length);
            while (bytesRead > 0)
            {
                response.OutputStream.Write(buffer, 0, bytesRead);
                bytesRead = input.Read(buffer, 0, buffer.Length);

            }

            sb.Append(DateTime.Now + " File exist; " + url + "; " + ip + "; Status code:200\n");
            locker.AcquireWriterLock(5000);

            File.AppendAllText("log.txt", sb.ToString());
            locker.ReleaseWriterLock();
            sb.Clear();

            Console.WriteLine(filepath);
            response.StatusCode = 200;
            input.Close();
            response.OutputStream.Close();


        }
    }
}
