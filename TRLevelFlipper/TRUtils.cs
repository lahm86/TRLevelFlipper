using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TRFDControl.FDEntryTypes;
using TRFDControl;
using TRLevelReader.Model;

namespace TRLevelFlipper;

public static class TRUtils
{
    public static bool IsTriangle(TRObjectTexture texture)
    {
        foreach (TRObjectTextureVert vertex in texture.Vertices)
        {
            if (vertex.XCoordinate.Fraction == 0
                && vertex.XCoordinate.Whole == 0
                && vertex.YCoordinate.Fraction == 0
                && vertex.YCoordinate.Whole == 0)
            {
                return true;
            }
        }

        return false;
    }

    public static void Swap<T>(T[] arr, int pos1, int pos2)
    {
        (arr[pos2], arr[pos1]) = (arr[pos1], arr[pos2]);
    }

    public static bool AreDoubleDoors(TREntity door1, TREntity door2)
    {
        // If the difference between X or Z position is one sector size, they share the same Y val,
        // and they are facing the same diretion, then they're double doors.
        return door1.Room == door2.Room &&
            door1.TypeID != door2.TypeID &&
            door1.Y == door2.Y &&
            door1.Angle == door2.Angle &&
            (Math.Abs(door1.X - door2.X) == TRConsts.SectorSize || Math.Abs(door1.Z - door2.Z) == TRConsts.SectorSize);
    }

    public static bool AreDoubleDoors(TR2Entity door1, TR2Entity door2)
    {
        // If the difference between X or Z position is one sector size, they share the same Y val,
        // and they are facing the same diretion, then they're double doors.
        return door1.Room == door2.Room &&
            door1.TypeID != door2.TypeID &&
            door1.Y == door2.Y &&
            door1.Angle == door2.Angle &&
            (Math.Abs(door1.X - door2.X) == TRConsts.SectorSize || Math.Abs(door1.Z - door2.Z) == TRConsts.SectorSize);
    }

    public static Vector4? GetBestSlantMidpoint(FDSlantEntry slant)
    {
        List<sbyte> corners = new() { 0, 0, 0, 0 };
        if (slant.XSlant > 0)
        {
            corners[0] += slant.XSlant;
            corners[1] += slant.XSlant;
        }
        else if (slant.XSlant < 0)
        {
            corners[2] -= slant.XSlant;
            corners[3] -= slant.XSlant;
        }

        if (slant.ZSlant > 0)
        {
            corners[0] += slant.ZSlant;
            corners[2] += slant.ZSlant;
        }
        else if (slant.ZSlant < 0)
        {
            corners[1] -= slant.ZSlant;
            corners[3] -= slant.ZSlant;
        }

        int uniqueHeights = corners.ToHashSet().Count;
        int dy = corners.Max() - corners.Min();
        int angle = -1;

        if (uniqueHeights == 2)
        {
            if (corners[0] == corners[1])
            {
                angle = corners[0] < corners[2] ? 16384 : -16384;
            }
            else
            {
                angle = corners[2] < corners[3] ? 0 : -32768;
            }
        }

        dy = dy * TRConsts.ClickSize / 2; // Half-way down the slope
        return new Vector4(TRConsts.SectorSize / 2, dy, TRConsts.SectorSize / 2, angle);
    }

