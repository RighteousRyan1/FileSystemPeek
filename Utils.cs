using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronizer;

#pragma warning disable
public static class Utils {
    // this is a real crap shoot lmfao
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
        writer.Put(file.FileName);

        // in reality, we should request these when they're wanted
        //writer.Put(file.AsBytes.Length);
        //writer.Put(file.AsBytes);
        //writer.Put(file.AsLines);
        //writer.Put(file.AsFullText);
    }
    public static FileContents DeserializeFile(this NetDataReader reader) {
        // empty constructor so we don't do file stuff on the computer recieving the data, since those directories don't exist
        // on the receiving end (yet..)
        var file = new FileContents();

        // we dont load any data directly since that would take ages. we want to manually request that
        file.FileName = reader.GetString();
        //var byteCount = reader.GetInt();
        //reader.GetBytes(file.AsBytes, byteCount);
        //file.AsLines = reader.GetLines();
        //file.AsFullText = reader.GetString();

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
}
