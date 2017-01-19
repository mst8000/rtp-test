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

                var codec = new NAudio.Codecs.G722Codec();
                var codecState = new NAudio.Codecs.G722CodecState(64000, NAudio.Codecs.G722Flags.None);

                long count = 0;

                //音声データ利用可能時の処理
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

        static public float[] Convert16BitToFloat(byte[] input)
        {
            int inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
            float[] output = new float[inputSamples];
            int outputIndex = 0;
            for (int n = 0; n < inputSamples; n++)
            {
                short sample = BitConverter.ToInt16(input, n * 2);
                output[outputIndex++] = sample / 32768f;
            }
            return output;
        }

        static public short[] Convert16BitToShort(byte[] input)
        {
            int inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
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
