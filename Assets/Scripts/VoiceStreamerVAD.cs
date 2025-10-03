// VoiceStreamerVAD.cs (音声再生中の録音停止機能を追加した修正版)

using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class VoiceStreamerVAD : MonoBehaviour
{
    private string serverUrl = "ws://10.24.195.76:8000/ws/transcribe";
    private WebSocket websocket;

    public float startDelay = 3.0f;

    private const int SAMPLING_RATE = 16000;
    private const int CHUNK_LENGTH_MS = 32;

    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip microphoneClip;
    private int lastPosition = 0;

    // ★★★ 変更点: ストリーミングコルーチンの参照を保持する変数 ★★★
    private Coroutine streamingCoroutine;

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
            StartCoroutine(DelayedStartStreaming(startDelay));
        };

        websocket.OnError += (e) => Debug.LogError("エラー: " + e);
        
        // ★★★ 変更点: サーバー切断時にも録音を停止する処理を追加 ★★★
        websocket.OnClose += (e) => 
        {
            Debug.Log("サーバーから切断されました。");
            // メインスレッドで録音停止処理を実行
            UnityMainThreadDispatcher.Instance().Enqueue(StopRecordingAndStreaming);
        };

        websocket.OnMessage += (bytes) =>
        {
            if (bytes.Length > 4 && 
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46) // 'RIFF'
            {
                Debug.Log($"WAVデータ ({bytes.Length} バイト) を受信しました。再生します。");
                UnityMainThreadDispatcher.Instance().Enqueue(() => PlayWavBytes(bytes));
            }
            else
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("テキスト応答: " + message);
            }
        };

        Debug.Log("サーバーに接続試行中...");
        await websocket.Connect();
    }

    // ★★★ 変更点: 音声再生ロジックを大幅に修正 ★★★
    /// <summary>
    /// WAV形式のバイト配列をAudioClipに変換し、再生する。
    /// 再生中は録音を停止し、再生完了後に録音を再開する。
    /// </summary>
    private void PlayWavBytes(byte[] wavBytes)
    {
        // まず現在の録音・ストリーミングを停止する
        StopRecordingAndStreaming();

        try
        {
            AudioClip clip = WavUtility.ToAudioClip(wavBytes);

            if (clip != null)
            {
                if (audioSource.isPlaying) audioSource.Stop();

                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log($"音声の再生を開始しました。長さ: {clip.length}秒");

                // 再生終了後に録音を再開するコルーチンを開始
                // 0.1秒のマージンを追加して、再生が完全に終わるのを待つ
                StartCoroutine(RestartStreamingAfterPlayback(clip.length + 0.1f));
            }
            else
            {
                Debug.LogError("WAVからAudioClipへの変換に失敗しました。録音を直ちに再開します。");
                // 失敗した場合は、すぐに録音を再開する
                StartRecordingAndStreaming();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"音声再生中にエラーが発生しました: {e.Message}。録音を直ちに再開します。");
            // エラーが発生した場合も、すぐに録音を再開する
            StartRecordingAndStreaming();
        }
    }

    // ★★★ 追加: 再生終了後に録音を再開するためのコルーチン ★★★
    private IEnumerator RestartStreamingAfterPlayback(float delay)
    {
        Debug.Log($"{delay:F1}秒後に録音を再開します。");
        yield return new WaitForSeconds(delay);
        StartRecordingAndStreaming();
    }
    
    private IEnumerator DelayedStartStreaming(float delay)
    {
        Debug.Log($"{delay}秒後に録音を開始します...");
        yield return new WaitForSeconds(delay);
        
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

    // ★★★ 変更点: 録音とストリーミングの開始処理をメソッドに集約 ★★★
    private void StartRecordingAndStreaming()
    {
        // すでに録音・実行中の場合は重複して開始しない
        if (Microphone.IsRecording(microphoneDevice) || streamingCoroutine != null)
        {
            Debug.LogWarning("すでに録音・ストリーミングが実行中のため、開始処理をスキップしました。");
            return;
        }

        Debug.Log("録音とストリーミングを開始します。");
        microphoneClip = Microphone.Start(microphoneDevice, true, 1, SAMPLING_RATE); 
        lastPosition = 0;
        streamingCoroutine = StartCoroutine(StreamAudio()); // コルーチンの参照を保持
    }

    // ★★★ 追加: 録音とストリーミングを停止するためのメソッド ★★★
    private void StopRecordingAndStreaming()
    {
        if (Microphone.IsRecording(microphoneDevice))
        {
            Debug.Log("録音を停止します。");
            Microphone.End(microphoneDevice);
        }
        if (streamingCoroutine != null)
        {
            Debug.Log("ストリーミングを停止します。");
            StopCoroutine(streamingCoroutine);
            streamingCoroutine = null;
        }
    }

    private IEnumerator StreamAudio()
    {
        int chunkSize = SAMPLING_RATE * CHUNK_LENGTH_MS / 1000;
        float[] chunk = new float[chunkSize];

        while (websocket != null && websocket.State == WebSocketState.Open)
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
            short intSample = (short)(Mathf.Clamp(sample, -1.0f, 1.0f) * 32767.0f);
            byte[] sampleBytes = BitConverter.GetBytes(intSample);
            bytes[byteIndex++] = sampleBytes[0];
            bytes[byteIndex++] = sampleBytes[1];
        }
        return bytes;
    }

    // ★★★ 変更点: アプリケーション終了時に録音停止処理を確実に行う ★★★
    async void OnApplicationQuit()
    {
        // まず録音とストリーミングを停止
        StopRecordingAndStreaming();

        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }
}