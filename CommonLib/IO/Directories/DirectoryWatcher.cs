using CommonLib.Extensions;
using CommonLib.Utilities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonLib.IO
{
    public class DirectoryWatcher
    {
        private FileSystemWatcher _watcher;
        private Dictionary<string, DateTime> _changes;

        public NotifyFilters Filters
        {
            get => _watcher.NotifyFilter;
            set => _watcher.NotifyFilter = value;
        }

        public bool IsEnabled
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public event Action<string> OnChanged;
        public event Action<string> OnCreated;
        public event Action<string> OnDeleted;

        public DirectoryWatcher(string path, NotifyFilters filters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.Size)
        {
            _watcher = new FileSystemWatcher(path);
            _watcher.NotifyFilter = filters;

            _watcher.Changed += Changed;
            _watcher.Created += Created;
            _watcher.Deleted += Deleted;

            _watcher.EnableRaisingEvents = true;

            _changes = new Dictionary<string, DateTime>();
        }

        private void Deleted(object _, FileSystemEventArgs ev)
            => OnDeleted.Call(ev.FullPath);

        private void Created(object _, FileSystemEventArgs ev)
            => OnCreated.Call(ev.FullPath);

        private void Changed(object _, FileSystemEventArgs ev)
        {
            if (_changes.ContainsKey(ev.FullPath) && DateTime.Now.Subtract(_changes[ev.FullPath]).TotalMilliseconds < 500)
                return;

            _changes[ev.FullPath] = DateTime.Now;

            Task.Run(async () =>
            {
                while (File.IsLocked(ev.FullPath))
                    await Task.Delay(100);
            }).ContinueWith(_ => OnChanged.Call(ev.FullPath));
        }
    }
}
