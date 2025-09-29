using NAudio.Wave;
using System.IO;

namespace Mechanical_Keyboard.Models
{
    public class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        public CachedSound(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Audio file not found.", audioFilePath);
            }

            using var reader = new AudioFileReader(audioFilePath);
            WaveFormat = reader.WaveFormat;
            var wholeFile = new float[reader.Length];
            reader.Read(wholeFile, 0, (int)reader.Length);
            AudioData = wholeFile;
        }
    }
}
