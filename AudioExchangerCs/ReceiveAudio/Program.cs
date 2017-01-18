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

        static void Main(string[] args)
        {
            //音源のフォーマットを設定
            var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

            //ボリューム調整をするために上のBufferedWaveProviderをデコレータっぽく包む
            var wavProvider = new VolumeWaveProvider16(bufferedWaveProvider);
            wavProvider.Volume = 0.1f;

            //再生デバイスと出力先を設定
            var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            //外部からの音声入力を受け付け開始
            Task t = StartDummySoundSource(bufferedWaveProvider);

            using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 200))
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
            //バインドするローカルIPとポート番号
            string localIpString = "127.0.0.1";
            System.Net.IPAddress localAddress = System.Net.IPAddress.Parse(localIpString);
            int localPort = 50002;

            //UdpClientを作成し、ローカルエンドポイントにバインドする
            System.Net.IPEndPoint localEP = new System.Net.IPEndPoint(localAddress, localPort);

            using (System.Net.Sockets.UdpClient udp = new System.Net.Sockets.UdpClient(localEP))
            {
                for (;;)
                {
                    //データを受信する
                    System.Net.IPEndPoint remoteEP = null;
                    byte[] rcvBytes = udp.Receive(ref remoteEP);

                    //バッファに追加
                    provider.AddSamples(rcvBytes,)

                    //データを文字列に変換する
                    string rcvMsg = System.Text.Encoding.UTF8.GetString(rcvBytes);

                    //受信したデータと送信者の情報を表示する
                    Console.WriteLine("受信したデータ:{0}", rcvMsg);
                    Console.WriteLine("送信元アドレス:{0}/ポート番号:{1}",                        remoteEP.Address, remoteEP.Port);


                }
            }
            
        }

        //外部入力のダミーとしてデスクトップにある"sample.wav"あるいは"sample.mp3"を用いて音声を入力する
        static async Task StartDummySoundSource(BufferedWaveProvider provider)
        {
            //外部入力のダミーとして適当な音声データを用意して使う
            string wavFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "sample.wav"
                );

            if (!(File.Exists(wavFilePath)))
            {
                Console.WriteLine("Target sound files were not found. Wav file is needed for this program.");
                Console.WriteLine($"expected wav file: {wavFilePath}");
                Console.WriteLine("(note: ONE file is enough, two files is not needed)");
                return;
            }

            byte[] data = File.ReadAllBytes(wavFilePath);

            //若干効率が悪いがヘッダのバイト数を確実に割り出して削る
            using (var r = new WaveFileReader(wavFilePath))
            {
                int headerLength = (int)(data.Length - r.Length);
                data = data.Skip(headerLength).ToArray();
            }

            int bufsize = 16000;
            for (int i = 0; i + bufsize < data.Length; i += bufsize)
            {
                provider.AddSamples(data, i, bufsize);
                await Task.Delay(100);
            }
        }
    }
}
