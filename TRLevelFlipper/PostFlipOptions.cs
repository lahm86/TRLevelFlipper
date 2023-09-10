using TRLevelReader.Helpers;
using TRLevelReader.Model;
using TRLevelReader.Model.Enums;

namespace TRLevelFlipper;

public class PostFlipOptions
{
    public bool Drain { get; set; }
    public bool RemoveDoors { get; set; }
    public bool RemovePushblocks { get; set; }
    public bool RemoveTiles { get; set; }
    public List<int> ExtraRemovals { get; set; }

    public void Apply(TRLevel level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.Drain());

        if (RemoveDoors)
            level.Entities.ToList()
                .FindAll(e => IsDoor((TREntities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TREntities.CameraTarget_N);

        if (RemovePushblocks)
            level.Entities.ToList()
                .FindAll(e => TR1EntityUtilities.IsPushblockType((TREntities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TREntities.CameraTarget_N);

        if (RemoveTiles)
            level.Entities.ToList()
                .FindAll(e => e.TypeID == (short)TREntities.FallingBlock)
                .ForEach(e => e.TypeID = (short)TREntities.CameraTarget_N);

        if (ExtraRemovals != null)
            level.Entities
                .Where(e => ExtraRemovals.Contains(e.TypeID))
                .ToList()
                .ForEach(e => e.TypeID = (short)TREntities.CameraTarget_N);
    }

    public void Apply(TR2Level level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.Drain());

        if (RemoveDoors)
            level.Entities.ToList()
                .FindAll(e => IsDoor((TR2Entities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TR2Entities.CameraTarget_N);

        if (RemovePushblocks)
            level.Entities.ToList()
                .FindAll(e => TR2EntityUtilities.IsPushblockType((TR2Entities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TR2Entities.CameraTarget_N);

        if (RemoveTiles)
            level.Entities.ToList()
                .FindAll(e => e.TypeID == (short)TR2Entities.FallingBlock)
                .ForEach(e => e.TypeID = (short)TR2Entities.CameraTarget_N);

        if (ExtraRemovals != null)
            level.Entities
                .Where(e => ExtraRemovals.Contains(e.TypeID))
                .ToList()
                .ForEach(e => e.TypeID = (short)TR2Entities.CameraTarget_N);
    }

    public void Apply(TR3Level level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.ContainsWater = false);

        if (RemoveDoors)
            level.Entities.ToList()
                .FindAll(e => IsDoor((TR3Entities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TR3Entities.LookAtItem_H);

        if (RemovePushblocks)
            level.Entities.ToList()
                .FindAll(e => TR3EntityUtilities.IsPushblockType((TR3Entities)e.TypeID))
                .ForEach(e => e.TypeID = (short)TR3Entities.LookAtItem_H);

        if (RemoveTiles)
            level.Entities.ToList()
                .FindAll(e => e.TypeID == (short)TR3Entities.FallingBlock)
                .ForEach(e => e.TypeID = (short)TR3Entities.LookAtItem_H);

        if (ExtraRemovals != null)
            level.Entities
                .Where(e => ExtraRemovals.Contains(e.TypeID))
                .ToList()
                .ForEach(e => e.TypeID = (short)TR3Entities.LookAtItem_H);
    }

    static bool IsDoor(TREntities e)
    {
        return TR1EntityUtilities.IsDoorType(e)
            || e == TREntities.Trapdoor1
            || e == TREntities.Trapdoor2
            || e == TREntities.Trapdoor3;
    }

    static bool IsDoor(TR2Entities e)
    {
        return TR2EntityUtilities.IsDoorType(e)
            || e == TR2Entities.Trapdoor1
            || e == TR2Entities.Trapdoor2
            || e == TR2Entities.Trapdoor3;
    }

    static bool IsDoor(TR3Entities e)
    {
        return TR3EntityUtilities.IsDoorType(e)
            || e == TR3Entities.Trapdoor1
            || e == TR3Entities.Trapdoor2
            || e == TR3Entities.Trapdoor3;
    }
}
