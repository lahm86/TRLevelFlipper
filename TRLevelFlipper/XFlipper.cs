using System.Diagnostics;
using TRFDControl;
using TRFDControl.FDEntryTypes;
using TRLevelReader.Helpers;
using TRLevelReader.Model;
using TRLevelReader.Model.Enums;

namespace TRLevelFlipper;

public class XFlipper : IFlipper
{
    public FlipType FlipType => FlipType.X;

    private int _worldWidth, _xAdjustment;

    public void Flip(string levelName, TRLevel level)
    {
        CalculateWorldWidth(level);

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    public void Flip(string levelName, TR2Level level)
    {
        CalculateWorldWidth(level);

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    public void Flip(string levelName, TR3Level level)
    {
        CalculateWorldWidth(level);

        MirrorFloorData(level);
        MirrorRooms(level);
        MirrorBoxes(level);

        MirrorStaticMeshes(level);
        MirrorEntities(level);
        MirrorNullMeshes(level);

        MirrorTextures(level);
    }

    private void CalculateWorldWidth(TRLevel level)
    {
        _worldWidth = 0;
        _xAdjustment = 0;
        foreach (TRRoom room in level.Rooms)
        {
            _worldWidth = Math.Max(_worldWidth, room.Info.X + TRConsts.SectorSize * room.NumXSectors);
        }
    }

    private void CalculateWorldWidth(TR2Level level)
    {
        _worldWidth = 0;
        _xAdjustment = 0;
        foreach (TR2Room room in level.Rooms)
        {
            _worldWidth = Math.Max(_worldWidth, room.Info.X + TRConsts.SectorSize * room.NumXSectors);
        }
    }

    private void CalculateWorldWidth(TR3Level level)
    {
        _worldWidth = 0;
        _xAdjustment = 0;
        foreach (TR3Room room in level.Rooms)
        {
            _worldWidth = Math.Max(_worldWidth, room.Info.X + TRConsts.SectorSize * room.NumXSectors);
        }

        TR2Entity puna = Array.Find(level.Entities, e => e.TypeID == (short)TR3Entities.Puna);
        if (puna != null)
        {
            // Rebuild the world around Puna's Lizard
            TR2Entity lizardMan = Array.Find(level.Entities, e => e.Room == puna.Room && e.TypeID == (short)TR3Entities.LizardMan);
            _xAdjustment = lizardMan.X - FlipWorldX(lizardMan.X);
        }
    }

    private int FlipWorldX(int x)
    {
        // Shift the point 100% to the left, then flip it back to +. If we have a level such as Puna
        // that's been built around particular coords, adjust X.
        x -= _worldWidth;
        x *= -1;
        x += _xAdjustment;
        Debug.Assert(x >= 0);
        return x;
    }

    private static void MirrorFloorData(TRLevel level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TRRoom room in level.Rooms)
        {
            List<TRRoomSector> sectors = room.Sectors.ToList();
            MirrorSectors(sectors, room.NumXSectors, room.NumZSectors, floorData);
            room.Sectors = sectors.ToArray();
        }

        floorData.WriteToLevel(level);
    }

    private static void MirrorFloorData(TR2Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TR2Room room in level.Rooms)
        {
            List<TRRoomSector> sectors = room.SectorList.ToList();
            MirrorSectors(sectors, room.NumXSectors, room.NumZSectors, floorData);
            room.SectorList = sectors.ToArray();
        }

        floorData.WriteToLevel(level);
    }

    private static void MirrorFloorData(TR3Level level)
    {
        FDControl floorData = new();
        floorData.ParseFromLevel(level);

        foreach (TR3Room room in level.Rooms)
        {
            List<TRRoomSector> sectors = room.Sectors.ToList();
            MirrorSectors(sectors, room.NumXSectors, room.NumZSectors, floorData);
            room.Sectors = sectors.ToArray();
        }

        floorData.WriteToLevel(level);
    }