    public static Vector4? GetBestTriangleMidpoint(TRRoomSector sector, TR3TriangulationEntry triangulation, int sectorIndex, int roomDepth, int roomYTop)
    {
        int t0 = triangulation.TriData.C10;
        int t1 = triangulation.TriData.C00;
        int t2 = triangulation.TriData.C01;
        int t3 = triangulation.TriData.C11;

        List<byte> triangleCorners = new()
        {
            triangulation.TriData.C00,
            triangulation.TriData.C01,
            triangulation.TriData.C10,
            triangulation.TriData.C11
        };

        int max = triangleCorners.Max();
        List<int> corners = new()
        {
            max - triangleCorners[0],
            max - triangleCorners[1],
            max - triangleCorners[2],
            max - triangleCorners[3]
        };

        List<Vector3> triangle1, triangle2;

        int x1, x2, dx1, dx2;
        int z1, z2, dz1, dz2;
        int xoff1, zoff1, xoff2, zoff2;
        int angle = -1;
        Vector3 triSum1, triSum2;
        Vector4? bestMatch = null;

        int sectorXPos = sectorIndex / roomDepth * TRConsts.SectorSize;
        int sectorZPos = sectorIndex % roomDepth * TRConsts.SectorSize;

        FDFunctions func = (FDFunctions)triangulation.Setup.Function;
        switch (func)
        {
            case FDFunctions.FloorTriangulationNWSE_Solid:
            case FDFunctions.FloorTriangulationNWSE_SW:
            case FDFunctions.FloorTriangulationNWSE_NE:
                triangle1 = new List<Vector3>
                {
                    new Vector3(0, corners[0], 0),
                    new Vector3(0, corners[1], 4),
                    new Vector3(4, corners[2], 0)
                };
                triangle2 = new List<Vector3>
                {
                    new Vector3(0, corners[1], 4),
                    new Vector3(4, corners[2], 0),
                    new Vector3(4, corners[3], 4)
                };

                triSum1 = (triangle1[0] + triangle1[1] + triangle1[2]) / 3;
                triSum2 = (triangle2[0] + triangle2[1] + triangle2[2]) / 3;

                x1 = (int)(sectorXPos + triSum1.X * TRConsts.ClickSize);
                z1 = (int)(sectorZPos + triSum1.Z * TRConsts.ClickSize);

                // Which quarter of the tile are we in?
                dx1 = x1 & (TRConsts.SectorSize - 1);
                dz1 = z1 & (TRConsts.SectorSize - 1);

                // Is this the top triangle?
                if (dx1 <= (TRConsts.SectorSize - dz1))
                {
                    xoff1 = t2 - t1;
                    zoff1 = t0 - t1;
                }
                else
                {
                    xoff1 = t3 - t0;
                    zoff1 = t3 - t2;
                }

                x2 = (int)(sectorXPos + triSum2.X * TRConsts.ClickSize);
                z2 = (int)(sectorZPos + triSum2.Z * TRConsts.ClickSize);
                dx2 = x2 & (TRConsts.SectorSize - 1);
                dz2 = z2 & (TRConsts.SectorSize - 1);

                if (dx2 <= (TRConsts.SectorSize - dz2))
                {
                    xoff2 = t2 - t1;
                    zoff2 = t0 - t1;
                }
                else
                {
                    xoff2 = t3 - t0;
                    zoff2 = t3 - t2;
                }

                // Eliminate hidden flat triangles on shore lines, otherwise the location will be OOB.
                // See geometry in room 44 in Coastal Village for example.
                if (sector.Floor * TRConsts.ClickSize == roomYTop)
                {
                    if (xoff1 == 0 && zoff1 == 0)
                    {
                        xoff1 = zoff1 = int.MaxValue;
                    }
                    if (xoff2 == 0 && zoff2 == 0)
                    {
                        xoff2 = zoff2 = int.MaxValue;
                    }
                }

                // Pick a suitable angle for the incline
                if (Math.Abs(zoff1) < Math.Abs(zoff2))
                {
                    angle = -24576;
                }
                else if (Math.Abs(zoff1) > Math.Abs(zoff2))
                {
                    angle = 8192;
                }

                // Work out which triangle has the smallest gradient. We can only include it if
                // the triangle is not a collisional portal.
                if (Math.Abs(xoff1) < Math.Abs(xoff2) && Math.Abs(zoff1) < Math.Abs(zoff2) && func != FDFunctions.FloorTriangulationNWSE_SW)
                {
                    bestMatch = new Vector4(triSum1.X, triSum1.Y, triSum1.Z, angle);
                }
                else if (func != FDFunctions.FloorTriangulationNWSE_NE)
                {
                    bestMatch = new Vector4(triSum2.X, triSum2.Y, triSum2.Z, angle);
                }

                break;

            case FDFunctions.FloorTriangulationNESW_Solid:
            case FDFunctions.FloorTriangulationNESW_SE:
            case FDFunctions.FloorTriangulationNESW_NW:
                triangle1 = new List<Vector3>
                {
                    new Vector3(0, corners[0], 0),
                    new Vector3(4, corners[2], 0),
                    new Vector3(4, corners[3], 4)
                };
                triangle2 = new List<Vector3>
                {
                    new Vector3(0, corners[0], 0),
                    new Vector3(0, corners[1], 4),
                    new Vector3(4, corners[3], 4)
                };

                triSum1 = (triangle1[0] + triangle1[1] + triangle1[2]) / 3;
                triSum2 = (triangle2[0] + triangle2[1] + triangle2[2]) / 3;

                x1 = (int)(sectorXPos + triSum1.X * TRConsts.ClickSize);
                z1 = (int)(sectorZPos + triSum1.Z * TRConsts.ClickSize);
                dx1 = x1 & (TRConsts.SectorSize - 1);
                dz1 = z1 & (TRConsts.SectorSize - 1);

                if (dx1 <= dz1)
                {
                    xoff1 = t2 - t1;
                    zoff1 = t3 - t2;
                }
                else
                {
                    xoff1 = t3 - t0;
                    zoff1 = t0 - t1;
                }

                x2 = (int)(sectorXPos + triSum2.X * TRConsts.ClickSize);
                z2 = (int)(sectorZPos + triSum2.Z * TRConsts.ClickSize);
                dx2 = x2 & (TRConsts.SectorSize - 1);
                dz2 = z2 & (TRConsts.SectorSize - 1);

                if (dx2 <= dz2)
                {
                    xoff2 = t2 - t1;
                    zoff2 = t3 - t2;
                }
                else
                {
                    xoff2 = t3 - t0;
                    zoff2 = t0 - t1;
                }

                if (sector.Floor * TRConsts.ClickSize == roomYTop)
                {
                    if (xoff1 == 0 && zoff1 == 0)
                    {
                        xoff1 = zoff1 = int.MaxValue;
                    }
                    if (xoff2 == 0 && zoff2 == 0)
                    {
                        xoff2 = zoff2 = int.MaxValue;
                    }
                }

                if (Math.Abs(xoff1) < Math.Abs(xoff2))
                {
                    angle = 24576;
                }
                else if (Math.Abs(xoff1) > Math.Abs(xoff2))
                {
                    angle = -8192;
                }

                if (Math.Abs(xoff1) < Math.Abs(xoff2) && Math.Abs(zoff1) < Math.Abs(zoff2) && func != FDFunctions.FloorTriangulationNESW_NW)
                {
                    bestMatch = new Vector4(triSum1.X, triSum1.Y, triSum1.Z, angle);
                }
                else if (func != FDFunctions.FloorTriangulationNESW_SE)
                {
                    bestMatch = new Vector4(triSum2.X, triSum2.Y, triSum2.Z, angle);
                }

                break;
        }

        return bestMatch;
    }
}
