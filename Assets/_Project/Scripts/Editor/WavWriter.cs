using System.IO;
using System.Text;
using UnityEngine;

// Запись PCM-сэмплов в WAV-файл (16-bit, mono).
// Используется AssetForge для генерации звуков-плейсхолдеров без внешних
// аудио-ассетов. Готовые .wav-файлы кладутся в Audio/Generated/* и Unity
// импортирует их как обычные AudioClip.
public static class WavWriter
{
    public static void Write(string path, float[] samples, int sampleRate)
    {
        // 1. Готовим целевой каталог
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 2. PCM 16-bit, моно — стандартный совместимый формат
        var byteCount = samples.Length * 2;
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // 3. RIFF-заголовок
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + byteCount);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        // 4. fmt-чанк
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);              // размер fmt
        bw.Write((short)1);        // PCM
        bw.Write((short)1);        // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);  // byte rate (sr * channels * bits/8)
        bw.Write((short)2);        // block align
        bw.Write((short)16);       // bits per sample

        // 5. data-чанк
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(byteCount);
        for (int i = 0; i < samples.Length; i++)
        {
            var clamped = Mathf.Clamp(samples[i], -1f, 1f);
            var pcm = (short)(clamped * 32767f);
            bw.Write(pcm);
        }
    }
}
