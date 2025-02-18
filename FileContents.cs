namespace FileSynchronizer;

#pragma warning disable
public class FileContents {
    public Folder Parent { get; set; }
    public bool IsDataFetched { get; set; }
    public byte[] AsBytes;
    public string[] AsLines;
    public string AsFullText;
    public FileInfo Info { get; set; }

    public long Size;

    public string FilePath;
    public string FileName;

    public FileContents(Folder parent, string filePath, bool fetchData = true) {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Parent = parent;
        Info = new FileInfo(Parent.FolderPath + "/" + FileName);
        Size = Info.Length;

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
    public override string ToString() => $"{FileName}, {Utils.SizeSuffix(Info.Length, 2)}";
}
