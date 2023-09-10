using TRLevelReader;
using TRLevelReader.Helpers;
using TRLevelReader.Model;

namespace TRLevelFlipper;

class Program
{
    const uint TR1 = 32u;
    const uint TR2 = 45u;
    const uint TR3a = 4278714424u;
    const uint TR3b = 4279763000u;

    static void Main(string[] args)
    {
        if (args.Length < 2 || args[0].Contains('?'))
        {
            Usage();
            return;
        }

        IFlipper flipper;
        string flipType = args[1];
        switch (flipType.ToUpper())
        {
            case "X":
                flipper = new XFlipper();
                break;
            case "Y":
                flipper = new YFlipper();
                break;
            case "Z":
                flipper = new ZFlipper();
                break;
            default:
                Console.WriteLine("Unrecognised flip type: {0}", flipType);
                return;
        }

        PostFlipOptions opts = new();
        for (int i = 2; i < args.Length; i++)
        {
            string arg = args[i].ToUpper();
            if (arg.EndsWith("DRAIN"))
                opts.Drain = true;
            if (arg.EndsWith("DOORS"))
                opts.RemoveDoors = true;
            if (arg.EndsWith("PUSHBLOCKS"))
                opts.RemovePushblocks = true;
            if (arg.EndsWith("TILES"))
                opts.RemoveTiles = true;
            if (arg.EndsWith("EXTRAS") && i < args.Length - 1)
            {
                string[] types = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                opts.ExtraRemovals = types.Select(a => int.Parse(a)).ToList();
            }
        }

        uint version = DetectVersion(args[0]);
        switch (version)
        {
            case TR1:
                FlipTR1(args[0], flipper, opts);
                break;
            case TR2:
                FlipTR2(args[0], flipper, opts);
                break;
            case TR3a:
            case TR3b:
                FlipTR3(args[0], flipper, opts);
                break;
            default:
                Console.WriteLine("Unhandled level version: {0}", version);
                break;
        }
    }

    static uint DetectVersion(string path)
    {
        switch (path.ToUpper())
        {
            case "TR1":
                return TR1;
            case "TR2":
                return TR2;
            case "TR3":
                return TR3a;
            default:
                {
                    using BinaryReader reader = new(File.Open(path, FileMode.Open));
                    return reader.ReadUInt32();
                }
        }
    }

    static void FlipTR1(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR1LevelReader reader = new();
        TR1LevelWriter writer = new();

        List<string> levels = new();
        if (levelFile.ToLower().Equals("tr1"))
        {
            levels.AddRange(TRLevelNames.AsOrderedList);
        }
        else
        {
            levels.Add(levelFile);
        }

        foreach (string lvl in levels)
        {
            if (!File.Exists(lvl))
                continue;

            Console.WriteLine($"Flipping {Path.GetFileName(lvl)}");
            TRLevel level = reader.ReadLevel(lvl);

            flipper.Flip(Path.GetFileName(lvl).ToUpper(), level);
            options.Apply(level);

            Write(lvl, flipper, path => writer.WriteLevelToFile(level, path));
        }
    }

    static void FlipTR2(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR2LevelReader reader = new();
        TR2LevelWriter writer = new();

        List<string> levels = new();
        if (levelFile.ToLower().Equals("tr2"))
        {
            levels.AddRange(TR2LevelNames.AsOrderedList);
        }
        else
        {
            levels.Add(levelFile);
        }

        foreach (string lvl in levels)
        {
            if (!File.Exists(lvl))
                continue;

            Console.WriteLine($"Flipping {Path.GetFileName(lvl)}");
            TR2Level level = reader.ReadLevel(lvl);

            flipper.Flip(Path.GetFileName(lvl).ToUpper(), level);
            options.Apply(level);

            Write(lvl, flipper, path => writer.WriteLevelToFile(level, path));
        }
    }

    static void FlipTR3(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR3LevelReader reader = new();
        TR3LevelWriter writer = new();

        List<string> levels = new();
        if (levelFile.ToLower().Equals("tr3"))
        {
            levels.AddRange(TR3LevelNames.AsOrderedList);
        }
        else
        {
            levels.Add(levelFile);
        }

        foreach (string lvl in levels)
        {
            if (!File.Exists(lvl))
                continue;

            Console.WriteLine($"Flipping {Path.GetFileName(lvl)}");
            TR3Level level = reader.ReadLevel(lvl);

            flipper.Flip(Path.GetFileName(lvl).ToUpper(), level);
            options.Apply(level);

            Write(lvl, flipper, path => writer.WriteLevelToFile(level, path));
        }
    }

    static void Write(string levelFile, IFlipper flipper, Action<string> writeAction)
    {
        string saveDir = flipper.FlipType + "Flipped";
        Directory.CreateDirectory(saveDir);
        writeAction.Invoke(Path.Combine(saveDir, levelFile));
    }

    static void Usage()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Usage: TRLevelFlipper LEVEL.PHD|LEVEL.TR2|TR1|TR2|TR3 X|Y|Z [ -drain | -doors | -pushblocks | -tiles | -extras 1,2,3 ]");
        Console.WriteLine("-drain : empty water from all rooms");
        Console.WriteLine("-doors : remove all doors and pushblocks");
        Console.WriteLine("-pushblocks : remove all pushblocks");
        Console.WriteLine("-tiles : remove all breakable tiles");
        Console.WriteLine("-extras : comma-separated type ID list - extra item types to be removed");
        Console.WriteLine();
        Console.WriteLine("Examples");
        Console.WriteLine("   Flip Caves about the X axis");
        Console.WriteLine("   TRLevelFlipper LEVEL1.PHD X");
        Console.WriteLine();
        Console.WriteLine("   Flip Great Wall about the Z axis and open all doors");
        Console.WriteLine("   TRLevelFlipper WALL.TR2 Z -doors");
        Console.WriteLine();
        Console.WriteLine("   Flip every TR2 level about the Y axis and open all doors");
        Console.WriteLine("   TRLevelFlipper TR2 Y -doors");
        Console.WriteLine();
        Console.WriteLine("   Flip Jungle about the Y axis, drain all rooms and open all doors");
        Console.WriteLine("   TRLevelFlipper JUNGLE.TR2 Y -drain -doors");
        Console.WriteLine();
        Console.ResetColor();
        Console.WriteLine("Place level files in this directory.");
        Console.WriteLine("Flipped levels will be saved to a sub-directory named XFlipped, YFLipped or ZFlipped.");
        Console.WriteLine();
    }
}
