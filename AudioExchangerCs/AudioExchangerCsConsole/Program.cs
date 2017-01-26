using AudioExchangerCs;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AudioExchangerCsConsole
{
    class Program
    {
        //エントリポイント
        static void Main(string[] args)
        {
            while (true)
            {
                var receiveAudio = new ReceiveAudio();
                var sendAudio = new SendAudio();

                var source = new CancellationTokenSource();


                //接続先のIPアドレスを入力
                Console.Write("接続先IPアドレス：");
                string remoteHost = Console.ReadLine();

                //String型のIPアドレスをIPAddress型にParse
                IPAddress remoteAddress;
                if (IPAddress.TryParse(remoteHost, out remoteAddress) == false)
                {
                    Console.WriteLine("正しくないIPアドレスが入力されました");
                    continue;
                }

                Console.Write("Enterキーを押すと通話を開始します...");
                Console.ReadKey();

                //タスクを生成
                var task_r = new Task(async () => await receiveAudio.StartReceiveAsync(source));
                var task_s = new Task(async () => await sendAudio.StartSendAsync(remoteAddress, source));

                //タスクを開始
                task_r.Start();
                task_s.Start();

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine($"[通話開始] ({DateTime.Now.ToString("HH:mm:ss")}) -->{remoteAddress.ToString()}({sendAudio.RemotePort})");
                Console.Write("Enterキーを押すと通話を終了します...");
                Console.ReadKey();

                //タスクのキャンセル要求を発行
                source.Cancel();
                try
                {
                    //タスクが終了するまで待機
                    task_r.Wait();
                    task_s.Wait();
                }
                catch (AggregateException exception)
                {
                    foreach (var inner in exception.InnerExceptions)
                    {
                        Console.WriteLine(inner.Message);
                        Console.WriteLine("Type : {0}", inner.GetType());
                    }
                }
                finally
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine($"[通話終了] ({DateTime.Now.ToString("HH:mm:ss")})               ");
                }
            }
        }
    }
}
