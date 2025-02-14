using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSynchronizer;

#pragma warning disable CS8618
public class Folder {
    public string FolderPath { get; set; }
    public FileContents[] Files { get; set; }
    public Folder[] SubFolders { get; set; }
    public Folder(string path) {
        FolderPath = path;
        SubFolders = [];

        var files = Directory.GetFiles(path);
        Files = new FileContents[files.Length];
        for (int i = 0; i < files.Length; i++)
            Files[i] = new FileContents(this, Path.GetFileName(files[i]), false);

        // happens recursively
        var folders = Directory.GetDirectories(path);
        SubFolders = new Folder[folders.Length];
        for (int i = 0; i < folders.Length; i++) {
            SubFolders[i] = new Folder(folders[i]);
        }
    }
    public Folder() { }
    public FileContents[] GetAllFilesOfType(string fileExt) => Files.Where(x => x.FileName.EndsWith(fileExt)).ToArray();
    public Folder DeepCopy() => new(FolderPath);
    public void FetchAllFileContents() {
        foreach (var item in Files)
            item.FetchData();
    }
    public override string ToString() {
        StringBuilder s = new();
        s.Append(FolderPath + ", ");
        s.Append(Files.Length + " file(s), ");
        if (SubFolders.Length == 0)
            s.Append("No Folders");
        else if (SubFolders.Length > 0)
            s.Append($"{SubFolders.Length} folder(s)");
        return s.ToString();
    } 
}
