using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfflineDownloader.Models
{
    public class mFileInfo
    {
        public mFileInfo() { }
        public mFileInfo(string mFileName, long mFileSize)
        {
            fileName = mFileName;
            fileSize = mFileSize;
        }
        public string fileName { get; set; }
        public long fileSize { get; set; }
    }
}