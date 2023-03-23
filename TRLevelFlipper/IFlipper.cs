using TRLevelReader.Model;

namespace TRLevelFlipper;

public interface IFlipper
{
    FlipType FlipType { get; }
    void Flip(TRLevel level);
    void Flip(TR2Level level);
    void Flip(TR3Level level);
}
