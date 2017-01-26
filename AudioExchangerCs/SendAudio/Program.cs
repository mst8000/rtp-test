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

        //ポート番号
        const int localPort = 50002;
        const int remotePort = 50003;

        //最大UDPペイロードサイズ
        const int bufsize = 350;

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
            var localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            
            //UdpClientオブジェクトを生成
            var udp = new System.Net.Sockets.UdpClient(localEndPoint);

            using (var waveIn = new WaveInEvent())
            {
                //総送信バイト数を保持する変数
                long count = 0;

                //入力バッファのサイズを設定(20ms)
                waveIn.BufferMilliseconds = 20;

                //G.722コーデックを用意
                var codec = new NAudio.Codecs.G722Codec();
                var codecState = new NAudio.Codecs.G722CodecState(64000, NAudio.Codecs.G722Flags.None);

                //音声データ利用可能時の処理(非同期)
                waveIn.DataAvailable += async (_, e) =>
                {
                    //-- 送信処理 --
                    byte[] bufferedBytes = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, bufferedBytes, e.BytesRecorded);

                    short[] bufferedData = Convert16BitToShort(bufferedBytes);
                    byte[] encodedBytes = new byte[e.BytesRecorded];
                    int encodedLength = codec.Encode(codecState, encodedBytes,bufferedData , bufferedData.Length);

                    byte[] bufferToSend = new byte[bufsize];

                    for (int i = 0; i < encodedLength; i += bufsize)
                    {
                        if (encodedLength > i + bufsize)
                        {
                            //バッファ内のデータがペイロードサイズ以上
                            Array.Copy(encodedBytes, i, bufferToSend, 0, bufsize);
                            await udp.SendAsync(bufferToSend, bufferToSend.Length, remoteEndPoint);
                        }
                        else
                        {
                            //バッファ内のサイズがペイロードサイズ以下
                            Array.Copy(encodedBytes, i, bufferToSend, 0, encodedLength - i);
                            await udp.SendAsync(bufferToSend, encodedLength - i, remoteEndPoint);
                        }
                    }

                    count += encodedLength;
                    Console.WriteLine(count);

                    await Task.Delay(10);
                };

                //入力フォーマット設定(16kHz, 16bit, 1ch)
                waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

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

        //byte型配列からShort型配列に変換するメソッド
        static public short[] Convert16BitToShort(byte[] input)
        {
            int inputSamples = (input.Length + 1) / 2;
            short[] output = new short[inputSamples];
            int outputIndex = 0;
            for (int n = 0; n < inputSamples; n++)
            {
                short sample = BitConverter.ToInt16(input, n * 2);
                output[outputIndex++] = sample;
            }
            return output;
        }
    }
}
