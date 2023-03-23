using System.Numerics;
using TRFDControl;
using TRFDControl.FDEntryTypes;
using TRFDControl.Utilities;
using TRLevelReader.Helpers;
using TRLevelReader.Model;
using TRLevelReader.Model.Enums;

namespace TRLevelFlipper;

public class YFlipper : IFlipper
{
    public FlipType FlipType => FlipType.Y;

    private sbyte _yClickAdjustment;

    public void Flip(TRLevel level)
    {
        CalculateYAdjustment(level.Rooms.Select(r => r.Info));

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        ReorganiseFloorData(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    public void Flip(TR2Level level)
    {
        CalculateYAdjustment(level.Rooms.Select(r => r.Info));

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        ReorganiseFloorData(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    public void Flip(TR3Level level)
    {
        CalculateYAdjustment(level.Rooms.Select(r => r.Info));

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        ReorganiseFloorData(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    private void CalculateYAdjustment(IEnumerable<TRRoomInfo> roomInfos)
    {
        int minY = int.MaxValue;
        foreach (TRRoomInfo info in roomInfos)
        {
            minY = Math.Min(info.YTop, minY);
        }

        _yClickAdjustment = (sbyte)(minY == short.MinValue ? 1 : 0);
    }

    private int FlipWorldY(int y)
    {
        y *= -1;
        y -= _yClickAdjustment * TRConsts.ClickSize;
        return y;
    }

    private short FlipWorldY(short y)
    {
        return (short)FlipWorldY((int)y);
    }

    private sbyte FlipClickY(sbyte y)
    {
        y *= -1;
        y -= _yClickAdjustment;
        return y;
    }

    private void MirrorFloorData(TRLevel level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TRRoom room in level.Rooms)
        {
            MirrorSectors(room.Sectors, floorData);
        }

        floorData.WriteToLevel(level);
    }

    private void MirrorFloorData(TR2Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TR2Room room in level.Rooms)
        {
            MirrorSectors(room.SectorList, floorData);
        }

        floorData.WriteToLevel(level);
    }

    private void MirrorFloorData(TR3Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TR3Room room in level.Rooms)
        {
            MirrorSectors(room.Sectors, floorData);
        }

        floorData.WriteToLevel(level);
    }

    private void MirrorSectors(TRRoomSector[] sectors, FDControl floorData)
    {
        foreach (TRRoomSector sector in sectors)
        {
            (sector.Ceiling, sector.Floor) = (sector.Floor, sector.Ceiling);
            (sector.RoomBelow, sector.RoomAbove) = (sector.RoomAbove, sector.RoomBelow);

            if (sector.Floor != TRConsts.Wall)
                sector.Floor = FlipClickY(sector.Floor);

            if (sector.Ceiling != TRConsts.Wall)
                sector.Ceiling = FlipClickY(sector.Ceiling);

            if (sector.FDIndex == 0)
                continue;
            
            List<FDEntry> entries = floorData.Entries[sector.FDIndex];

            for (int i = 0; i < entries.Count; i++)
            {
                FDEntry entry = entries[i];
                if (entry is FDSlantEntry slantEntry)
                {
                    FDSlantEntryType newType = slantEntry.Type == FDSlantEntryType.FloorSlant ? FDSlantEntryType.CeilingSlant : FDSlantEntryType.FloorSlant;
                    entries[i] = new FDSlantEntry
                    {
                        Setup = new FDSetup(newType == FDSlantEntryType.FloorSlant ? FDFunctions.FloorSlant : FDFunctions.CeilingSlant),
                        Type = newType,
                        XSlant = (sbyte)-slantEntry.XSlant,
                        ZSlant = slantEntry.ZSlant,
                    };
                }                    
                else if (entry is TR3TriangulationEntry triangulation)
                {
                    byte c00 = triangulation.TriData.C00;
                    byte c10 = triangulation.TriData.C10;
                    byte c01 = triangulation.TriData.C01;
                    byte c11 = triangulation.TriData.C11;

                    triangulation.TriData.C00 = c01;
                    triangulation.TriData.C01 = c00;
                    triangulation.TriData.C10 = c11;
                    triangulation.TriData.C11 = c10;

                    sbyte h1 = triangulation.Setup.H1;
                    sbyte h2 = triangulation.Setup.H2;

                    if (h1 != 0)
                        triangulation.Setup.H1 = (sbyte)((h1 - 31) * -1 + 1);
                    if (h2 != 0)
                        triangulation.Setup.H2 = (sbyte)((h2 - 31) * -1 + 1);

                    FDFunctions func = (FDFunctions)triangulation.Setup.Function;
                    switch (func)
                    {
                        // Non-portals
                        case FDFunctions.FloorTriangulationNWSE_Solid:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_Solid;
                            break;

                        case FDFunctions.FloorTriangulationNESW_Solid:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_Solid;
                            break;

                        case FDFunctions.CeilingTriangulationNW_Solid:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_Solid;
                            break;

                        case FDFunctions.CeilingTriangulationNE_Solid:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_Solid;
                            break;

                        // Portals: _SW, _NE etc indicate triangles whose right-angles point towards the portal
                        case FDFunctions.FloorTriangulationNWSE_SW:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_SW;
                            break;
                        case FDFunctions.FloorTriangulationNWSE_NE:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_NE;
                            break;
                        case FDFunctions.FloorTriangulationNESW_SE:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_SE;
                            break;
                        case FDFunctions.FloorTriangulationNESW_NW:
                            triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_NW;
                            break;

                        case FDFunctions.CeilingTriangulationNW_SW:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_SW;
                            break;
                        case FDFunctions.CeilingTriangulationNW_NE:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_NE;
                            break;
                        case FDFunctions.CeilingTriangulationNE_NW:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_NW;
                            break;
                        case FDFunctions.CeilingTriangulationNE_SE:
                            triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_SE;
                            break;
                    }
                }
            }

            // Floor entries must come before ceiling
            FDEntry floorSlant = entries.Find(e =>
                (e is FDSlantEntry slant && slant.Type == FDSlantEntryType.FloorSlant)
                || (e is TR3TriangulationEntry triangulation && (triangulation.IsFloorTriangulation || triangulation.IsFloorPortal)));
            FDEntry ceilingSlant = entries.Find(e =>
                (e is FDSlantEntry slant && slant.Type == FDSlantEntryType.CeilingSlant)
                || (e is TR3TriangulationEntry triangulation && !(triangulation.IsFloorTriangulation || triangulation.IsFloorPortal)));

            if (floorSlant != null)
                entries.Remove(floorSlant);
            
            if (ceilingSlant != null)
                entries.Remove(ceilingSlant);

            if (ceilingSlant != null)
                entries.Insert(0, ceilingSlant);

            if (floorSlant != null)
                entries.Insert(0, floorSlant);
        }
    }

    private static void ReorganiseFloorData(TRLevel level)
    {
        // Must be done after all rooms are repositioned
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TRRoom room in level.Rooms)
        {
            for (int x = 0; x < room.NumXSectors; x++)
            {
                for (int z = 0; z < room.NumZSectors; z++)
                {
                    TRRoomSector roomSector = room.Sectors[x * room.NumZSectors + z];
                    PositionInfo position = new(x, z, room);

                    MoveTriggersToFloor(roomSector, floorData, sector =>
                    {
                        while (sector.RoomBelow != TRConsts.NoRoom)
                        {
                            TRRoom roomBelow = level.Rooms[sector.RoomBelow];
                            int roomBelowX = (position.WorldX - roomBelow.Info.X) / TRConsts.SectorSize;
                            int roomBelowZ = (position.WorldZ - roomBelow.Info.Z) / TRConsts.SectorSize;
                            sector = roomBelow.Sectors[roomBelowX * roomBelow.NumZSectors + roomBelowZ];
                        }

                        return sector;
                    });

                    CreateWallPortals(position, roomSector, room.Sectors, floorData, roomAboveIdx =>
                    {
                        TRRoom roomAbove = level.Rooms[roomAboveIdx];
                        int roomAboveX = (position.WorldX - roomAbove.Info.X) / TRConsts.SectorSize;
                        int roomAboveZ = (position.WorldZ - roomAbove.Info.Z) / TRConsts.SectorSize;
                        return roomAbove.Sectors[roomAboveX * roomAbove.NumZSectors + roomAboveZ];
                    });
                }
            }
        }

        floorData.WriteToLevel(level);
    }

    private static void ReorganiseFloorData(TR2Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TR2Room room in level.Rooms)
        {
            for (int x = 0; x < room.NumXSectors; x++)
            {
                for (int z = 0; z < room.NumZSectors; z++)
                {
                    TRRoomSector roomSector = room.SectorList[x * room.NumZSectors + z];
                    PositionInfo position = new(x, z, room);

                    MoveTriggersToFloor(roomSector, floorData, sector =>
                    {
                        while (sector.RoomBelow != TRConsts.NoRoom)
                        {
                            TR2Room roomBelow = level.Rooms[sector.RoomBelow];
                            int roomBelowX = (position.WorldX - roomBelow.Info.X) / TRConsts.SectorSize;
                            int roomBelowZ = (position.WorldZ - roomBelow.Info.Z) / TRConsts.SectorSize;
                            sector = roomBelow.SectorList[roomBelowX * roomBelow.NumZSectors + roomBelowZ];
                        }

                        return sector;
                    });

                    CreateWallPortals(position, roomSector, room.SectorList, floorData, roomAboveIdx =>
                    {
                        TR2Room roomAbove = level.Rooms[roomAboveIdx];
                        int roomAboveX = (position.WorldX - roomAbove.Info.X) / TRConsts.SectorSize;
                        int roomAboveZ = (position.WorldZ - roomAbove.Info.Z) / TRConsts.SectorSize;
                        return roomAbove.SectorList[roomAboveX * roomAbove.NumZSectors + roomAboveZ];
                    });
                }
            }
        }

        floorData.WriteToLevel(level);
    }

    private static void ReorganiseFloorData(TR3Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);
        int r = 0;
        foreach (TR3Room room in level.Rooms)
        {
            r++;
            for (int x = 0; x < room.NumXSectors; x++)
            {
                for (int z = 0; z < room.NumZSectors; z++)
                {
                    TRRoomSector roomSector = room.Sectors[x * room.NumZSectors + z];
                    PositionInfo position = new(x, z, room);

                    MoveTriggersToFloor(roomSector, floorData, sector =>
                    {
                        while (sector.RoomBelow != TRConsts.NoRoom)
                        {
                            TR3Room roomBelow = level.Rooms[sector.RoomBelow];
                            int roomBelowX = (position.WorldX - roomBelow.Info.X) / TRConsts.SectorSize;
                            int roomBelowZ = (position.WorldZ - roomBelow.Info.Z) / TRConsts.SectorSize;
                            sector = roomBelow.Sectors[roomBelowX * roomBelow.NumZSectors + roomBelowZ];
                        }
                        return sector;
                    });

                    CreateWallPortals(position, roomSector, room.Sectors, floorData, roomAboveIdx =>
                    {
                        TR3Room roomAbove = level.Rooms[roomAboveIdx];
                        int roomAboveX = (position.WorldX - roomAbove.Info.X) / TRConsts.SectorSize;
                        int roomAboveZ = (position.WorldZ - roomAbove.Info.Z) / TRConsts.SectorSize;
                        return roomAbove.Sectors[roomAboveX * roomAbove.NumZSectors + roomAboveZ];
                    });
                }
            }
        }

        floorData.WriteToLevel(level);
    }

