using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace AudioExchangerCsConsole
{
    class Program
    {

        //バインドするローカルポート番号
        const int localPort = 50002;

        //リモートポート番号
        const int RemotePort = 50002;

        //最大UDPペイロードサイズ
        const int bufsize = 350;


        //エントリポイント
        static void Main(string[] args)
        {
            bool exitIgnoreToken = true; //falseにすると終了

            while (exitIgnoreToken)
            {
                ////出力音源のフォーマットを設定(16kHz, 16bit, 1ch)
                //var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

                ////バッファサイズを設定
                //bufferedWaveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 150);    //150ms分確保
                //bufferedWaveProvider.DiscardOnBufferOverflow = true;                    //バッファオーバーフロー時にDiscardするように設定

                ////再生デバイスと出力先を設定
                //var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                ////G.722コーデックを用意
                //var codec = new NAudio.Codecs.G722Codec();
                //var codecState = new NAudio.Codecs.G722CodecState(64000, NAudio.Codecs.G722Flags.None);

                //モード選択
                Console.WriteLine("モード（0=通話待受，1=通話開始，exit=終了）: ");
                Console.Write(">");
                string userInputString = Console.ReadLine();
                if (userInputString == "0")
                {
                    //通話待受モード

                    //リモートエンドポイントを生成
                    IPEndPoint remoteEndPoint = null;

                    //ローカルエンドポイントを生成
                    var localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

                    //受信待機
                    Console.WriteLine("Waiting RX...");
                    remoteEndPoint = WaitReceiveAudioPacketAsync(localEndPoint).Result;

                    //通話開始確認
                    Console.WriteLine($"接続元IPアドレス: {remoteEndPoint.Address}({remoteEndPoint.Port})");
                    Console.WriteLine("接続要求を承認しますか? [Y/N(既定)]:");
                    Console.Write(">");
                    if (Console.ReadLine().ToUpper() != "Y") continue;

                    //音声送受信開始
                    AudioExchange(localEndPoint, remoteEndPoint);

                }
                else if (userInputString == "1")
                {
                    //通話開始モード

                    //接続先のIPアドレスを入力
                    Console.Write("接続先IPアドレス：");
                    string remoteHost = Console.ReadLine();

                    //String型のIPアドレスをIPAddress型にParseしてremoteEndPointを更新
                    if (IPAddress.TryParse(remoteHost, out var remoteAddress) == false)
                    {
                        Console.WriteLine("正しくないIPアドレスが入力されました");
                        continue;
                    }

                    //リモートエンドポイントを生成
                    var remoteEndPoint = new IPEndPoint(remoteAddress, RemotePort);

                    //ローカルエンドポイントを生成
                    var localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

                    //音声送受信開始
                    AudioExchange(localEndPoint, remoteEndPoint);

                    ////UdpClientオブジェクトを生成
                    //var udp = new UdpClient();
                    //
                    //using (var waveIn = new WaveInEvent())
                    //{
                    //    //入力バッファのサイズを設定(20ms)
                    //    waveIn.BufferMilliseconds = 20;
                    //
                    //    //入力フォーマット設定(16kHz, 16bit, 1ch)
                    //    waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
                    //
                    //    //音声データ利用可能時の処理(非同期)
                    //    waveIn.DataAvailable += async (_, e) =>
                    //    {
                    //        await SendAudioPacketAsync(udp, localEndPoint, remoteEndPoint, e, codec, codecState);
                    //    };
                    //
                    //    //音声の取得開始
                    //    waveIn.StartRecording();
                    //    Console.WriteLine("UDP TX begin.");
                    //
                    //    //UDPパケットを受付開始
                    //    Task t = StartReceiveAudioPacketAsync(udp, localEndPoint, remoteEndPoint, bufferedWaveProvider, codec, codecState);
                    //    Console.WriteLine("UDP RX bigin.");
                    //
                    //    using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 20))
                    //    {
                    //        //出力に入力バッファを接続して再生開始
                    //        wavPlayer.Init(bufferedWaveProvider);
                    //        wavPlayer.Play();
                    //
                    //        Console.WriteLine("Press ENTER to exit...");
                    //        Console.ReadLine();
                    //
                    //        wavPlayer.Stop();
                    //    }
                    //
                    //    //音声の取得終了
                    //    waveIn.StopRecording();
                    //}
                    //
                    //udp.Close();

                }
                else if (userInputString.ToLower() == "exit")
                {
                    //終了
                    exitIgnoreToken = false;
                }
            }

        }


        //音声送受信ループ
        static void AudioExchange(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            //出力音源のフォーマットを設定(16kHz, 16bit, 1ch)
            var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

            //バッファサイズを設定
            bufferedWaveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 200);    //200ms分確保
            bufferedWaveProvider.DiscardOnBufferOverflow = true;                    //バッファオーバーフロー時にDiscardするように設定

            //再生デバイスと出力先を設定
            var mmDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            //G.722コーデックを用意
            var codec = new NAudio.Codecs.G722Codec();
            var codecState = new NAudio.Codecs.G722CodecState(64000, NAudio.Codecs.G722Flags.None);

            //UdpClientオブジェクトを生成
            var udp = new UdpClient(localEndPoint);

            using (var waveIn = new WaveInEvent())
            {
                //入力バッファのサイズを設定(20ms)
                waveIn.BufferMilliseconds = 15;

                //入力フォーマット設定(16kHz, 16bit, 1ch)
                waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

                //音声データ利用可能時の処理(非同期)
                waveIn.DataAvailable += async (_, e) =>
                                    {
                                        await SendAudioPacketAsync(udp, remoteEndPoint, e, bufferedWaveProvider,codec, codecState);
                                    };

                //音声の取得開始
                waveIn.StartRecording();
                Console.WriteLine("UDP TX begin.");

                //UDPパケットを受付開始
                //Task t = StartReceiveAudioPacketAsync(udp, remoteEndPoint, bufferedWaveProvider, codec, codecState);
                Console.WriteLine("UDP RX bigin.");

                using (IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 30))
                {
                    //出力に入力バッファを接続して再生開始
                    wavPlayer.Init(bufferedWaveProvider);
                    wavPlayer.Play();

                    Console.WriteLine($"[通話中] ({localEndPoint.Port})<-->{remoteEndPoint.Address}({remoteEndPoint.Port})");
                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();

                    wavPlayer.Stop();
                }


                //音声の取得終了
                waveIn.StopRecording();
                waveIn.Dispose();
            }

            Task.Delay(250);

            udp.Close();
        }

        //UDP音声パケット受信待ちメソッド
        static async Task<IPEndPoint> WaitReceiveAudioPacketAsync(IPEndPoint localEndPoint)
        {
            IPEndPoint remoteEndPoint = null;

            using (var udp = new UdpClient(localEndPoint))
            {
                while (true)
                {
                    if (udp.Available > 0)
                    {
                        byte[] rcvBytes = udp.Receive(ref remoteEndPoint);

                        //送信元アドレスを返す
                        return remoteEndPoint;
                    }

                    await Task.Delay(50);
                }
            }
        }

        //UDP音声パケット非同期受信メソッド
        static async Task StartReceiveAudioPacketAsync(UdpClient udp, IPEndPoint remoteEndPoint, BufferedWaveProvider provider, NAudio.Codecs.G722Codec codec, NAudio.Codecs.G722CodecState codecState)
        {
            short[] bufferedData = new short[350];
            IPEndPoint remoteEP = null;

            for (;;)
            {
                //データを受信する
                while (udp.Available > 0)
                {
                    byte[] rcvBytes = udp.Receive(ref remoteEP);

                    int bufferdLength = codec.Decode(codecState, bufferedData, rcvBytes, rcvBytes.Length);

                    byte[] bufferdBytes = ConvertShortTo16Bit(bufferedData, bufferdLength);

                    //バッファに追加
                    provider.AddSamples(bufferdBytes, 0, bufferdBytes.Length);

                    Console.WriteLine($"RX: {bufferdBytes.Length}");
                    Console.WriteLine("buffered= " + provider.BufferedBytes);
                }
                await Task.Delay(5);
            }

        }

        //UDP音声パケット非同期送信メソッド
        static async Task SendAudioPacketAsync(UdpClient udp, IPEndPoint remoteEndPoint, WaveInEventArgs e, BufferedWaveProvider provider, NAudio.Codecs.G722Codec codec, NAudio.Codecs.G722CodecState codecState)
        {
            //データを受信する
            short[] bufferedRxData = new short[350];
            IPEndPoint remoteEP = null;
            
            while (udp.Available > 0)
            {
                byte[] rcvBytes = udp.Receive(ref remoteEP);

                int bufferdLength = codec.Decode(codecState, bufferedRxData, rcvBytes, rcvBytes.Length);

                byte[] bufferdBytes = ConvertShortTo16Bit(bufferedRxData, bufferdLength);

                //バッファに追加
                provider.AddSamples(bufferdBytes, 0, bufferdBytes.Length);

                Console.WriteLine($"RX: {bufferdBytes.Length}");
                Console.WriteLine("buffered= " + provider.BufferedBytes);
            }


            //データを送信する
            byte[] bufferedBytes = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, bufferedBytes, e.BytesRecorded);

            short[] bufferedTxData = Convert16BitToShort(bufferedBytes);
            byte[] encodedBytes = new byte[e.BytesRecorded];
            int encodedLength = codec.Encode(codecState, encodedBytes, bufferedTxData, bufferedTxData.Length);

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

            Console.WriteLine($"TX: {encodedLength}");

            //await Task.Delay(10);
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
