using System;
using System.Collections.Generic;
using System.IO;

namespace WEBAPI_m1IL_1.Utils
{
    public class DocumentationItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
    }
    public static class ScannerUtils
    {
        public static List<DocumentationItem> ScanDirectory(string rootPath)
        {
            var items = new List<DocumentationItem>();

            foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                items.Add(new DocumentationItem
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    IsDirectory = true
                });
            }

            foreach (var file in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                items.Add(new DocumentationItem
                {
                    Name = Path.GetFileName(file),
                    Path = file,
                    IsDirectory = false
                });
            }

            return items;
        }
    }
}