    private static void MoveTriggersToFloor(TRRoomSector sector, FDControl floorData, Func<TRRoomSector, TRRoomSector> sectorBelowCallback)
    {
        // Move ladders and triggers to the absolute floor
        List<FDEntry> triggers;
        if (sector.FDIndex == 0
            || sector.RoomBelow == TRConsts.NoRoom
            || (triggers = floorData.Entries[sector.FDIndex].FindAll(e => e is FDTriggerEntry || e is FDClimbEntry)).Count == 0)
        {
            return;
        }

        TRRoomSector sectorBelow = sectorBelowCallback(sector);
        if (sectorBelow.FDIndex == 0)
        {
            floorData.CreateFloorData(sectorBelow);
        }

        floorData.Entries[sectorBelow.FDIndex].AddRange(triggers);
        floorData.Entries[sector.FDIndex].RemoveAll(e => e is FDTriggerEntry);
        if (floorData.Entries[sector.FDIndex].Count == 0)
        {
            floorData.RemoveFloorData(sector);
        }
    }

    private static void CreateWallPortals(PositionInfo pos, TRRoomSector sector, TRRoomSector[] roomSectors, FDControl floorData, Func<byte, TRRoomSector> sectorAboveCallback)
    {
        // If it's not a wall or it already has a portal, don't change anything
        if (!sector.IsImpenetrable
            || (sector.FDIndex != 0 && floorData.Entries[sector.FDIndex].Find(e => e is FDPortalEntry) != null))
            return;

        for (int dx = -1; dx < 2; dx++)
        {
            for (int dz = -1; dz < 2; dz++)
            {
                // Skip intercardinal neighbours and the sector itself
                if (dx == dz)
                    continue;

                // Skip the void
                int neighbourX = pos.SectorX + dx;
                int neighbourZ = pos.SectorZ + dz;
                if (neighbourX < 0 || neighbourX >= pos.RoomSizeX
                    || neighbourZ < 0 || neighbourZ >= pos.RoomSizeZ)
                    continue;

                // Skip neighbouring walls and those with solid ceilings
                TRRoomSector adjNeighbour = roomSectors[neighbourX * pos.RoomSizeZ + neighbourZ];
                if (adjNeighbour.IsImpenetrable || adjNeighbour.RoomAbove == TRConsts.NoRoom)
                    continue;

                byte roomAbove;
                TRRoomSector upstairsNeighbour = sectorAboveCallback(adjNeighbour.RoomAbove);
                if (upstairsNeighbour.FDIndex != 0
                    && floorData.Entries[upstairsNeighbour.FDIndex].Find(e => e is FDPortalEntry) is FDPortalEntry portal)
                {
                    // The sector directly above this is a portal into
                    // another room e.g. Ganges rooms 13/3/11
                    roomAbove = (byte)portal.Room;
                }
                else if (upstairsNeighbour.IsImpenetrable)
                {
                    // Nothing but wall above.
                    continue;
                }
                else
                {
                    // The room above overlaps this wall and its neighbour,
                    // so use that directly e.g. Jungle rooms 4/20
                    roomAbove = adjNeighbour.RoomAbove;
                }

                if (sector.FDIndex == 0)
                    floorData.CreateFloorData(sector);

                floorData.Entries[sector.FDIndex].Add(new FDPortalEntry
                {
                    Setup = new FDSetup(FDFunctions.PortalSector),
                    Room = roomAbove
                });

                // Only ever one portal
                break;
            }
        }
    }

