using CommonLib.Pooling.Pools;

using System;
using System.IO;
using System.Linq;

namespace CommonLib.IO
{
    public class Directory
    {
        private Directory(DirectoryInfo directoryInfo)
            => Info = directoryInfo;

        public Directory(string path) : this(new DirectoryInfo(path)) { }

        public DirectoryInfo Info { get; private set; }

        public Directory Parent => new Directory(Info.Parent);
        public Directory Root => new Directory(Info.Root);

        public bool Exists => Info.Exists;

        public DateTime CreatedAt { get => Info.CreationTime; set => Info.CreationTime = value; }
        public DateTime AccessedAt { get => Info.LastAccessTime; set => Info.LastAccessTime = value; }
        public DateTime WrittenAt { get => Info.LastWriteTime; set => Info.LastWriteTime = value; }

        public FileAttributes Attributes { get => Info.Attributes; set => Info.Attributes = value; }

        public string Name
        {
            get => Info.Name;
            set
            {
                var dir = System.IO.Path.GetDirectoryName(Path);
                var newPath = $"{dir}/{value}";

                Path = newPath;
            }
        }

        public string Path
        {
            get => Info.FullName;
            set
            {
                System.IO.Directory.Delete(value, true);
                System.IO.Directory.Move(Path, value);

                Info = new DirectoryInfo(value);
            }
        }

        public Directory CreateDirectory(string name)
            => new Directory($"{Info.FullName}/{name}").CheckExistance();

        public Directory[] GetSubdirectories()
            => Info.GetDirectories().Select(d => new Directory(d)).ToArray();

        public File[] GetFiles(string pattern = "*")
        {
            var list = ListPool<File>.Shared.Rent();

            foreach (var filePath in System.IO.Directory.GetFiles(Info.FullName, pattern))
                list.Add(new File(filePath));

            return ListPool<File>.Shared.ToArrayReturn(list);
        }

        public File[] GetFilesWithSubdirectories(string pattern = "*")
        {
            var list = ListPool<File>.Shared.Rent();

            foreach (var filePath in System.IO.Directory.GetFiles(Info.FullName, pattern, SearchOption.AllDirectories))
                list.Add(new File(filePath));

            return ListPool<File>.Shared.ToArrayReturn(list);
        }

        public string[] GetFilePaths()
            => System.IO.Directory.GetFiles(Info.FullName);

        public string[] GetFilePathsWithSubdirectories(string pattern = "*")
            => System.IO.Directory.GetFiles(Info.FullName, pattern, SearchOption.AllDirectories);

        public Directory CheckExistance()
        {
            if (!Info.Exists)
                Info.Create();

            return this;
        }

        public void Create()
        {
            if (Exists)
                Delete();

            Info.Create();
        }

        public void Delete()
            => Info.Delete(true);
    }
}