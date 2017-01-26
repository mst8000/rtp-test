using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace ReceiveAudio
{
    class Program
    {
        // http://www.baku-dreameater.net/archives/10111
        // http://www.baku-dreameater.net/archives/10441
        // http://so-zou.jp/software/tech/programming/c-sharp/media/audio/naudio/

        //バインドするローカルポート番号
        const int localPort = 50003;

        static void Main(string[] args)
        {
            //音源のフォーマットを設定(16kHz, 16bit, 1ch)
            var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

            //バッファサイズを設定
            bufferedWaveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 150);    //150ms分確保
            bufferedWaveProvider.DiscardOnBufferOverflow = true;                    //バッファオーバーフロー時にDiscardするように設定

            //バッファサイズ表示
            Console.WriteLine("BufferSize= " + bufferedWaveProvider.BufferLength);

            //再生デバイスと出力先を設定
            var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            //UDPパケットを受付開始
            Task t = StartReceiveAudioPacketAsync(bufferedWaveProvider);

            using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 20))
            {
                //出力に入力バッファを接続して再生開始
                wavPlayer.Init(bufferedWaveProvider);
                wavPlayer.Play();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();

                wavPlayer.Stop();
            }
        }

        static async Task StartReceiveAudioPacketAsync(BufferedWaveProvider provider)
        {            
            //UdpClientを作成し、ローカルエンドポイントにバインドする
            var localEP = new IPEndPoint(IPAddress.Any, localPort);

            //G.722コーデックを用意
            var codec = new NAudio.Codecs.G722Codec();
            var codecState = new NAudio.Codecs.G722CodecState(64000, NAudio.Codecs.G722Flags.None);

            using (var udp = new System.Net.Sockets.UdpClient(localEP))
            {
                IPEndPoint remoteEP = null;

                for (;;)
                {
                    //データを受信する
                    while (udp.Available > 0)
                    {
                        byte[] rcvBytes = udp.Receive(ref remoteEP);

                        short[] bufferedData = new short[350];

                        int bufferdLength = codec.Decode(codecState, bufferedData, rcvBytes, rcvBytes.Length);

                        byte[] bufferdBytes = ConvertShortTo16Bit(bufferedData, bufferdLength);

                        
                        //バッファに追加
                        provider.AddSamples(bufferdBytes, 0, bufferdBytes.Length);

                        Console.WriteLine("Buffered= " + provider.BufferedBytes);
                    }

                    await Task.Delay(10);
                }
            }
        }

        //Short型配列からbyte型配列に変換するメソッド
        static public byte[] ConvertShortTo16Bit(short[] input, int inputLength)
        {
            int outputLength = inputLength * 2;
            byte[] output = new byte[outputLength];

            for (int n = 0; n < inputLength; n++)
            {
                byte[] sample = BitConverter.GetBytes(input[n]);
                output[2 * n] = sample[0];
                output[2 * n + 1] = sample[1];
            }
            return output;
        }
    }
}
