using TRLevelReader.Model;

namespace TRLevelFlipper;

public class PositionInfo
{
    public int WorldX { get; set; }
    public int WorldZ { get; set; }
    public int SectorX { get; set; }
    public int SectorZ { get; set; }
    public int RoomSizeX { get; set; }
    public int RoomSizeZ { get; set;}

    public PositionInfo() { }

    public PositionInfo(int sectorX, int sectorZ, TRRoom room)
    {
        SectorX = sectorX;
        SectorZ = sectorZ;
        WorldX = room.Info.X + sectorX * TRConsts.SectorSize;
        WorldZ = room.Info.Z + sectorZ * TRConsts.SectorSize;
        RoomSizeX = room.NumXSectors;
        RoomSizeZ = room.NumZSectors;
    }

    public PositionInfo(int sectorX, int sectorZ, TR2Room room)
    {
        SectorX = sectorX;
        SectorZ = sectorZ;
        WorldX = room.Info.X + sectorX * TRConsts.SectorSize;
        WorldZ = room.Info.Z + sectorZ * TRConsts.SectorSize;
        RoomSizeX = room.NumXSectors;
        RoomSizeZ = room.NumZSectors;
    }

    public PositionInfo(int sectorX, int sectorZ, TR3Room room)
    {
        SectorX = sectorX;
        SectorZ = sectorZ;
        WorldX = room.Info.X + sectorX * TRConsts.SectorSize;
        WorldZ = room.Info.Z + sectorZ * TRConsts.SectorSize;
        RoomSizeX = room.NumXSectors;
        RoomSizeZ = room.NumZSectors;
    }
}
