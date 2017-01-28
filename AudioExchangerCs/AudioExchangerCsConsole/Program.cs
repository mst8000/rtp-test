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
            UPnPWanService napt = null;  //UPnPサービス

            //UPnPを使用するか確認
            Console.Write("UPnPを使用してNAT外の端末と通信を行いますか？ [Y(既定)/N] > ");
            if (Console.ReadLine().ToUpper() != "N")
            {
                //UPnPを使用
                bool naptSearchComplete = false;
                var task = new Task(() =>
                    {
                        Console.Write("UPnP対応ルータを探索中");
                        while (naptSearchComplete == false)
                        {
                            Console.Write(".");
                            Thread.Sleep(1000);
                        }
                        Console.WriteLine("");
                    });
                task.Start();

                //UPnP対応ルータを探索
                napt = UPnPWanService.FindUPnPWanService();
                naptSearchComplete = true;
                task.Wait();

                if (napt != null)
                {
                    //ポートマッピングを追加
                    try
                    {
                        napt.AddPortMapping(null, 50003, "UDP", 50003, napt.GetLocalIPAddress(), true, "AudioExchangerCs", 3600);
                        Console.WriteLine("ポートマッピングを追加しました");
                    }
                    catch (System.Runtime.InteropServices.COMException exception)
                    {
                        Console.WriteLine(exception.Message);
                        napt = null;
                    }
                }
                else
                {
                    Console.WriteLine("UPnP対応ルータが見つかりませんでした");
                }
            }

            //通話待受IPアドレスを表示
            if (napt != null)
            {
                Console.WriteLine($"{napt.GetLocalIPAddress().ToString()} もしくは {napt.GetExternalIPAddress().ToString()} で通話を受け付けます\r\n");
            }
            else
            {
                IPAddress[] localAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress address in localAddresses)
                {
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Console.Write($"{address.ToString()}, ");
                    }
                }
                Console.CursorLeft = Console.CursorLeft - 2;
                Console.WriteLine($" で通話を受け付けます\r\n");
            }

            //通話待受処理ループ
            while (true)
            {
                var receiveAudio = new ReceiveAudio();
                var sendAudio = new SendAudio();
                var source = new CancellationTokenSource();
                IPAddress remoteAddress;

                //接続先のIPアドレスを入力
                Console.Write("接続先IPアドレス・ホスト名 (exit:終了) > ");
                string remoteHost = Console.ReadLine();

                //終了コマンド
                if (remoteHost == "exit")
                {
                    break;
                }
                //String型のIPアドレスとしてIPAddress型にParse
                else if (IPAddress.TryParse(remoteHost, out remoteAddress) == false)
                {
                    try
                    {
                        //String型のDNS名としてIPAddress型に名前解決
                        remoteAddress = Dns.GetHostEntry(remoteHost).AddressList[0];
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("正しくないIPアドレスもしくはホスト名が入力されました");
                        continue;
                    }
                }

                Console.WriteLine($"Enterキーを押すと{remoteAddress.ToString()}と通話を開始します...");
                Console.ReadLine();

                //タスクを生成
                var task_r = new Task(async () => await receiveAudio.StartReceiveAsync(source));
                var task_s = new Task(async () => await sendAudio.StartSendAsync(remoteAddress, source));

                //タスクを開始
                task_r.Start();
                task_s.Start();

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine($"[通話開始] ({DateTime.Now.ToString("HH:mm:ss")}) -->{remoteAddress.ToString()}({sendAudio.RemotePort})");
                Console.WriteLine("Enterキーを押すと通話を終了します...");
                Console.ReadLine();

                //タスクのキャンセル要求を発行(通話終了)
                source.Cancel();
                try
                {
                    //タスクが終了するまで待機
                    task_r.Wait();
                    task_s.Wait();
                }
                catch (AggregateException exception)
                {
                    Console.WriteLine(exception.Message);
                }
                finally
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine($"[通話終了] ({DateTime.Now.ToString("HH:mm:ss")})\r\n");
                }
            }

            //UPnPが有効な場合ポートマッピングを削除
            if (napt != null)
            {
                try
                {
                    napt.DeletePortMapping(null, 50003, "UDP");
                    Console.WriteLine("ポートマッピング削除完了");
                }
                catch (System.Runtime.InteropServices.COMException exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }
    }
}
