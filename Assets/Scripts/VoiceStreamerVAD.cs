// VoiceStreamerVAD.cs (トグルボタン対応・整形済み完成版)

using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System;
using System.Linq;
using TMPro; // ボタンのテキストを変えるため追加

[RequireComponent(typeof(AudioSource))]
public class VoiceStreamerVAD : MonoBehaviour
{
    // --- 変数の定義 ---
    private string serverUrl = "ws://10.24.195.76:8000/ws/transcribe";
    private WebSocket websocket;
    public float startDelay = 0.5f;
    private const int SAMPLING_RATE = 16000;
    private const int CHUNK_LENGTH_MS = 32;

    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip microphoneClip;
    private int lastPosition = 0;
    private Coroutine streamingCoroutine;

    [Header("UI Settings")]
    public TextMeshProUGUI buttonText; // ボタンのテキストコンポーネント

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません！");
            return;
        }
        microphoneDevice = Microphone.devices[0];
        Debug.Log("マイクの準備ができました。");
        UpdateButtonText(false);
    }

    /// <summary>
    /// 接続と切断を切り替えるトグルメソッド (ボタンから呼び出す)
    /// </summary>
    public void ToggleConnection()
    {
        bool isConnectedOrConnecting = (websocket != null && 
                                       (websocket.State == WebSocketState.Open || 
                                        websocket.State == WebSocketState.Connecting));
        
        if (isConnectedOrConnecting)
        {
            StopConnectionAsync();
        }
        else
        {
            StartConnectionAsync();
        }
    }

    private async void StartConnectionAsync()
    {
        Debug.Log("サーバーへの接続を開始します...");
        UpdateButtonText(true);
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("サーバーに接続しました。");
            StartCoroutine(DelayedStartStreaming(startDelay));
        };

        websocket.OnError += (e) => 
        {
            Debug.LogError("エラー: " + e);
            UpdateButtonText(false);
        };
        
        websocket.OnClose += (e) => 
        {
            Debug.Log("サーバーから切断されました。");
            UnityMainThreadDispatcher.Instance().Enqueue(StopRecordingAndStreaming);
            UpdateButtonText(false);
        };

        websocket.OnMessage += (bytes) =>
        {
            if (bytes.Length > 4 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
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

        await websocket.Connect();
    }
    
    private async void StopConnectionAsync()
    {
        StopRecordingAndStreaming();
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log("サーバーから切断します。");
            await websocket.Close();
        }
        websocket = null;
    }

    private void UpdateButtonText(bool isConnecting)
    {
        if (buttonText != null)
        {
            buttonText.text = isConnecting ? "stop" : "talk";
        }
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

    private void PlayWavBytes(byte[] wavBytes)
    {
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
                StartCoroutine(RestartStreamingAfterPlayback(clip.length + 0.1f));
            }
            else
            {
                Debug.LogError("WAVからAudioClipへの変換に失敗しました。録音を直ちに再開します。");
                StartRecordingAndStreaming();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"音声再生中にエラーが発生しました: {e.Message}。録音を直ちに再開します。");
            StartRecordingAndStreaming();
        }
    }

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
    
    private void StartRecordingAndStreaming()
    {
        if (Microphone.IsRecording(microphoneDevice) || streamingCoroutine != null)
        {
            Debug.LogWarning("すでに録音・ストリーミングが実行中のため、開始処理をスキップしました。");
            return;
        }
        Debug.Log("録音とストリーミングを開始します。");
        microphoneClip = Microphone.Start(microphoneDevice, true, 1, SAMPLING_RATE);
        lastPosition = 0;
        streamingCoroutine = StartCoroutine(StreamAudio());
    }

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
            if (currentPosition < lastPosition) { lastPosition = 0; }
            if (currentPosition - lastPosition >= chunkSize)
            {
                microphoneClip.GetData(chunk, lastPosition);
                byte[] bytes = ConvertFloatToInt16Bytes(chunk);
                if (websocket.State == WebSocketState.Open) { websocket.Send(bytes); }
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

    async void OnApplicationQuit()
    {
        StopConnectionAsync();
    }
}