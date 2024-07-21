using System;
using System.IO;

namespace CommonLib.Serialization.Objects
{
    public class FileObject : Object
    {
        [Flags]
        public enum SerializedData : byte
        {
            Name = 0,
            Path = 2,
            Extension = 4,
            Content = 8
        }

        public string Name { get; set; }
        public string Path { get; set; }

        public string Extension { get; set; }

        public byte[] Content { get; set; }

        public SerializedData Flags { get; set; }

        public bool HasName => (Flags & SerializedData.Name) != 0;
        public bool HasPath => (Flags & SerializedData.Path) != 0;
        public bool HasExtension => (Flags & SerializedData.Extension) != 0;
        public bool HasContent => (Flags & SerializedData.Content) != 0;

        public FileObject()
        { }

        public FileObject(string name, string path, string extension, byte[] content, SerializedData serializedData = SerializedData.Name | SerializedData.Path | SerializedData.Extension | SerializedData.Content)
        {
            Name = name;
            Path = path;
            Extension = extension;
            Content = content;
            Flags = serializedData;
        }

        public FileObject(string filePath, SerializedData serializedData = SerializedData.Name | SerializedData.Path | SerializedData.Extension | SerializedData.Content)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File does not exist", filePath);

            Name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Path = System.IO.Path.GetFullPath(filePath);
            Extension = System.IO.Path.GetExtension(filePath);
            Content = File.ReadAllBytes(filePath);
            Flags = serializedData;
        }

        public override void Serialize(Serializer serializer)
        {
            base.Serialize(serializer);

            serializer.Put((byte)Flags);

            if ((Flags & SerializedData.Name) != 0)
                serializer.Put(Name);

            if ((Flags & SerializedData.Path) != 0)
                serializer.Put(Path);

            if ((Flags & SerializedData.Extension) != 0)
                serializer.Put(Extension);

            if ((Flags & SerializedData.Content) != 0)
                serializer.Put(Content);
        }
    }
}