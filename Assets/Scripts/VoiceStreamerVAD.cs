// VoiceStreamerVAD.cs (3秒後に開始する修正版)

using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoiceStreamerVAD : MonoBehaviour
{
    private string serverUrl = "ws://localhost:8000/ws/transcribe";
    private WebSocket websocket;

    // ★★★★★ 録音開始までの待機時間（秒）★★★★★
    public float startDelay = 3.0f;

    private const int SAMPLING_RATE = 16000;
    private const int CHUNK_LENGTH_MS = 32;

    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip microphoneClip;
    private int lastPosition = 0;

    async void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません！");
            return;
        }
        microphoneDevice = Microphone.devices[0];

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("サーバーに接続しました。");
            
            // ★★★★★ 変更点：直接開始せず、遅延させるコルーチンを呼び出す ★★★★★
            StartCoroutine(DelayedStartStreaming(startDelay));
        };

        websocket.OnError += (e) => Debug.LogError("エラー: " + e);
        websocket.OnClose += (e) => Debug.Log("サーバーから切断されました。");
        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("文字起こし結果: " + message);
        };

        Debug.Log("サーバーに接続試行中...");
        await websocket.Connect();
    }

    // ★★★★★ 追加したコルーチン ★★★★★
    /// <summary>
    /// 指定した秒数だけ待ってから、録音とストリーミングを開始する
    /// </summary>
    private IEnumerator DelayedStartStreaming(float delay)
    {
        Debug.Log($"{delay}秒後に録音を開始します...");
        // 指定した秒数だけ処理を待つ
        yield return new WaitForSeconds(delay);
        
        // 待機後、録音とストリーミングを開始する
        StartRecordingAndStreaming();
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.DispatchMessageQueue();
        }
        #endif
    }

    private void StartRecordingAndStreaming()
    {
        Debug.Log("録音とストリーミングを開始します。");
        microphoneClip = Microphone.Start(microphoneDevice, true, 1, SAMPLING_RATE);
        StartCoroutine(StreamAudio());
    }

    private IEnumerator StreamAudio()
    {
        int chunkSize = SAMPLING_RATE * CHUNK_LENGTH_MS / 1000;
        float[] chunk = new float[chunkSize];

        while (websocket.State == WebSocketState.Open)
        {
            int currentPosition = Microphone.GetPosition(microphoneDevice);

            if (currentPosition < lastPosition)
            {
                lastPosition = 0;
            }

            if (currentPosition - lastPosition >= chunkSize)
            {
                microphoneClip.GetData(chunk, lastPosition);
                byte[] bytes = ConvertFloatToInt16Bytes(chunk);

                if (websocket.State == WebSocketState.Open)
                {
                    websocket.Send(bytes);
                }
                lastPosition += chunkSize;
            }
            yield return null;
        }
    }

    private byte[] ConvertFloatToInt16Bytes(float[] data)
    {
        byte[] bytes = new byte[data.Length * 2];
        int byteIndex = 0;
        foreach (var sample in data)
        {
            short intSample = (short)(sample * 32767.0f);
            byte[] sampleBytes = BitConverter.GetBytes(intSample);
            bytes[byteIndex++] = sampleBytes[0];
            bytes[byteIndex++] = sampleBytes[1];
        }
        return bytes;
    }

    async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }
}