    private void MirrorRooms(TRLevel level)
    {
        foreach (TRRoom room in level.Rooms)
        {
            int oldRoomTop = room.Info.YTop;
            room.Info.YTop = FlipWorldY(room.Info.YBottom);
            room.Info.YBottom = FlipWorldY(oldRoomTop);

            List<TRRoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TRRoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];
                roomVertex.Vertex.Y = FlipWorldY(roomVertex.Vertex.Y);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TRRoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                vert.Vertex.Y = FlipWorldY(vert.Vertex.Y);
            }

            // Change visibility portal vertices and flip the normal for Y
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    vert.Y = FlipWorldY(vert.Y);
                }
                portal.Normal.Y *= -1;
            }

            // Move the lights to their new spots
            foreach (TRRoomLight light in room.Lights)
            {
                light.Y = FlipWorldY(light.Y);
            }

            // Move the static meshes
            foreach (TRRoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.Y = (uint)((short)mesh.Y * -1);
                mesh.Y += (uint)(_yClickAdjustment * TRConsts.ClickSize);
            }
        }
    }

    private void MirrorRooms(TR2Level level)
    {
        foreach (TR2Room room in level.Rooms)
        {
            int oldRoomTop = room.Info.YTop;
            room.Info.YTop = FlipWorldY(room.Info.YBottom);
            room.Info.YBottom = FlipWorldY(oldRoomTop);

            List<TR2RoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TR2RoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];
                roomVertex.Vertex.Y = FlipWorldY(roomVertex.Vertex.Y);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TR2RoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                vert.Vertex.Y = FlipWorldY(vert.Vertex.Y);
            }

            // Change visibility portal vertices and flip the normal for Y
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    vert.Y = FlipWorldY(vert.Y);
                }
                portal.Normal.Y *= -1;
            }

            // Move the lights to their new spots
            foreach (TR2RoomLight light in room.Lights)
            {
                light.Y = FlipWorldY(light.Y);
            }

            // Move the static meshes
            foreach (TR2RoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.Y = (uint)((short)mesh.Y * -1);
                mesh.Y += (uint)(_yClickAdjustment * TRConsts.ClickSize);
            }
        }
    }

    private void MirrorRooms(TR3Level level)
    {
        foreach (TR3Room room in level.Rooms)
        {
            int oldRoomTop = room.Info.YTop;
            room.Info.YTop = FlipWorldY(room.Info.YBottom);
            room.Info.YBottom = FlipWorldY(oldRoomTop);

            List<TR3RoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TR3RoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];
                roomVertex.Vertex.Y = FlipWorldY(roomVertex.Vertex.Y);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TR3RoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                vert.Vertex.Y = FlipWorldY(vert.Vertex.Y);
            }

            // Change visibility portal vertices and flip the normal for Y
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    vert.Y = FlipWorldY(vert.Y);
                }
                portal.Normal.Y *= -1;
            }

            // Move the lights to their new spots
            foreach (TR3RoomLight light in room.Lights)
            {
                light.Y = FlipWorldY(light.Y);
            }

            // Move the static meshes
            foreach (TR3RoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.Y = (uint)((short)mesh.Y * -1);
                mesh.Y += (uint)(_yClickAdjustment * TRConsts.ClickSize);
            }
        }
    }

    private void MirrorBoxes(TRLevel level)
    {
        MirrorBoxes(level.Boxes);
    }

    private void MirrorBoxes(TR2Level level)
    {
        MirrorBoxes(level.Boxes);
    }

    private void MirrorBoxes(TR3Level level)
    {
        MirrorBoxes(level.Boxes);
    }

    private void MirrorBoxes(TRBox[] boxes)
    {
        foreach (TRBox box in boxes)
        {
            box.TrueFloor = FlipWorldY(box.TrueFloor);
        }
    }

    private void MirrorBoxes(TR2Box[] boxes)
    {
        foreach (TR2Box box in boxes)
        {
            box.TrueFloor = FlipWorldY(box.TrueFloor);
        }
    }

    private void MirrorStaticMeshes(TRLevel level)
    {
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            return TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
        });
    }

    private void MirrorStaticMeshes(TR2Level level)
    {
        TRMesh hips = TRMeshUtilities.GetModelFirstMesh(level, TR2Entities.Lara);
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            TRMesh mesh = TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
            return mesh == hips ? null : mesh;
        });
    }

    private void MirrorStaticMeshes(TR3Level level)
    {
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            return TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
        });
    }

    private void MirrorStaticMeshes(TRStaticMesh[] staticMeshes, Func<TRStaticMesh, TRMesh> meshFunc)
    {
        foreach (TRStaticMesh staticMesh in staticMeshes)
        {
            TRMesh mesh = meshFunc(staticMesh);
            if (mesh == null)
                continue;

            foreach (TRVertex vert in mesh.Vertices)
            {
                vert.Y = FlipWorldY(vert.Y);
            }

            FlipBoundingBox(staticMesh.CollisionBox);
            FlipBoundingBox(staticMesh.VisibilityBox);
        }
    }

    private void FlipBoundingBox(TRBoundingBox box)
    {
        short min = box.MinY;
        short max = box.MaxY;
        box.MinY = FlipWorldY(max);
        box.MaxY = FlipWorldY(min);
    }

    private void MirrorEntities(TRLevel level)
    {
        foreach (TREntity entity in level.Entities)
        {
            entity.Y = FlipWorldY(entity.Y);
            AdjustTR1EntityPosition(level, entity);
        }
    }

    private void MirrorEntities(TR2Level level)
    {
        foreach (TR2Entity entity in level.Entities)
        {
            entity.Y = FlipWorldY(entity.Y);
            AdjustTR2EntityPosition(level, entity);
        }
    }

    private void MirrorEntities(TR3Level level)
    {
        foreach (TR2Entity entity in level.Entities)
        {
            entity.Y = FlipWorldY(entity.Y);
            AdjustTR3EntityPosition(level, entity);
        }
    }

    private static void AdjustTR1EntityPosition(TRLevel level, TREntity entity)
    {
        TREntities type = (TREntities)entity.TypeID;
        if (TR1EntityUtilities.IsDoorType(type))
        {
            TRMesh[] meshes = TRMeshUtilities.GetModelMeshes(level, type);
            int ymin = int.MaxValue;
            int ymax = int.MinValue;
            foreach (TRMesh mesh in meshes)
            {
                foreach (TRVertex vertex in mesh.Vertices)
                {
                    ymin = Math.Min(ymin, vertex.Y);
                    ymax = Math.Max(ymax, vertex.Y);
                }
            }
            int height = Math.Abs(ymax - ymin) + 255;
            int clicks = Math.Max(1, height / TRConsts.ClickSize);
            entity.Y += clicks * TRConsts.ClickSize;
            return;
        }

        if (type == TREntities.Lara || TR1EntityUtilities.IsAnyPickupType(type))
        {
            FDControl floorData = new();
            floorData.ParseFromLevel(level);

            int y = entity.Y;
            short room = entity.Room;
            TRRoomSector sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            while (sector.RoomBelow != TRConsts.NoRoom)
            {
                y = (sector.Floor + 1) * TRConsts.ClickSize;
                room = sector.RoomBelow;
                sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            }

            entity.Y = sector.Floor * TRConsts.ClickSize;
            entity.Room = room;

            if (sector.FDIndex != 0)
            {
                FDEntry entry = floorData.Entries[sector.FDIndex].Find(e => e is FDSlantEntry s && s.Type == FDSlantEntryType.FloorSlant);
                if (entry is FDSlantEntry slant)
                {
                    Vector4? bestMidpoint = TRUtils.GetBestSlantMidpoint(slant);
                    if (bestMidpoint.HasValue)
                    {
                        entity.Y += (int)bestMidpoint.Value.Y;
                    }
                }
            }
            return;
        }

        switch (type)
        {
            case TREntities.DartEmitter:
            case TREntities.WallSwitch:
            case TREntities.UnderwaterSwitch:
            case TREntities.CameraTarget_N:
            case TREntities.Lara:
            case TREntities.SavegameCrystal_P:
            case TREntities.FallingBlock:
            case TREntities.Keyhole1:
            case TREntities.Keyhole2:
            case TREntities.Keyhole3:
            case TREntities.Keyhole4:
            case TREntities.PuzzleHole1:
            case TREntities.PuzzleHole2:
            case TREntities.PuzzleHole3:
            case TREntities.PuzzleHole4:
            case TREntities.PushBlock1:
            case TREntities.PushBlock2:
            case TREntities.PushBlock3:
            case TREntities.PushBlock4:            
                entity.Y += TRConsts.SectorSize;
                break;

            case TREntities.AtlanteanEgg:
            case TREntities.ScionHolder:
                entity.Y += 2 * TRConsts.SectorSize;
                break;

            case TREntities.BridgeTilt1:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= TRConsts.ClickSize;
                break;
            case TREntities.BridgeTilt2:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= 2 * TRConsts.ClickSize;
                break;
        }
    }

    private static void AdjustTR2EntityPosition(TR2Level level, TR2Entity entity)
    {
        TR2Entities type = (TR2Entities)entity.TypeID;
        if (TR2EntityUtilities.IsDoorType(type))
        {
            TRMesh[] meshes = TRMeshUtilities.GetModelMeshes(level, type);
            int ymin = int.MaxValue;
            int ymax = int.MinValue;
            foreach (TRMesh mesh in meshes)
            {
                foreach (TRVertex vertex in mesh.Vertices)
                {
                    ymin = Math.Min(ymin, vertex.Y);
                    ymax = Math.Max(ymax, vertex.Y);
                }
            }
            int height = Math.Abs(ymax - ymin) + 255;
            int clicks = Math.Max(1, height / TRConsts.ClickSize);
            entity.Y += clicks * TRConsts.ClickSize;
            return;
        }

        if (type == TR2Entities.Lara || TR2EntityUtilities.IsAnyPickupType(type))
        {
            FDControl floorData = new();
            floorData.ParseFromLevel(level);

            int y = entity.Y;
            short room = entity.Room;
            TRRoomSector sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            while (sector.RoomBelow != TRConsts.NoRoom)
            {
                y = (sector.Floor + 1) * TRConsts.ClickSize;
                room = sector.RoomBelow;
                sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            }

            entity.Y = sector.Floor * TRConsts.ClickSize;
            entity.Room = room;

            if (sector.FDIndex != 0)
            {
                FDEntry entry = floorData.Entries[sector.FDIndex].Find(e => e is FDSlantEntry s && s.Type == FDSlantEntryType.FloorSlant);
                if (entry is FDSlantEntry slant)
                {
                    Vector4? bestMidpoint = TRUtils.GetBestSlantMidpoint(slant);
                    if (bestMidpoint.HasValue)
                    {
                        entity.Y += (int)bestMidpoint.Value.Y;
                    }
                }
            }
            return;
        }

        switch ((TR2Entities)entity.TypeID)
        {
            case TR2Entities.Discgun:
            case TR2Entities.WallSwitch:
            case TR2Entities.SmallWallSwitch:
            case TR2Entities.PushButtonSwitch:
            case TR2Entities.UnderwaterSwitch:
            case TR2Entities.CameraTarget_N:
            case TR2Entities.FallingBlock:
            case TR2Entities.BreakableWindow1:
            case TR2Entities.BreakableWindow2:
            case TR2Entities.Keyhole1:
            case TR2Entities.Keyhole2:
            case TR2Entities.Keyhole3:
            case TR2Entities.Keyhole4:
            case TR2Entities.PuzzleHole1:
            case TR2Entities.PuzzleHole2:
            case TR2Entities.PuzzleHole3:
            case TR2Entities.PuzzleHole4:
            case TR2Entities.PushBlock1:
            case TR2Entities.PushBlock2:
            case TR2Entities.PushBlock3:
            case TR2Entities.PushBlock4:
            case TR2Entities.PowerSaw:
            case TR2Entities.Elevator:
            case TR2Entities.WheelKnob:
                entity.Y += TRConsts.SectorSize;
                break;

            case TR2Entities.TibetanBell:
                entity.Y += 6 * TRConsts.ClickSize;
                break;

            case TR2Entities.SpikyWall:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                    case TRConsts.North:
                        entity.Y += 2 * TRConsts.SectorSize;
                        break;
                }
                break;
            case TR2Entities.Gong:
                entity.Y += 3 * TRConsts.SectorSize;
                break;

            case TR2Entities.StatueWithKnifeBlade:
                if (entity.Angle == TRConsts.East)
                {
                    entity.Angle = TRConsts.West;
                    entity.X += TRConsts.SectorSize;
                }
                else if (entity.Angle == TRConsts.West)
                {
                    entity.Angle = TRConsts.East;
                    entity.X -= TRConsts.SectorSize;
                }
                break;

            case TR2Entities.BridgeTilt1:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= TRConsts.ClickSize;
                break;
            case TR2Entities.BridgeTilt2:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= 2 * TRConsts.ClickSize;
                break;
        }
    }

    private static void AdjustTR3EntityPosition(TR3Level level, TR2Entity entity)
    {
        TR3Entities type = (TR3Entities)entity.TypeID;
        if (TR3EntityUtilities.IsDoorType(type))
        {
            TRMesh[] meshes = TRMeshUtilities.GetModelMeshes(level, type);
            int ymin = int.MaxValue;
            int ymax = int.MinValue;
            foreach (TRMesh mesh in meshes)
            {
                foreach (TRVertex vertex in mesh.Vertices)
                {
                    ymin = Math.Min(ymin, vertex.Y);
                    ymax = Math.Max(ymax, vertex.Y);
                }
            }
            int height = Math.Abs(ymax - ymin) + 255;
            int clicks = Math.Max(1, height / TRConsts.ClickSize);
            entity.Y += clicks * TRConsts.ClickSize;
            return;
        }

        if (type == TR3Entities.Lara
            || TR3EntityUtilities.IsAnyPickupType(type)
            || TR3EntityUtilities.IsPushblockType(type))
        {
            FDControl floorData = new();
            floorData.ParseFromLevel(level);

            int y = entity.Y;
            short room = entity.Room;
            TRRoomSector sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            while (sector.RoomBelow != TRConsts.NoRoom)
            {
                y = (sector.Floor + 1) * TRConsts.ClickSize;
                room = sector.RoomBelow;
                sector = FDUtilities.GetRoomSector(entity.X, y, entity.Z, room, level, floorData);
            }

            entity.Y = sector.Floor * TRConsts.ClickSize;
            entity.Room = room;

            if (sector.FDIndex != 0)
            {
                FDEntry entry = floorData.Entries[sector.FDIndex].Find(e => e is FDSlantEntry s && s.Type == FDSlantEntryType.FloorSlant);
                if (entry is FDSlantEntry slant)
                {
                    Vector4? bestMidpoint = TRUtils.GetBestSlantMidpoint(slant);
                    if (bestMidpoint.HasValue)
                    {
                        entity.Y += (int)bestMidpoint.Value.Y;
                    }
                }
            }
            return;
        }

        switch (type)
        {
            case TR3Entities.DartShooter:
            case TR3Entities.WallSwitch:
            case TR3Entities.SmallWallSwitch:
            case TR3Entities.PushButtonSwitch:
            case TR3Entities.RollingBallOrBarrel:
            case TR3Entities.TeethSpikesOrBarbedWire:
            case TR3Entities.FallingBlock:
            case TR3Entities.DestroyableBoardedUpWindow:
            case TR3Entities.Keyhole1:
            case TR3Entities.Keyhole2:
            case TR3Entities.Keyhole3:
            case TR3Entities.Keyhole4:
            case TR3Entities.Slot1Empty:
            case TR3Entities.Slot2Empty:
            case TR3Entities.Slot3Empty:
            case TR3Entities.Slot4Empty:
            case TR3Entities.PushableBlock1:
            case TR3Entities.PushableBlock2:
            case TR3Entities.SaveCrystal_P:
            case TR3Entities.FallingCeiling:
            case TR3Entities.SkeletonTrapOrSlammingDoor:
            case TR3Entities.ValveWheelOrPulley:
            case TR3Entities.AlarmLight:
            case TR3Entities.HangingRaptor:
            case TR3Entities.SubwayTrain:
            case TR3Entities.Quad:
            case TR3Entities.Tripwire_N:
            case TR3Entities.KillerTripwire_N:
            case TR3Entities.ElectrifiedWire_N:
                entity.Y += TRConsts.SectorSize;
                break;

            case TR3Entities.LaserSweeper:
                entity.Y += 2 * TRConsts.ClickSize;
                break;

            case TR3Entities.SpikyVertWallOrTunnelBorer:
            case TR3Entities.SpikyWall:
            case TR3Entities.DamagingAnimating1:
            case TR3Entities.DestroyableBoardedUpWall:
                entity.Y += 2 * TRConsts.SectorSize;
                break;

            case TR3Entities.BridgeTilt1:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= TRConsts.ClickSize;
                break;
            case TR3Entities.BridgeTilt2:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                }
                entity.Y -= 2 * TRConsts.ClickSize;
                break;
        }
    }

    private void MirrorNullMeshes(TRLevel level)
    {
        // The deals with actual cameras as well as sinks
        foreach (TRCamera camera in level.Cameras)
        {
            camera.Y = FlipWorldY(camera.Y);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.Y = FlipWorldY(sound.Y);
        }
    }

    private void MirrorNullMeshes(TR2Level level)
    {
        foreach (TRCamera camera in level.Cameras)
        {
            camera.Y = FlipWorldY(camera.Y);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.Y = FlipWorldY(sound.Y);
        }
    }

    private void MirrorNullMeshes(TR3Level level)
    {
        foreach (TRCamera camera in level.Cameras)
        {
            camera.Y = FlipWorldY(camera.Y);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.Y = FlipWorldY(sound.Y);
        }
    }

    private static void MirrorTextures(TRLevel level)
    {
        // Collect unique texture references from each of the rooms
        ISet<ushort> textureReferences = new HashSet<ushort>();

        // Keep track of static meshes so they are only processed once,
        // and so we only target those actually in use in rooms.
        List<TRStaticMesh> staticMeshes = level.StaticMeshes.ToList();
        ISet<TRStaticMesh> processedMeshes = new HashSet<TRStaticMesh>();

        foreach (TRRoom room in level.Rooms)
        {
            // Invert the faces, otherwise they are inside out
            foreach (TRFace4 f in room.RoomData.Rectangles)
            {
                TRUtils.Swap(f.Vertices, 0, 3);
                TRUtils.Swap(f.Vertices, 1, 2);

                textureReferences.Add(f.Texture);
            }

            foreach (TRFace3 f in room.RoomData.Triangles)
            {
                TRUtils.Swap(f.Vertices, 0, 2);
                textureReferences.Add(f.Texture);
            }

            foreach (TRRoomStaticMesh roomStaticMesh in room.StaticMeshes)
            {
                TRStaticMesh staticMesh = staticMeshes.Find(m => m.ID == roomStaticMesh.MeshID);
                if (!processedMeshes.Add(staticMesh))
                {
                    continue;
                }

                TRMesh mesh = TRMeshUtilities.GetMesh(level, staticMesh.Mesh);

                // Flip the faces and store texture references
                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                    textureReferences.Add(f.Texture);
                }

                foreach (TRFace4 f in mesh.ColouredRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                }

                foreach (TRFace3 f in mesh.TexturedTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                    textureReferences.Add(f.Texture);
                }

                foreach (TRFace3 f in mesh.ColouredTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                }
            }
        }

        // Include all animated texture references too
        foreach (TRAnimatedTexture anim in level.AnimatedTextures)
        {
            for (int i = 0; i < anim.Textures.Length; i++)
            {
                textureReferences.Add(anim.Textures[i]);
            }
        }

        MirrorObjectTextures(textureReferences, level.ObjectTextures);
        MirrorSpriteTextures(level);

        // Models such as doors may use textures also used on walls, but
        // these models aren't mirrored so the texture will end up being
        // upside down. Rotate the relevant mesh faces.
        MirrorDependentFaces(level.Models, textureReferences,
            modelID => TRMeshUtilities.GetModelMeshes(level, (TREntities)modelID));
    }

    private static void MirrorTextures(TR2Level level)
    {
        // Collect unique texture references from each of the rooms
        ISet<ushort> textureReferences = new HashSet<ushort>();

        // Keep track of static meshes so they are only processed once,
        // and so we only target those actually in use in rooms.
        List<TRStaticMesh> staticMeshes = level.StaticMeshes.ToList();
        ISet<TRStaticMesh> processedMeshes = new HashSet<TRStaticMesh>();

        TRMesh hips = TRMeshUtilities.GetModelFirstMesh(level, TR2Entities.Lara);

        foreach (TR2Room room in level.Rooms)
        {
            // Invert the faces, otherwise they are inside out
            foreach (TRFace4 f in room.RoomData.Rectangles)
            {
                TRUtils.Swap(f.Vertices, 0, 3);
                TRUtils.Swap(f.Vertices, 1, 2);
                textureReferences.Add(f.Texture);
            }

            foreach (TRFace3 f in room.RoomData.Triangles)
            {
                TRUtils.Swap(f.Vertices, 0, 2);
                textureReferences.Add(f.Texture);
            }

            foreach (TR2RoomStaticMesh roomStaticMesh in room.StaticMeshes)
            {
                TRStaticMesh staticMesh = staticMeshes.Find(m => m.ID == roomStaticMesh.MeshID);
                if (!processedMeshes.Add(staticMesh))
                {
                    continue;
                }

                TRMesh mesh = TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
                if (mesh == hips)
                    continue;

                // Flip the faces and store texture references
                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                    textureReferences.Add(f.Texture);
                }

                foreach (TRFace4 f in mesh.ColouredRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                }

                foreach (TRFace3 f in mesh.TexturedTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                    textureReferences.Add(f.Texture);
                }

                foreach (TRFace3 f in mesh.ColouredTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                }
            }
        }

        // Include all animated texture references too
        foreach (TRAnimatedTexture anim in level.AnimatedTextures)
        {
            for (int i = 0; i < anim.Textures.Length; i++)
            {
                textureReferences.Add(anim.Textures[i]);
            }
        }

        TRMesh[] skybox = TRMeshUtilities.GetModelMeshes(level, TR2Entities.Skybox_H);
        if (skybox != null)
        {
            foreach (TRMesh mesh in skybox)
            {
                foreach (TRVertex vert in mesh.Vertices)
                {
                    vert.Z *= -1;
                }

                if (mesh.Normals != null)
                {
                    foreach (TRVertex norm in mesh.Normals)
                    {
                        norm.Z *= -1;
                    }
                }

                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                    TRUtils.Swap(f.Vertices, 0, 2);
                    TRUtils.Swap(f.Vertices, 1, 3);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace4 f in mesh.ColouredRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                }

                foreach (TRFace3 f in mesh.TexturedTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace3 f in mesh.ColouredTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                }
            }
        }

        MirrorObjectTextures(textureReferences, level.ObjectTextures);

        MirrorDependentFaces(level.Models, textureReferences,
            modelID => TRMeshUtilities.GetModelMeshes(level, (TR2Entities)modelID));
    }

    private static void MirrorTextures(TR3Level level)
    {
        ISet<ushort> textureReferences = new HashSet<ushort>();

        List<TRStaticMesh> staticMeshes = level.StaticMeshes.ToList();
        ISet<TRStaticMesh> processedMeshes = new HashSet<TRStaticMesh>();

        foreach (TR3Room room in level.Rooms)
        {
            // Invert the faces, otherwise they are inside out
            foreach (TRFace4 f in room.RoomData.Rectangles)
            {
                TRUtils.Swap(f.Vertices, 0, 3);
                TRUtils.Swap(f.Vertices, 1, 2);
                textureReferences.Add((ushort)(f.Texture & 0x0fff));
            }

            foreach (TRFace3 f in room.RoomData.Triangles)
            {
                TRUtils.Swap(f.Vertices, 0, 2);
                textureReferences.Add((ushort)(f.Texture & 0x0fff));
            }

            foreach (TR3RoomStaticMesh roomStaticMesh in room.StaticMeshes)
            {
                TRStaticMesh staticMesh = staticMeshes.Find(m => m.ID == roomStaticMesh.MeshID);
                if (!processedMeshes.Add(staticMesh))
                {
                    continue;
                }

                TRMesh mesh = TRMeshUtilities.GetMesh(level, staticMesh.Mesh);

                // Flip the faces and store texture references
                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace4 f in mesh.ColouredRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                }

                foreach (TRFace3 f in mesh.TexturedTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace3 f in mesh.ColouredTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                }
            }
        }

        foreach (TRAnimatedTexture anim in level.AnimatedTextures)
        {
            for (int i = 0; i < anim.Textures.Length; i++)
            {
                textureReferences.Add(anim.Textures[i]);
            }
        }

        TRMesh[] skybox = TRMeshUtilities.GetModelMeshes(level, TR3Entities.Skybox_H);
        if (skybox != null)
        {
            foreach (TRMesh mesh in skybox)
            {
                foreach (TRVertex vert in mesh.Vertices)
                {
                    vert.Y *= -1;
                }

                if (mesh.Normals != null)
                {
                    foreach (TRVertex norm in mesh.Normals)
                    {
                        norm.Y *= -1;
                    }
                }

                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace4 f in mesh.ColouredRectangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 3);
                    TRUtils.Swap(f.Vertices, 1, 2);
                }

                foreach (TRFace3 f in mesh.TexturedTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                    textureReferences.Add((ushort)(f.Texture & 0x0fff));
                }

                foreach (TRFace3 f in mesh.ColouredTriangles)
                {
                    TRUtils.Swap(f.Vertices, 0, 2);
                }
            }
        }

        MirrorObjectTextures(textureReferences, level.ObjectTextures);
    }

    private static void MirrorObjectTextures(ISet<ushort> textureReferences, TRObjectTexture[] objectTextures)
    {
        // Flip the object texture vertices in the same way as done for faces
        foreach (ushort textureRef in textureReferences)
        {
            TRObjectTexture texture = objectTextures[textureRef];
            if (TRUtils.IsTriangle(texture))
            {
                TRUtils.Swap(texture.Vertices, 0, 2);
            }
            else
            {
                TRUtils.Swap(texture.Vertices, 0, 3);
                TRUtils.Swap(texture.Vertices, 1, 2);
            }
        }
    }

    private static void MirrorSpriteTextures(TRLevel level)
    {
        foreach (TRSpriteSequence sequence in level.SpriteSequences)
        {
            if (sequence.SpriteID >= 191 && sequence.SpriteID <= 239)
            {
                for (int i = 0; i < -sequence.NegativeLength; i++)
                {
                    TRSpriteTexture texture = level.SpriteTextures[sequence.Offset + i];
                    short top = texture.TopSide;
                    short left = texture.LeftSide;
                    texture.TopSide = texture.BottomSide;
                    texture.LeftSide = texture.RightSide;
                    texture.BottomSide = top;
                    texture.RightSide = left;
                }
            }
        }
    }

    private static void MirrorDependentFaces(TRModel[] models, ISet<ushort> textureReferences, Func<uint, TRMesh[]> meshAction)
    {
        foreach (TRModel model in models)
        {
            TRMesh[] meshes = meshAction.Invoke(model.ID);
            if (meshes == null)
            {
                continue;
            }

            foreach (TRMesh mesh in meshes)
            {
                foreach (TRFace4 f in mesh.TexturedRectangles)
                {
                    if (textureReferences.Contains(f.Texture))
                    {
                        TRUtils.Swap(f.Vertices, 0, 2);
                        TRUtils.Swap(f.Vertices, 1, 3);
                    }
                }
            }
        }
    }
}