    private static void MirrorSectors(List<TRRoomSector> sectors, ushort numXSectors, ushort numZSectors, FDControl floorData)
    {
        // Convert the flattened sector list to 2D            
        List<List<TRRoomSector>> sectorMap = new();
        for (int x = 0; x < numXSectors; x++)
        {
            sectorMap.Add(new List<TRRoomSector>());
            for (int z = 0; z < numZSectors; z++)
            {
                sectorMap[x].Add(sectors[z + x * numZSectors]);
            }
        }

        // We are flipping X, so we just reverse the list of sector lists
        sectorMap.Reverse();
        sectors.Clear();
        foreach (List<TRRoomSector> sectorList in sectorMap)
        {
            sectors.AddRange(sectorList);
        }

        // Change slants and climbable entries
        foreach (TRRoomSector sector in sectors)
        {
            if (sector.FDIndex != 0)
            {
                List<FDEntry> entries = floorData.Entries[sector.FDIndex];
                for (int i = 0; i < entries.Count; i++)
                {
                    FDEntry entry = entries[i];
                    if (entry is FDSlantEntry slantEntry)
                    {
                        // If the X slope is greater than zero, then its value is added to the floor heights of corners 00 and 01.
                        // If it is less than zero, then its value is subtracted from the floor heights of corners 10 and 11.
                        slantEntry.XSlant *= -1;
                    }
                    else if (entry is FDClimbEntry climbEntry)
                    {
                        // We only need to flip the direction if it's exclusively set in +/- X direction.
                        if (climbEntry.IsNegativeX ^ climbEntry.IsPositiveX)
                        {
                            climbEntry.IsNegativeX = !(climbEntry.IsPositiveX ^= true);
                        }
                    }
                    else if (entry is TR3TriangulationEntry triangulation)
                    {
                        // Flip the corners
                        byte c00 = triangulation.TriData.C00;
                        byte c10 = triangulation.TriData.C10;
                        byte c01 = triangulation.TriData.C01;
                        byte c11 = triangulation.TriData.C11;
                        triangulation.TriData.C00 = c10;
                        triangulation.TriData.C10 = c00;
                        triangulation.TriData.C01 = c11;
                        triangulation.TriData.C11 = c01;

                        // And the heights
                        sbyte h1 = triangulation.Setup.H1;
                        sbyte h2 = triangulation.Setup.H2;
                        triangulation.Setup.H1 = h2;
                        triangulation.Setup.H2 = h1;

                        // And the triangulation
                        switch ((FDFunctions)triangulation.Setup.Function)
                        {
                            // Non-portals
                            case FDFunctions.FloorTriangulationNWSE_Solid:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_Solid;
                                break;
                            case FDFunctions.FloorTriangulationNESW_Solid:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_Solid;
                                break;

                            case FDFunctions.CeilingTriangulationNW_Solid:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_Solid;
                                break;
                            case FDFunctions.CeilingTriangulationNE_Solid:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_Solid;
                                break;

                            // Portals: _SW, _NE etc indicate triangles whose right-angles point towards the portal
                            case FDFunctions.FloorTriangulationNWSE_SW:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_NW;
                                break;
                            case FDFunctions.FloorTriangulationNWSE_NE:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNESW_SE;
                                break;
                            case FDFunctions.FloorTriangulationNESW_SE:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_NE;
                                break;
                            case FDFunctions.FloorTriangulationNESW_NW:
                                triangulation.Setup.Function = (byte)FDFunctions.FloorTriangulationNWSE_SW;
                                break;

                            case FDFunctions.CeilingTriangulationNW_SW:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_SE;
                                break;
                            case FDFunctions.CeilingTriangulationNW_NE:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNE_NW;
                                break;
                            case FDFunctions.CeilingTriangulationNE_NW:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_NE;
                                break;
                            case FDFunctions.CeilingTriangulationNE_SE:
                                triangulation.Setup.Function = (byte)FDFunctions.CeilingTriangulationNW_SW;
                                break;
                        }
                    }
                    else if (entry is TR3MinecartRotateLeftEntry)
                    {
                        // If left is followed by right, it means stop the minecart and they appear to
                        // need to remain in this order. Only switch the entry if there is no other.
                        if (!(i < entries.Count - 1 && entries[i + 1] is TR3MinecartRotateRightEntry))
                        {
                            entries[i] = new TR3MinecartRotateRightEntry
                            {
                                Setup = new FDSetup(FDFunctions.MechBeetleOrMinecartRotateRight)
                            };
                        }
                    }
                    else if (entry is TR3MinecartRotateRightEntry)
                    {
                        if (!(i > 0 && entries[i - 1] is TR3MinecartRotateLeftEntry))
                        {
                            entries[i] = new TR3MinecartRotateLeftEntry
                            {
                                Setup = new FDSetup(FDFunctions.DeferredTriggeringOrMinecartRotateLeft)
                            };
                        }
                    }
                }
            }
        }
    }

