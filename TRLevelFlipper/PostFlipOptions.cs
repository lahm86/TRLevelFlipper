using TRLevelReader.Helpers;
using TRLevelReader.Model;
using TRLevelReader.Model.Enums;

namespace TRLevelFlipper;

public class PostFlipOptions
{
    public bool Drain { get; set; }
    public bool OpenDoors { get; set; }

    public void Apply(TRLevel level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.Drain());

        if (OpenDoors)
            level.Entities.ToList()
                .FindAll(e => TR1EntityUtilities.IsDoorType((TREntities)e.TypeID))
                .ForEach(e => e.Flags = TRConsts.AllBits);
    }

    public void Apply(TR2Level level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.Drain());

        if (OpenDoors)
            level.Entities.ToList()
                .FindAll(e => TR2EntityUtilities.IsDoorType((TR2Entities)e.TypeID))
                .ForEach(e => e.Flags = TRConsts.AllBits);
    }

    public void Apply(TR3Level level)
    {
        if (Drain)
            level.Rooms.ToList().ForEach(r => r.ContainsWater = false);

        if (OpenDoors)
            level.Entities.ToList()
                .FindAll(e => TR3EntityUtilities.IsDoorType((TR3Entities)e.TypeID))
                .ForEach(e => e.Flags = TRConsts.AllBits);
    }
}
