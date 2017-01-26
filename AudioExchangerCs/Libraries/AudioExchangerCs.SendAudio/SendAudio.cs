using NAudio.Wave;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AudioExchangerCs
{
    public class SendAudio
    {
        /// <summary>
        /// ローカルポート番号
        /// </summary>
        public int LocalPort { get; set; } = 50002;

        /// <summary>
        /// リモートポート番号
        /// </summary>
        public int RemotePort { get; set; } = 50003;

        /// <summary>
        /// 最大UDPペイロードサイズ
        /// </summary>
        public int MaxPacketPayloadSize { get; set; } = 350;

        /// <summary>
        /// UDP音声パケットの送信を開始します
        /// </summary>
        /// <param name="remoteAddress">リモートIPアドレス</param>
        /// <param name="cancellTokenSource">受信を停止するためのCancellationTokenSource</param>
        /// <returns></returns>
        public async Task StartSendAsync(IPAddress remoteAddress, CancellationTokenSource cancellTokenSource)
        {
            //IPエンドポイントを生成
            var remoteEndPoint = new IPEndPoint(remoteAddress, RemotePort);
            var localEndPoint = new IPEndPoint(IPAddress.Any, LocalPort);

            //UdpClientオブジェクトを生成
            var udp = new System.Net.Sockets.UdpClient(localEndPoint);

            using (var waveIn = new WaveInEvent())
            {
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
                    int encodedLength = codec.Encode(codecState, encodedBytes, bufferedData, bufferedData.Length);

                    byte[] bufferToSend = new byte[MaxPacketPayloadSize];

                    for (int i = 0; i < encodedLength; i += MaxPacketPayloadSize)
                    {
                        if (encodedLength > i + MaxPacketPayloadSize)
                        {
                            //バッファ内のデータがペイロードサイズ以上
                            Array.Copy(encodedBytes, i, bufferToSend, 0, MaxPacketPayloadSize);
                            await udp.SendAsync(bufferToSend, bufferToSend.Length, remoteEndPoint);
                        }
                        else
                        {
                            //バッファ内のサイズがペイロードサイズ以下
                            Array.Copy(encodedBytes, i, bufferToSend, 0, encodedLength - i);
                            await udp.SendAsync(bufferToSend, encodedLength - i, remoteEndPoint);
                        }
                    }
                    
                    await Task.Delay(10);
                };

                //入力フォーマット設定(16kHz, 16bit, 1ch)
                waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

                //音声の取得開始
                waveIn.StartRecording();

                while (true)
                {
                    if (cancellTokenSource.IsCancellationRequested) break;
                    await Task.Delay(100);
                }
               
                //音声の取得終了
                waveIn.StopRecording();
            }

            udp.Close();
        }
        
        //byte型配列からShort型配列に変換するメソッド
        static private short[] Convert16BitToShort(byte[] input)
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
