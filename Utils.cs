using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronizer;

#pragma warning disable
public static class Utils {

    public static string[] AllSpecialFolders = Enum.GetNames<Environment.SpecialFolder>();
    public static void Serialize(this NetDataWriter writer, Folder folder, bool isSubFolderSquared = false) {
        // var temp = folder.DeepCopy();
        writer.Put(folder.FolderPath);
        // don't bother writing "SubdirsScanned" since we aren't doing that part here.

        int numFiles = folder.Files.Length;
        writer.Put(numFiles);

        // put all files
        for (int i = 0; i < numFiles; i++) {
            writer.Serialize(folder.Files[i]);
        }

        var numSubs = folder.SubFolders.Length;
        writer.Put(numSubs);
        for (int i = 0; i < numSubs; i++) {
            writer.Put(isSubFolderSquared);

            // if we are currently serializing a subfolder, don't serialize a sub-subfolder
            if (isSubFolderSquared)
                writer.Put(folder.SubFolders[i].FolderPath);
            else
                writer.Serialize(folder.SubFolders[i]);
        }
    }
    public static Folder DeserializeFolder(this NetDataReader reader) {
        var folderPath = reader.GetString();
        var folder = new Folder() {
            FolderPath = folderPath,
        };

        var numFiles = reader.GetInt();

        folder.Files = new FileContents[numFiles];
        for (int i = 0; i < numFiles; i++) {
            folder.Files[i] = reader.DeserializeFile();
        }

        var numSubs = reader.GetInt();

        folder.SubFolders = new Folder[numSubs];
        for (int i = 0; i < numSubs; i++) {
            var isSubFolderSquared = reader.GetBool();

            if (isSubFolderSquared) {
                folder.SubFolders[i] = new() {
                    FolderPath = reader.GetString(),
                };
            }
            else {
                folder.SubFolders[i] = reader.DeserializeFolder();
            }
        }

        return folder;
    }
    public static void Serialize(this NetDataWriter writer, FileContents file) {
        writer.Put(file.Parent.FolderPath);
        writer.Put(file.FileName);
        writer.Put(file.Size);
    }
    public static FileContents DeserializeFile(this NetDataReader reader) {
        // empty constructor so we don't do file stuff on the computer recieving the data, since those directories don't exist
        // on the receiving end (yet..)
        var file = new FileContents();

        // we dont load any data directly since that would take ages. we want to manually request that
        file.Parent = new Folder() {
            FolderPath = reader.GetString(),
        };
        file.FileName = reader.GetString();
        file.Size = reader.GetLong();
        //file.Info = new(file.Parent.FolderPath + "/" + file.FileName);

        return file;
    } 

    // serialize string array
    public static void Put(this NetDataWriter writer, string[] lines) {
        int numLines = lines.Length;
        writer.Put(numLines);
        for (int i = 0; i < numLines; i++)
            writer.Put(lines[i]);
    }
    public static string[] GetLines(this NetDataReader reader) {
        int numLines = reader.GetInt();
        string[] lines = new string[numLines];
        for (int i = 0; i < numLines; i++)
            lines[i] = reader.GetString();
        return lines;
    }

    public static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    public static string SizeSuffix(long value, int decimalPlaces = 1) {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000) {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + (mag > 0 ? decimalPlaces : 0) + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
}
