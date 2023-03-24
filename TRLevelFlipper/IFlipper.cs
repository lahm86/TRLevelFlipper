using TRLevelReader.Model;

namespace TRLevelFlipper;

public interface IFlipper
{
    FlipType FlipType { get; }
    void Flip(string levelName, TRLevel level);
    void Flip(string levelName, TR2Level level);
    void Flip(string levelName, TR3Level level);
}
