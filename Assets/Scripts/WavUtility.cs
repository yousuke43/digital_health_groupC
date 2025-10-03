// WavUtility.cs (新しいファイルとしてプロジェクトに追加)
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class WavUtility
{
    // WAVデータの最小ヘッダ長（Unityが扱える形式を想定）
    private const int HEADER_SIZE = 44;

    /// <summary>
    /// WAV形式のバイト配列をAudioClipに変換する
    /// </summary>
    public static AudioClip ToAudioClip(byte[] wavBytes)
    {
        if (wavBytes == null || wavBytes.Length < HEADER_SIZE)
        {
            Debug.LogError("WAVデータが不正または小さすぎます。");
            return null;
        }

        try
        {
            // UnityがAudioClipを作成するために必要な情報をWAVヘッダから抽出する
            // チャンネル数 (Offset 22, 2 bytes)
            int channels = BitConverter.ToInt16(wavBytes, 22);
            // サンプリングレート (Offset 24, 4 bytes)
            int sampleRate = BitConverter.ToInt32(wavBytes, 24);
            // データサイズ (Offset 40, 4 bytes)
            int dataSize = BitConverter.ToInt32(wavBytes, 40);

            // PCM 16-bitでデータは44バイト目から始まる
            int pcmDataOffset = HEADER_SIZE;
            int numSamples = dataSize / 2 / channels; // 16bit = 2バイト

            // データ部分のみを抽出
            if (numSamples <= 0 || pcmDataOffset + dataSize > wavBytes.Length)
            {
                Debug.LogError($"WAVデータサイズ ({dataSize}バイト) またはサンプル数 ({numSamples}) が不正です。");
                return null;
            }

            // PCM (16bit short) データを float (-1.0 to 1.0) に変換
            float[] audioData = new float[numSamples * channels];
            for (int i = 0; i < numSamples * channels; i++)
            {
                // 16-bit little-endian shortを読み込み
                short sample = BitConverter.ToInt16(wavBytes, pcmDataOffset + i * 2);
                // shortを float に正規化
                audioData[i] = sample / 32768f;
            }

            // AudioClipを作成
            AudioClip audioClip = AudioClip.Create("GeneratedVoice", numSamples, channels, sampleRate, false);
            audioClip.SetData(audioData, 0);

            return audioClip;
        }
        catch (Exception e)
        {
            Debug.LogError($"WAVからAudioClipへの変換エラー: {e.Message}");
            return null;
        }
    }
}