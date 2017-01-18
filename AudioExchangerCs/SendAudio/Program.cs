using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.IO;

namespace SendAudio
{
    class Program
    {
        // http://www.baku-dreameater.net/archives/10111
        // http://www.baku-dreameater.net/archives/10441
        // http://wildpie.hatenablog.com/entry/2014/09/24/000900

        //データを送信するリモートホストとポート番号
        const string remoteHost = "127.0.0.1";
        const int remotePort = 50002;

        static void Main(string[] args)
        {
            //UdpClientオブジェクトを作成する
            System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient();

            using (var waveIn = new WaveInEvent())
            {
                waveIn.BufferMilliseconds = 200;
                long count = 0;
                int bufsize = 1400;

                //音声データ利用可能時の処理
                waveIn.DataAvailable += async (_, e) =>
                {
                    //byte[]であるe.Bufferのうち先頭からe.BytesRecorded個が有効な録音データなのでそれを使って何かする

                    //byte[] bufferToSend = new byte[e.BytesRecorded];      //送信するデータ
                    //Array.Copy(e.Buffer, bufferToSend, e.BytesRecorded);

                    //-- 送信処理 --

                    byte[] bufferToSend = new byte[bufsize];

                    for (int i = 0; i + bufsize < e.BytesRecorded; i += bufsize)
                    {
                        Array.Copy(e.Buffer, i, bufferToSend, e.BytesRecorded, bufsize);

                        await udp.SendAsync(bufferToSend, e.BytesRecorded, remoteHost, remotePort);
                    }




                    count += e.BytesRecorded;
                    Console.WriteLine(count);

                };

                //入力フォーマット設定
                waveIn.WaveFormat = new WaveFormat(16000, 8, 1);

                //音声の取得開始
                waveIn.StartRecording();

                Console.WriteLine("Press ENTER to quit...");
                Console.ReadLine();

                //音声の取得終了
                waveIn.StopRecording();
            }

            Console.WriteLine("Program ended successfully.");
        }


    }
}
