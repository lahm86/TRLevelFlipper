namespace TRLevelFlipper;

public static class TRConsts
{
    public const int East = (short.MaxValue + 1) / 2;
    public const int North = 0;
    public const int West = East * -1;
    public const int South = short.MinValue;

    public const int ClickSize = 256;
    public const int SectorSize = 1024;
    public const int Wall = -127;
    public const int NoRoom = 255;

    public const ushort AllBits = 0x3E00;
}
