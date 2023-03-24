using TRLevelReader;
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
                opts.OpenDoors = true;
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
        using BinaryReader reader = new(File.Open(path, FileMode.Open));
        return reader.ReadUInt32();
    }

    static void FlipTR1(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR1LevelReader reader = new();
        TR1LevelWriter writer = new();

        TRLevel level = reader.ReadLevel(levelFile);

        flipper.Flip(Path.GetFileName(levelFile).ToUpper(), level);
        options.Apply(level);

        Write(levelFile, flipper, path => writer.WriteLevelToFile(level, path));
    }

    static void FlipTR2(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR2LevelReader reader = new();
        TR2LevelWriter writer = new();

        TR2Level level = reader.ReadLevel(levelFile);

        flipper.Flip(Path.GetFileName(levelFile).ToUpper(), level);
        options.Apply(level);

        Write(levelFile, flipper, path => writer.WriteLevelToFile(level, path));
    }

    static void FlipTR3(string levelFile, IFlipper flipper, PostFlipOptions options)
    {
        TR3LevelReader reader = new();
        TR3LevelWriter writer = new();

        TR3Level level = reader.ReadLevel(levelFile);

        flipper.Flip(Path.GetFileName(levelFile).ToUpper(), level);
        options.Apply(level);

        Write(levelFile, flipper, path => writer.WriteLevelToFile(level, path));
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
        Console.WriteLine("Usage: TRLevelFlipper LEVEL.PHD|LEVEL.TR2 X|Y|Z [ -drain | -doors ]");
        Console.WriteLine("-drain : empty water from all rooms");
        Console.WriteLine("-doors : open all doors");
        Console.WriteLine();
        Console.WriteLine("Examples");
        Console.WriteLine("   Flip Caves about the X axis");
        Console.WriteLine("   TRLevelFlipper LEVEL1.PHD X");
        Console.WriteLine();
        Console.WriteLine("   Flip Great Wall about the Z axis and open all doors");
        Console.WriteLine("   TRLevelFlipper WALL.TR2 Z -doors");
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
