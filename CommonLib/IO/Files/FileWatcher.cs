using CommonLib.Extensions;
using CommonLib.Utilities;

using System;
using System.IO;
using System.Threading.Tasks;

namespace CommonLib.IO
{
    public class FileWatcher
    {
        private FileSystemWatcher watcher;
        private string filePath;
        private DateTime lastChanged;
        private bool changedRaised;

        public NotifyFilters Filters
        {
            get => watcher.NotifyFilter;
            set => watcher.NotifyFilter = value;
        }

        public bool IsEnabled
        {
            get => watcher.EnableRaisingEvents;
            set => watcher.EnableRaisingEvents = value;
        }

        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                watcher.Path = Path.GetDirectoryName(value);
            }
        }

        public event Action OnChanged;
        public event Action OnCreated;
        public event Action OnDeleted;

        public FileWatcher(string path, NotifyFilters filters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.Size)
        {
            filePath = Path.GetFullPath(path);

            watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
            watcher.NotifyFilter = filters;

            watcher.Changed += Changed;
            watcher.Created += Created;
            watcher.Deleted += Deleted;

            watcher.EnableRaisingEvents = true;
        }

        private void Deleted(object _, FileSystemEventArgs ev)
        {
            if (Path.GetFullPath(ev.FullPath) != filePath)
                return;

            OnDeleted.Call();
        }

        private void Created(object _, FileSystemEventArgs ev)
        {
            if (Path.GetFullPath(ev.FullPath) != filePath)
                return;

            OnCreated.Call();
        }

        private void Changed(object _, FileSystemEventArgs ev)
        {
            if (Path.GetFullPath(ev.FullPath) != filePath)
                return;

            if (ev.ChangeType is WatcherChangeTypes.Changed && changedRaised && DateTime.Now.Subtract(lastChanged).TotalMilliseconds < 500)
                return;

            changedRaised = true;
            lastChanged = DateTime.Now;

            Task.Run(async () =>
            {
                while (File.IsLocked(ev.FullPath))
                    await Task.Delay(100);
            }).ContinueWith(_ => OnChanged.Call());
        }
    }
}