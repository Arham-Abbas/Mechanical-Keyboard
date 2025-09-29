using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mechanical_Keyboard.Models
{
    public class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        // Existing constructor that loads from a file path
        public CachedSound(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Audio file not found.", audioFilePath);
            }

            using var audioFileReader = new AudioFileReader(audioFilePath);
            WaveFormat = audioFileReader.WaveFormat;
            var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
            var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
            int samplesRead;
            while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }
            AudioData = [.. wholeFile];
        }

        public CachedSound(float[] audioData, WaveFormat waveFormat)
        {
            AudioData = audioData;
            WaveFormat = waveFormat;
        }
    }
}
