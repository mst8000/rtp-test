using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace SendAudio
{
    class Program
    {
        // http://www.baku-dreameater.net/archives/10111
        // http://www.baku-dreameater.net/archives/10441
        // http://wildpie.hatenablog.com/entry/2014/09/24/000900

        //データを送信するリモートホストとポート番号
        const int remotePort = 50002;

        //最大UDPペイロードサイズ
        const int bufsize = 330;

        static void Main(string[] args)
        {
            //接続先のIPアドレスを入力
            Console.Write("接続先IPアドレス：");
            string remoteHost = Console.ReadLine();

            //String型のIPアドレスをIPAddress型にParse
            IPAddress remoteAddress;
            if (IPAddress.TryParse(remoteHost, out remoteAddress) == false)
            {
                Console.WriteLine("正しくないIPアドレスが入力されました");

                // 終了
                Environment.Exit(-1);
            }

            //IPエンドポイントを生成
            var remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);


            //UdpClientオブジェクトを生成
            System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient();

            using (var waveIn = new WaveInEvent())
            {
                waveIn.BufferMilliseconds = 20;

                long count = 0;

                //音声データ利用可能時の処理
                waveIn.DataAvailable += async (_, e) =>
                {
                    //-- 送信処理 --
                    byte[] bufferToSend = new byte[bufsize];

                    for (int i = 0; i < e.BytesRecorded; i += bufsize)
                    {
                        if (e.BytesRecorded > i + bufsize)
                        {
                            //バッファ内のデータがペイロードサイズ以上
                            Array.Copy(e.Buffer, i, bufferToSend, 0, bufsize);
                            await udp.SendAsync(bufferToSend, bufferToSend.Length, remoteEndPoint);
                        }
                        else
                        {
                            //バッファ内のサイズがペイロードサイズ以下
                            Array.Copy(e.Buffer, i, bufferToSend, 0, e.BytesRecorded - i);
                            await udp.SendAsync(bufferToSend, e.BytesRecorded - i, remoteEndPoint);
                        }
                    }

                    count += e.BytesRecorded;
                    Console.WriteLine(count);

                    await Task.Delay(10);

                };

                //入力フォーマット設定
                waveIn.WaveFormat = new WaveFormat(8000, 16, 1);

                //音声の取得開始
                waveIn.StartRecording();

                Console.WriteLine("Press ENTER to quit...");
                Console.ReadLine();

                //音声の取得終了
                waveIn.StopRecording();
            }

            udp.Close();

            Console.WriteLine("Program ended successfully.");
        }


    }
}