    private void MirrorRooms(TRLevel level)
    {
        foreach (TRRoom room in level.Rooms)
        {
            int oldRoomX = room.Info.X;
            room.Info.X = FlipWorldX(oldRoomX);
            room.Info.X -= room.NumXSectors * TRConsts.SectorSize;
            Debug.Assert(room.Info.X >= 0);
            // Flip room sprites separately as they don't sit on tile edges
            List<TRRoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TRRoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];

                // Flip the old world coordinate, then subtract the new room position
                int x = oldRoomX + roomVertex.Vertex.X;
                x = FlipWorldX(x);
                x -= room.Info.X;
                roomVertex.Vertex.X = (short)x;

                Debug.Assert(roomVertex.Vertex.X >= 0);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TRRoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                int sectorX = vert.Vertex.X / TRConsts.SectorSize;
                int newSectorX = room.NumXSectors - sectorX;
                vert.Vertex.X = (short)(newSectorX * TRConsts.SectorSize);
                Debug.Assert(vert.Vertex.X >= 0);
            }

            // Change visibility portal vertices and flip the normal for X
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    int sectorX = (int)Math.Round((double)vert.X / TRConsts.SectorSize);
                    int newSectorX = room.NumXSectors - sectorX;
                    vert.X = (short)(newSectorX * TRConsts.SectorSize);
                    Debug.Assert(vert.X >= 0);
                }
                portal.Normal.X *= -1;
            }

            // Move the lights to their new spots
            foreach (TRRoomLight light in room.Lights)
            {
                light.X = FlipWorldX(light.X);
            }

            // Move the static meshes
            foreach (TRRoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.X = (uint)FlipWorldX((int)mesh.X);

                // Convert the angle to short units for consistency and then flip it if +/-X
                int angle = mesh.Rotation + TRConsts.South;
                if (angle == TRConsts.East || angle == TRConsts.West)
                {
                    angle *= -1;
                    angle -= TRConsts.South;
                    mesh.Rotation = (ushort)angle;
                }
            }
        }
    }

    private void MirrorRooms(TR2Level level)
    {
        foreach (TR2Room room in level.Rooms)
        {
            int oldRoomX = room.Info.X;
            room.Info.X = FlipWorldX(oldRoomX);
            room.Info.X -= room.NumXSectors * TRConsts.SectorSize;
            Debug.Assert(room.Info.X >= 0);
            // Flip room sprites separately as they don't sit on tile edges
            List<TR2RoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TR2RoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];

                // Flip the old world coordinate, then subtract the new room position
                int x = oldRoomX + roomVertex.Vertex.X;
                x = FlipWorldX(x);
                x -= room.Info.X;
                roomVertex.Vertex.X = (short)x;

                Debug.Assert(roomVertex.Vertex.X >= 0);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TR2RoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                int sectorX = vert.Vertex.X / TRConsts.SectorSize;
                int newSectorX = room.NumXSectors - sectorX;
                vert.Vertex.X = (short)(newSectorX * TRConsts.SectorSize);
                Debug.Assert(vert.Vertex.X >= 0);
            }

            // Change visibility portal vertices and flip the normal for X
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    int sectorX = (int)Math.Round((double)vert.X / TRConsts.SectorSize);
                    int newSectorX = room.NumXSectors - sectorX;
                    vert.X = (short)(newSectorX * TRConsts.SectorSize);
                    Debug.Assert(vert.X >= 0);
                }
                portal.Normal.X *= -1;
            }

            // Move the lights to their new spots
            foreach (TR2RoomLight light in room.Lights)
            {
                light.X = FlipWorldX(light.X);
            }

            // Move the static meshes
            foreach (TR2RoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.X = (uint)FlipWorldX((int)mesh.X);

                // Convert the angle to short units for consistency and then flip it if +/-X
                int angle = mesh.Rotation + TRConsts.South;
                if (angle == TRConsts.East || angle == TRConsts.West)
                {
                    angle *= -1;
                    angle -= TRConsts.South;
                    mesh.Rotation = (ushort)angle;
                }
            }
        }
    }

    private void MirrorRooms(TR3Level level)
    {
        foreach (TR3Room room in level.Rooms)
        {
            int oldRoomX = room.Info.X;
            room.Info.X = FlipWorldX(oldRoomX);
            room.Info.X -= room.NumXSectors * TRConsts.SectorSize;
            Debug.Assert(room.Info.X >= 0);
            // Flip room sprites separately as they don't sit on tile edges
            List<TR3RoomVertex> processedVerts = new();
            foreach (TRRoomSprite sprite in room.RoomData.Sprites)
            {
                TR3RoomVertex roomVertex = room.RoomData.Vertices[sprite.Vertex];

                // Flip the old world coordinate, then subtract the new room position
                int x = oldRoomX + roomVertex.Vertex.X;
                x = FlipWorldX(x);
                x -= room.Info.X;
                roomVertex.Vertex.X = (short)x;

                Debug.Assert(roomVertex.Vertex.X >= 0);
                processedVerts.Add(roomVertex);
            }

            // Flip the face vertices
            foreach (TR3RoomVertex vert in room.RoomData.Vertices)
            {
                if (processedVerts.Contains(vert))
                {
                    continue;
                }

                int sectorX = vert.Vertex.X / TRConsts.SectorSize;
                int newSectorX = room.NumXSectors - sectorX;
                vert.Vertex.X = (short)(newSectorX * TRConsts.SectorSize);
                Debug.Assert(vert.Vertex.X >= 0);
            }

            // Change visibility portal vertices and flip the normal for X
            foreach (TRRoomPortal portal in room.Portals)
            {
                foreach (TRVertex vert in portal.Vertices)
                {
                    int sectorX = (int)Math.Round((double)vert.X / TRConsts.SectorSize);
                    int newSectorX = room.NumXSectors - sectorX;
                    vert.X = (short)(newSectorX * TRConsts.SectorSize);
                    Debug.Assert(vert.X >= 0);
                }
                portal.Normal.X *= -1;
            }

            // Move the lights to their new spots
            foreach (TR3RoomLight light in room.Lights)
            {
                light.X = FlipWorldX(light.X);
            }

            // Move the static meshes
            foreach (TR3RoomStaticMesh mesh in room.StaticMeshes)
            {
                mesh.X = (uint)FlipWorldX((int)mesh.X);

                // Convert the angle to short units for consistency and then flip it
                short angle = (short)(mesh.Rotation + TRConsts.South);
                angle *= -1;
                angle -= TRConsts.South;
                mesh.Rotation = (ushort)angle;
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
        // TR1 boxes are in world coordinate values
        foreach (TRBox box in boxes)
        {
            uint newMaxX = (uint)FlipWorldX((int)box.XMin);
            uint newMinX = (uint)FlipWorldX((int)box.XMax);
            Debug.Assert(newMaxX >= newMinX);
            box.XMin = newMinX;
            box.XMax = newMaxX;
        }
    }

    private void MirrorBoxes(TR2Box[] boxes)
    {
        // Boxes do not necessarily cover only one sector and several sectors can point
        // to the same box. So we need to work out the smallest new X position for shared
        // boxes and update each one only once. This is done by converting the xmin and xmax
        // to world coordinates, flipping them over X and then TRUtils.Swapping them.
        foreach (TR2Box box in boxes)
        {
            byte newMaxX = (byte)(FlipWorldX(box.XMin * TRConsts.SectorSize) / TRConsts.SectorSize);
            byte newMinX = (byte)(FlipWorldX(box.XMax * TRConsts.SectorSize) / TRConsts.SectorSize);
            Debug.Assert(newMaxX >= newMinX);
            box.XMin = newMinX;
            box.XMax = newMaxX;
        }
    }

    private static void MirrorStaticMeshes(TRLevel level)
    {
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            return TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
        });
    }

    private static void MirrorStaticMeshes(TR2Level level)
    {
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            return TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
        });
    }

    private static void MirrorStaticMeshes(TR3Level level)
    {
        MirrorStaticMeshes(level.StaticMeshes, delegate (TRStaticMesh staticMesh)
        {
            return TRMeshUtilities.GetMesh(level, staticMesh.Mesh);
        });
    }

    private static void MirrorStaticMeshes(TRStaticMesh[] staticMeshes, Func<TRStaticMesh, TRMesh> meshFunc)
    {
        foreach (TRStaticMesh staticMesh in staticMeshes)
        {
            TRMesh mesh = meshFunc(staticMesh);

            foreach (TRVertex vert in mesh.Vertices)
            {
                vert.X *= -1;
            }

            FlipBoundingBox(staticMesh.CollisionBox);
            FlipBoundingBox(staticMesh.VisibilityBox);
        }
    }

    private static void FlipBoundingBox(TRBoundingBox box)
    {
        short min = box.MinX;
        short max = box.MaxX;
        box.MinX = (short)(max * -1);
        box.MaxX = (short)(min * -1);
    }

    private void MirrorEntities(TRLevel level)
    {
        foreach (TREntity entity in level.Entities)
        {
            entity.X = FlipWorldX(entity.X);
            AdjustTR1EntityPosition(entity);
        }

        AdjustDoors(level.Entities.ToList().FindAll(e => TR1EntityUtilities.IsDoorType((TREntities)e.TypeID)));
    }

    private void MirrorEntities(TR2Level level)
    {
        foreach (TR2Entity entity in level.Entities)
        {
            entity.X = FlipWorldX(entity.X);
            AdjustTR2EntityPosition(entity);
        }

        AdjustDoors(level.Entities.ToList().FindAll(e => TR2EntityUtilities.IsDoorType((TR2Entities)e.TypeID)));
    }

    private void MirrorEntities(TR3Level level)
    {
        foreach (TR2Entity entity in level.Entities)
        {
            entity.X = FlipWorldX(entity.X);
            AdjustTR3EntityPosition(entity);
        }

        AdjustDoors(level.Entities.ToList().FindAll(e => TR3EntityUtilities.IsDoorType((TR3Entities)e.TypeID)));
    }

    private static void AdjustTR1EntityPosition(TREntity entity)
    {
        entity.Angle *= -1;

        switch ((TREntities)entity.TypeID)
        {
            case TREntities.Animating1:
            case TREntities.Animating2:
            case TREntities.Animating3:
            case TREntities.AtlanteanEgg:
                switch (entity.Angle)
                {
                    case TRConsts.East:
                        entity.Z -= TRConsts.SectorSize;
                        break;
                    case TRConsts.West:
                        entity.Z += TRConsts.SectorSize;
                        break;
                    case TRConsts.North:
                        entity.X += TRConsts.SectorSize;
                        break;
                    case TRConsts.South:
                        entity.X -= TRConsts.SectorSize;
                        break;
                }
                break;
            case TREntities.AdamEgg:
                switch (entity.Angle)
                {
                    case TRConsts.East:
                        entity.Z -= TRConsts.SectorSize * 2;
                        break;
                    case TRConsts.West:
                        entity.Z += TRConsts.SectorSize * 2;
                        break;
                    case TRConsts.North:
                        entity.X += TRConsts.SectorSize * 2;
                        break;
                    case TRConsts.South:
                        entity.X -= TRConsts.SectorSize * 2;
                        break;
                }
                break;
            case TREntities.BridgeTilt1:
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
                break;
        }
    }

    private static void AdjustTR2EntityPosition(TR2Entity entity)
    {
        // If it's facing +/-X direction, flip it
        if (entity.Angle == TRConsts.East || entity.Angle == TRConsts.West)
        {
            entity.Angle *= -1;
        }

        switch ((TR2Entities)entity.TypeID)
        {
            // These take up 2 tiles so need some fiddling
            case TR2Entities.Elevator:
            case TR2Entities.SpikyCeiling:
            case TR2Entities.SpikyWall:
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.X += TRConsts.SectorSize;
                        break;
                    case TRConsts.West:
                        entity.Z -= TRConsts.SectorSize;
                        break;
                    case TRConsts.North:
                        entity.X -= TRConsts.SectorSize;
                        break;
                    case TRConsts.East:
                        entity.Z += TRConsts.SectorSize;
                        break;
                }
                break;
            case TR2Entities.Gong: // case 0 applicable to IceCave
                switch (entity.Angle)
                {
                    case TRConsts.South:
                        entity.X -= TRConsts.SectorSize;
                        break;
                    case TRConsts.West:
                        entity.Z += TRConsts.SectorSize;
                        break;
                    case TRConsts.North:
                        entity.X += TRConsts.SectorSize;
                        break;
                    case TRConsts.East:
                        entity.Z -= TRConsts.SectorSize;
                        break;
                }
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

            // Bridge tilts need to be rotated
            case TR2Entities.BridgeTilt1:
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
                break;

            case TR2Entities.AirplanePropeller:
                if (entity.Angle == TRConsts.West)
                {
                    entity.Angle = TRConsts.East;
                }
                break;

            case TR2Entities.OverheadPulleyHook:
                if (entity.Angle == TRConsts.South || entity.Angle == TRConsts.North)
                {
                    entity.Angle += TRConsts.South;
                }
                break;

            case TR2Entities.PowerSaw:
                if (entity.Angle == TRConsts.North)
                {
                    entity.X += TRConsts.SectorSize;
                }
                break;

            case TR2Entities.Helicopter:
                if (entity.Angle == TRConsts.West)
                {
                    entity.Angle = TRConsts.North;
                    entity.X += TRConsts.SectorSize;
                    entity.Z += TRConsts.SectorSize;
                }
                break;

            case TR2Entities.MarcoBartoli:
                // InitialiseBartoli in Dragon.c always shifts Bartoli as follows,
                // so we need to move him 512 in the +X to avoid him ending up either
                // OOB or in mid-air.
                // item->pos.x_pos -= STEP_L*2;
                entity.X += TRConsts.SectorSize / 2;
                break;
        }
    }

    private static void AdjustTR3EntityPosition(TR2Entity entity)
    {
        // Flip the angle - north and south remain, everything else moves appropriately
        entity.Angle *= -1;

        switch ((TR3Entities)entity.TypeID)
        {
            // These take up several tiles so need some fiddling
            case TR3Entities.SpikyVertWallOrTunnelBorer:
            case TR3Entities.SpikyWall:
            case TR3Entities.SubwayTrain:
                switch (entity.Angle)
                {
                    case TRConsts.North:
                        entity.X -= TRConsts.SectorSize;
                        break;
                    case TRConsts.East:
                        entity.Z += TRConsts.SectorSize;
                        break;
                    case TRConsts.South:
                        entity.X += TRConsts.SectorSize;
                        break;
                    case TRConsts.West:
                        entity.Z -= TRConsts.SectorSize;
                        break;
                }
                break;

            case TR3Entities.Area51Swinger:
                switch (entity.Angle)
                {
                    case TRConsts.North:
                        entity.X += TRConsts.SectorSize;
                        break;
                    case TRConsts.West:
                        entity.Z += TRConsts.SectorSize;
                        break;
                }
                break;
            case TR3Entities.BigMissile:
            case TR3Entities.MovableBoom:
                switch (entity.Angle)
                {
                    case TRConsts.East:
                        entity.Z -= TRConsts.SectorSize;
                        break;
                }
                break;

            // Bridge tilts need to be rotated
            case TR3Entities.BridgeTilt1:
            case TR3Entities.BridgeTilt2:
            case TR3Entities.FireBreathingDragonStatue:
                switch (entity.Angle)
                {
                    case TRConsts.North:
                        entity.Angle = TRConsts.South;
                        break;
                    case TRConsts.East:
                        entity.Angle = TRConsts.West;
                        break;
                    case TRConsts.South:
                        entity.Angle = TRConsts.North;
                        break;
                    case TRConsts.West:
                        entity.Angle = TRConsts.East;
                        break;
                }
                break;

            // The Crash Site walls
            case TR3Entities.DestroyableBoardedUpWall:
                switch (entity.Angle)
                {
                    case TRConsts.East:
                        entity.Z -= 3 * TRConsts.SectorSize;
                        break;
                    case TRConsts.South:
                        entity.X -= 3 * TRConsts.SectorSize;
                        break;
                }
                break;
        }
    }

    private static void AdjustDoors(List<TREntity> doors)
    {
        // Double doors need to be TRUtils.Swapped otherwise they open in the wrong direction.
        // Iterate backwards and try to find doors that are next to each other.
        // If found, TRUtils.Swap their types.
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            TREntity door1 = doors[i];
            for (int j = doors.Count - 1; j >= 0; j--)
            {
                if (j == i)
                {
                    continue;
                }

                TREntity door2 = doors[j];

                if (TRUtils.AreDoubleDoors(door1, door2))
                {
                    (door2.TypeID, door1.TypeID) = (door1.TypeID, door2.TypeID);

                    // Don't process these doors again, so just remove the first
                    doors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static void AdjustDoors(List<TR2Entity> doors)
    {
        // Double doors need to be TRUtils.Swapped otherwise they open in the wrong direction.
        // Iterate backwards and try to find doors that are next to each other.
        // If found, TRUtils.Swap their types.
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            TR2Entity door1 = doors[i];
            for (int j = doors.Count - 1; j >= 0; j--)
            {
                if (j == i)
                {
                    continue;
                }

                TR2Entity door2 = doors[j];

                if (TRUtils.AreDoubleDoors(door1, door2))
                {
                    (door2.TypeID, door1.TypeID) = (door1.TypeID, door2.TypeID);

                    // Don't process these doors again, so just remove the first
                    doors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void MirrorNullMeshes(TRLevel level)
    {
        // The deals with actual cameras as well as sinks
        foreach (TRCamera camera in level.Cameras)
        {
            camera.X = FlipWorldX(camera.X);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.X = FlipWorldX(sound.X);
        }

        // TODO: Handle TRCinematicFrames by working out how to mirror animation
        // frames e.g. the LaraMiscAnim that corresponds with the cinematics.
        // Currently, frames are left untouched so the Rig starting animation
        // and dragon dagger cutscene behave normally. Lara is a bit out of
        // place in the HSH cinematics for now.
    }

    private void MirrorNullMeshes(TR2Level level)
    {
        // The deals with actual cameras as well as sinks
        foreach (TRCamera camera in level.Cameras)
        {
            camera.X = FlipWorldX(camera.X);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.X = FlipWorldX(sound.X);
        }

        // TODO: Handle TRCinematicFrames by working out how to mirror animation
        // frames e.g. the LaraMiscAnim that corresponds with the cinematics.
        // Currently, frames are left untouched so the Rig starting animation
        // and dragon dagger cutscene behave normally. Lara is a bit out of
        // place in the HSH cinematics for now.
    }

    private void MirrorNullMeshes(TR3Level level)
    {
        // The deals with actual cameras as well as sinks
        foreach (TRCamera camera in level.Cameras)
        {
            camera.X = FlipWorldX(camera.X);
        }

        foreach (TRSoundSource sound in level.SoundSources)
        {
            sound.X = FlipWorldX(sound.X);
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
