namespace FileSynchronizer;

#pragma warning disable
public class FileContents {
    public Folder Parent { get; set; }
    public bool IsDataFetched { get; set; }
    public byte[] AsBytes;
    public string[] AsLines;
    public string AsFullText;

    public string FileName;

    public FileContents(Folder parent, string fileName, bool fetchData = true) {
        FileName = fileName;
        Parent = parent;

        if (fetchData)
            FetchData();
    }
    public FileContents() { }

    public void FetchData() {
        AsBytes = File.ReadAllBytes(Parent.FolderPath + "/" + FileName);
        AsLines = File.ReadAllLines(Parent.FolderPath + "/" + FileName);
        AsFullText = File.ReadAllText(Parent.FolderPath + "/" + FileName);
        IsDataFetched = true;
    }
    public override string ToString() => $"{FileName}";
}
