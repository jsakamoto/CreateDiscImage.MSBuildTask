using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IMAPI2FS;
using Microsoft.Build.Framework;

namespace MSBuildTask
{
    public class CreateDiscImage : Microsoft.Build.Utilities.Task
    {
        public string FileSystemType { get; set; } = "ISO9660";

        public bool UseJoliet { get; set; } = true;

        public ITaskItem[] RemoveRoots { get; set; } = new ITaskItem[] { };

        public string VolumeLabel { get; set; } = "";

        [Required]
        public string OutputFilePath { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        /// <summary>
        /// executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, $"CreateDiscImage: [{this.OutputFilePath}]");
            try
            {
                if (File.Exists(this.OutputFilePath)) File.Delete(this.OutputFilePath);
            }
            catch (IOException e)
            {
                Log.LogErrorFromException(e, showStackTrace: false);
                return false;
            }

            var fileSystemImage = default(MsftFileSystemImage);
            var discRoot = default(IFsiDirectoryItem);
            var resultImage = default(IFileSystemImageResult);
            var imageStream = default(FsiStream);
            var fileItemStreams = new List<FsiStream>();
            try
            {
                fileSystemImage = new MsftFileSystemImage();
                fileSystemImage.FileSystemsToCreate =
                    this.FileSystemType == "ISO9660" ? FsiFileSystems.FsiFileSystemISO9660 :
                    this.FileSystemType == "UDF" ? FsiFileSystems.FsiFileSystemUDF :
                    FsiFileSystems.FsiFileSystemUnknown;
                if (this.UseJoliet && this.FileSystemType == "ISO9660") fileSystemImage.FileSystemsToCreate |= FsiFileSystems.FsiFileSystemJoliet;
                fileSystemImage.FreeMediaBlocks = 0;
                fileSystemImage.VolumeName = this.VolumeLabel;

                var removeRootsFullPath = this.RemoveRoots
                    .Select(item => item.GetMetadata("FullPath"))
                    .Select(path => path.EndsWith("\\") ? path : path + "\\")
                    .Select(path => path.ToLower())
                    .ToArray();

                discRoot = fileSystemImage.Root;
                var registerdSubDirs = new HashSet<string>();
                foreach (var sourceFile in this.SourceFiles)
                {
                    var srcFullPath = sourceFile.GetMetadata("FullPath");
                    var srcFullPathToLower = srcFullPath.ToLower();
                    var srcPath = string.Join("", new[] { "Directory", "FileName", "Extension" }.Select(name => sourceFile.GetMetadata(name)));

                    var fullPathToRemoveRoot = removeRootsFullPath.FirstOrDefault(path => srcFullPathToLower.StartsWith(path));
                    srcPath = removeRootsFullPath == null ? srcPath : srcFullPath.Substring(fullPathToRemoveRoot.Length);

                    Log.LogMessage(MessageImportance.Low, $"CreateDiscImage: srcFullPath is [{srcFullPath}]");
                    Log.LogMessage(MessageImportance.Low, $"CreateDiscImage: srcPath is [{srcPath}]");

                    var subDir = Path.GetDirectoryName(srcPath);
                    if (subDir != "" && !registerdSubDirs.Contains(subDir))
                    {
                        var dirs = subDir.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        dirs.Aggregate("", (prev, current) =>
                        {
                            var dir = Path.Combine(prev, current);
                            if (!registerdSubDirs.Contains(dir))
                            {
                                Log.LogMessage(MessageImportance.Low, $"CreateDiscImage: AddDirectory [{dir}]");
                                discRoot.AddDirectory(dir);
                                registerdSubDirs.Add(dir);
                            }
                            return dir;
                        });
                    }

                    Log.LogMessage(MessageImportance.Low, $"CreateDiscImage: AddFile [{srcPath}]");
                    var fileItemStream = default(FsiStream);
                    SHCreateStreamOnFile(srcFullPath, STGM.READ, out fileItemStream);
                    discRoot.AddFile(srcPath, fileItemStream);
                    fileItemStreams.Add(fileItemStream);
                }

                resultImage = fileSystemImage.CreateResultImage();
                imageStream = resultImage.ImageStream;
                WriteIStreamToFile(imageStream, this.OutputFilePath);
            }
            finally
            {
                try
                {
                    if (imageStream != null) Marshal.ReleaseComObject(imageStream);
                    if (resultImage != null) Marshal.ReleaseComObject(resultImage);
                    if (discRoot != null) Marshal.ReleaseComObject(discRoot);
                    if (fileSystemImage != null) Marshal.ReleaseComObject(fileSystemImage);
                    imageStream = null;
                    resultImage = null;
                    discRoot = null;
                    fileSystemImage = null;
                    fileItemStreams.ForEach(stream => Marshal.ReleaseComObject(stream));
                    fileItemStreams.Clear();
                }
                catch (Exception e)
                {
                    Log.LogWarningFromException(e, showStackTrace: true);
                }
            }

            Log.LogMessage(MessageImportance.Normal, $"CreateDiscImage: Complete.");
            return !Log.HasLoggedErrors;
        }

        public static void WriteIStreamToFile(IStream stream, string filePath)
        {
            using (var outputFileStream = File.OpenWrite(filePath))
            {
                const uint toRead = 2048;
                var buffer = new byte[toRead];
                var cbRead = default(uint);
                do
                {
                    stream.RemoteRead(out buffer[0], toRead, out cbRead);
                    outputFileStream.Write(buffer, 0, (int)cbRead);
                } while (cbRead == 2048);
            }
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false, EntryPoint = "SHCreateStreamOnFileW")]
        private static extern void SHCreateStreamOnFile(string pszFile, STGM grfMode, out FsiStream ppstm);

        [Flags]
        private enum STGM
        {
            DIRECT = 0x00000000,
            TRANSACTED = 0x00010000,
            SIMPLE = 0x08000000,
            READ = 0x00000000,
            WRITE = 0x00000001,
            READWRITE = 0x00000002,
            SHARE_DENY_NONE = 0x00000040,
            SHARE_DENY_READ = 0x00000030,
            SHARE_DENY_WRITE = 0x00000020,
            SHARE_EXCLUSIVE = 0x00000010,
            PRIORITY = 0x00040000,
            DELETEONRELEASE = 0x04000000,
            NOSCRATCH = 0x00100000,
            CREATE = 0x00001000,
            CONVERT = 0x00020000,
            FAILIFTHERE = 0x00000000,
            NOSNAPSHOT = 0x00200000,
            DIRECT_SWMR = 0x00400000,
        }
    }
}
