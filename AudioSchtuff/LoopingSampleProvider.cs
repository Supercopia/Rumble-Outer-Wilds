using NAudio.Wave;

namespace AudioSchtuff;

public class LoopingSampleProvider : ISampleProvider
{
    private readonly AudioFileReader _sourceStream;
    public bool IsLooping { get; set; }

    public LoopingSampleProvider(AudioFileReader sourceStream, bool isLooping = true)
    {
        _sourceStream = sourceStream;
        IsLooping = isLooping;
    }

    public WaveFormat WaveFormat => _sourceStream.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int totalSamplesRead = 0;

        while (totalSamplesRead < count)
        {
            // Read from the source file
            int samplesRead = _sourceStream.Read(buffer, offset + totalSamplesRead, count - totalSamplesRead);
            
            if (samplesRead == 0) // We hit the end of the file
            {
                if (IsLooping)
                {
                    _sourceStream.Position = 0; // Rewind to the start
                }
                else
                {
                    break; // Stop providing data
                }
            }
            totalSamplesRead += samplesRead;
        }
        return totalSamplesRead;
    }
}