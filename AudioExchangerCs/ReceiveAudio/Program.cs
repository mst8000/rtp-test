using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.IO;

namespace ReceiveAudio
{
    class Program
    {
        // http://www.baku-dreameater.net/archives/10111
        // http://www.baku-dreameater.net/archives/10441
        // http://so-zou.jp/software/tech/programming/c-sharp/media/audio/naudio/

        static void Main(string[] args)
        {
            //音源のフォーマットを設定
            var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            bufferedWaveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 150);
            bufferedWaveProvider.DiscardOnBufferOverflow = true;

            Console.WriteLine("buffersize= " + bufferedWaveProvider.BufferLength);

            //ボリューム調整をするために上のBufferedWaveProviderをデコレータっぽく包む
            var wavProvider = new VolumeWaveProvider16(bufferedWaveProvider);
            wavProvider.Volume = 1.0f;

            //再生デバイスと出力先を設定
            var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            //外部からの音声入力を受け付け開始
            Task t = StartReceiveAudioPacket(bufferedWaveProvider);

            using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 20))
            {
                //出力に入力を接続して再生開始
                wavPlayer.Init(wavProvider);
                wavPlayer.Play();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();

                wavPlayer.Stop();
            }
        }

        static async Task StartReceiveAudioPacket(BufferedWaveProvider provider)
        {
            //バインドするローカルポート番号
            int localPort = 50002;

            //UdpClientを作成し、ローカルエンドポイントにバインドする
            System.Net.IPEndPoint localEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, localPort);

            using (System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient(localEP))
            {
                System.Net.IPEndPoint remoteEP = null;

                for (;;)
                {
                    //データを受信する
                    while (udp.Available > 0)
                    {
                        byte[] rcvBytes = udp.Receive(ref remoteEP);

                        Console.WriteLine("buffered= " + provider.BufferedBytes);
                        Console.WriteLine("received= " + rcvBytes.Length);

                        //バッファに追加
                        provider.AddSamples(rcvBytes, 0, rcvBytes.Length);
                    }
                    
                    await Task.Delay(10);
                }


            }

        }
    }
}
