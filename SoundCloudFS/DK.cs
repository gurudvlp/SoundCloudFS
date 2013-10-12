// 
//  DK.cs
//  
//  Author:
//       Brian Murphy <gurudvlp@gmail.com>
// 
//  Copyright (c) 2012 Brian Murphy
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// 

using System;
using System.IO;
using System.Collections;
using Dokan;
using btEngine;

namespace Dokan.SoundCloud
{
    class DK : DokanOperations
    {
        private string root_;
        private int count_;
		
		public string MountPoint = "s:\\";
		public string VolumeLabel = "SoundCloudFS";
		public ushort ThreadCount = 5;
		public bool DebugMode = true;
		
		public DK()
		{
			
		}
		
        public DK(string root)
        {
            root_ = root;
            count_ = 1;
        }

        private string GetPath(string filename)
        {
            string path = root_ + filename;
            Console.Error.WriteLine("GetPath : {0}", path);
            return path;
        }

        public int CreateFile(String filename, FileAccess access, FileShare share,
            FileMode mode, FileOptions options, DokanFileInfo info)
        {
			Logging.Write("CreateFile (" + filename + ")");
			if(mode.HasFlag(FileMode.Open)) { Logging.Write("\tFileMode.Open true"); }
			if(mode.HasFlag(FileMode.OpenOrCreate)) { Logging.Write("\tFileMode.OpenOrCreate true"); }
			if(access.HasFlag(FileAccess.Read)) { Logging.Write("\tFileAccess.Read true"); }
			if(access.HasFlag(FileAccess.ReadWrite)) { Logging.Write("\tFileAccess.ReadWrite true"); }
			
			int nodeid = SoundCloudFS.FileTree.Node.FindNode(filename);
			if(nodeid < 0)
			{
				string pfilename = SoundCloudFS.FileTree.Node.ParseNodeName(filename);
				int ttnid = SoundCloudFS.FileTree.Node.FindNode(pfilename);
				
				if(ttnid < 0)
				{
					//	This node just straight up not found
					return -DokanNet.ERROR_FILE_NOT_FOUND;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				return 0;
			}
			/*
            string path = GetPath(filename);
            info.Context = count_++;
            if (File.Exists(path))
            {
                return 0;
            }
            else if(Directory.Exists(path))
            {
                info.IsDirectory = true;
                return 0;
            }
            else
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }*/
        }

        public int OpenDirectory(String filename, DokanFileInfo info)
        {
			Logging.Write("OpenDirectory (" + filename + ")");
            info.Context = count_++;
            
			int nodeid = -1;
			if(filename == "\\") { nodeid = 0; }
			else { nodeid = SoundCloudFS.FileTree.Node.FindNode(filename); }
			
			if(nodeid < 0)
			{
				Logging.Write("\tFile Not Found");
				return -DokanNet.ERROR_PATH_NOT_FOUND;
			}
			else 
			{
				info.IsDirectory = true;
				return 0;
			}
			
			/*if (Directory.Exists(GetPath(filename)))
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;*/
        }

        public int CreateDirectory(String filename, DokanFileInfo info)
        {
			Logging.Write("CreateDirectory (" + filename + ")");
            return -1;
        }

        public int Cleanup(String filename, DokanFileInfo info)
        {
            //Console.WriteLine("%%%%%% count = {0}", info.Context);
            return 0;
        }

        public int CloseFile(String filename, DokanFileInfo info)
        {
			Logging.Write("CloseFile (" + filename + ")");
            return 0;
        }

        public int ReadFile(String filename, Byte[] buffer, ref uint readBytes,
            long offset, DokanFileInfo info)
        {
			Logging.Write("ReadFile (" + filename + ", buflen=" + buffer.Length.ToString() + ", offset=" + offset.ToString() + ")");
            
			string nodename = SoundCloudFS.FileTree.Node.ParseNodeName(filename);
			string trackname = SoundCloudFS.FileTree.Node.ParseTrackFilename(filename);
			int nodeid = SoundCloudFS.FileTree.Node.FindNode(nodename);
			long size = buffer.Length;
			bool found = false;
			
			if(nodeid < 0) { return -DokanNet.ERROR_PATH_NOT_FOUND; }
			
			for(int et = 0; et < Engine.FSNodes[nodeid].Tracks.Length; et++)
			{
				if(Engine.FSNodes[nodeid].Tracks[et] != null)
				{
					if(Engine.FSNodes[nodeid].Tracks[et].Filename == trackname)
					{
						Engine.FSNodes[nodeid].Tracks[et].Touch();
						if(!Engine.FSNodes[nodeid].Tracks[et].Retrieved) { if(!Engine.FSNodes[nodeid].Tracks[et].Retrieve()) { return -DokanNet.ERROR_FILE_NOT_FOUND; } }
						
						
						if(offset < Engine.FSNodes[nodeid].Tracks[et].RawData.Length)
						{
							if(offset + (long)size > (long)Engine.FSNodes[nodeid].Tracks[et].RawData.Length) { size = ((long)Engine.FSNodes[nodeid].Tracks[et].RawData.Length - offset); }
							Buffer.BlockCopy(Engine.FSNodes[nodeid].Tracks[et].RawData, (int)offset, buffer, 0, (int)size);
							found = true;
							break;
						}
						else
						{
							size = 0;
						}
					}
				}
			}
			
			if(!found) { return -DokanNet.ERROR_FILE_NOT_FOUND; }
			readBytes = (uint)size;
			return 0;
			
			/*
			try
            {
                FileStream fs = File.OpenRead(GetPath(filename));
                fs.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }*/
        }

        public int WriteFile(String filename, Byte[] buffer,
            ref uint writtenBytes, long offset, DokanFileInfo info)
        {
			Logging.Write("WriteFile (" + filename + ")");
            return -1;
        }

        public int FlushFileBuffers(String filename, DokanFileInfo info)
        {
            return -1;
        }

        public int GetFileInformation(String filename, FileInformation fileinfo, DokanFileInfo info)
        {
			Logging.Write("GetFileInformation (" + filename + ")");
			
			int nodeid = SoundCloudFS.FileTree.Node.FindNode(filename);
			
			if(nodeid < 0)
			{
				string nodename = SoundCloudFS.FileTree.Node.ParseNodeName(filename);
				string trackname = SoundCloudFS.FileTree.Node.ParseTrackFilename(filename);
				int bnode = SoundCloudFS.FileTree.Node.FindNode(nodename);
				
				if(bnode < 0)
				{
					//	No node was found for this
				}
				else
				{
					if(Engine.FSNodes[bnode].Tracks != null)
					{
						for(int et = 0; et < Engine.FSNodes[bnode].Tracks.Length; et++)
						{
							if(Engine.FSNodes[bnode].Tracks[et] != null)
							{
								if(Engine.FSNodes[bnode].Tracks[et].Filename == trackname)
								{
									fileinfo.Attributes = FileAttributes.Normal;
									fileinfo.Length = Engine.FSNodes[bnode].Tracks[et].Filesize;
									fileinfo.CreationTime = Engine.UnixTimeStampToDateTime(Engine.FSNodes[bnode].Tracks[et].UnixTimeCreated());
									fileinfo.LastWriteTime = fileinfo.CreationTime;
									fileinfo.LastAccessTime = Engine.UnixTimeStampToDateTime(Engine.FSNodes[bnode].Tracks[et].UnixTimeAccessed());
									return 0;
								}
							}
						}
					}
					else
					{
						return -1;
					}
				}
				
				return -1;
			}
			else
			{
				fileinfo.Attributes = FileAttributes.Directory;
				fileinfo.Length = 0;
				fileinfo.CreationTime = new DateTime(2012, 10, 17, 10, 45, 0);
				fileinfo.LastAccessTime = new DateTime(2012, 10, 17, 10, 45, 0);
				fileinfo.LastWriteTime = new DateTime(2012, 10, 17, 10, 45, 0);
				return 0;
			}
			
			if(nodeid < 0)
			{
				return -1;
			}
			
			//	Technically there are only directories right now,so this is some
			//	gobblety gook
			
			
			
            /*string path = GetPath(filename);
            if (File.Exists(path))
            {
                FileInfo f = new FileInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = f.Length;
                return 0;
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo f = new DirectoryInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = 0;// f.Length;
                return 0;
            }
            else
            {
                return -1;
            }*/
        }

        public int FindFiles(String filename, ArrayList files, DokanFileInfo info)
        {
			Logging.Write("FindFiles (" + filename + ")");
            //string path = GetPath(filename);
            int nodeid = SoundCloudFS.FileTree.Node.FindNode(filename);
			
			if(nodeid < 0) { return -1; }
			else
			{
				if(Engine.FSNodes[nodeid].NodeType == SoundCloudFS.FileTree.Node.NodeTypeSearch)
				{
					//	This node should be a search result.
					if(!Engine.FSNodes[nodeid].HasSearched) 
					{
						Engine.FSNodes[nodeid].RunSearch(); 
						Engine.FSNodes[nodeid].HasSearched = true;
					}
					
					if(Engine.FSNodes[nodeid].Tracks != null)
					{
						for(int et = 0; et < Engine.FSNodes[nodeid].Tracks.Length; et++)
						{
							if(Engine.FSNodes[nodeid].Tracks[et] != null)
							{
								FileInformation fi = new FileInformation();
								fi.Attributes = FileAttributes.Normal;
								fi.CreationTime = Engine.UnixTimeStampToDateTime(Engine.FSNodes[nodeid].Tracks[et].UnixTimeCreated());
								fi.LastAccessTime = Engine.UnixTimeStampToDateTime(Engine.FSNodes[nodeid].Tracks[et].UnixTimeAccessed());
								fi.LastWriteTime = fi.CreationTime;
								fi.FileName = Engine.FSNodes[nodeid].Tracks[et].Filename;
								
								if(Engine.FSNodes[nodeid].Tracks[et].Filesize > 0) { fi.Length = Engine.FSNodes[nodeid].Tracks[et].Filesize; }
								else { fi.Length = Engine.FSNodes[nodeid].Tracks[et].CalculateFilesize(); }
								
								files.Add(fi);
							}
						}
					}
				}
				else if(Engine.FSNodes[nodeid].SubNodes != null)
				{
					for(int esn = 0; esn < Engine.FSNodes[nodeid].SubNodes.Length; esn++)
					{
						int tsnid = Engine.FSNodes[nodeid].SubNodes[esn];
						
						if(tsnid > -1)
						{
							FileInformation fi = new FileInformation();
							fi.Attributes = FileAttributes.Directory;
							fi.FileName = Engine.FSNodes[tsnid].Name;
							fi.CreationTime = new DateTime(2012, 10, 17, 10, 45, 0);
							fi.LastWriteTime = new DateTime(2012, 10, 17, 10, 45, 0);
							fi.LastAccessTime = new DateTime(2012, 10, 17, 10, 45, 0);
							fi.Length = 0;
							files.Add(fi);
						}
					}
				}
				return 0;
			}
			
			/*if (Directory.Exists(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                FileSystemInfo[] entries = d.GetFileSystemInfos();
                foreach (FileSystemInfo f in entries)
                {
                    FileInformation fi = new FileInformation();
                    fi.Attributes = f.Attributes;
                    fi.CreationTime = f.CreationTime;
                    fi.LastAccessTime = f.LastAccessTime;
                    fi.LastWriteTime = f.LastWriteTime;
                    fi.Length = (f is DirectoryInfo) ? 0 : ((FileInfo)f).Length;
                    fi.FileName = f.Name;
                    files.Add(fi);
                }
                return 0;
            }
            else
            {
                return -1;
            }*/
        }

        public int SetFileAttributes(String filename, FileAttributes attr, DokanFileInfo info)
        {
			Logging.Write("SetFileAttributes (" + filename + ")");
            return -1;
        }

        public int SetFileTime(String filename, DateTime ctime,
                DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            return -1;
        }

        public int DeleteFile(String filename, DokanFileInfo info)
        {
			Logging.Write("DeleteFile (" + filename + ")");
            return -1;
        }

        public int DeleteDirectory(String filename, DokanFileInfo info)
        {
			Logging.Write("DeleteDirectory (" + filename + ")");
            return -1;
        }

        public int MoveFile(String filename, String newname, bool replace, DokanFileInfo info)
        {
            return -1;
        }

        public int SetEndOfFile(String filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetAllocationSize(String filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int LockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int UnlockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes,
            ref ulong totalFreeBytes, DokanFileInfo info)
        {
            /*freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;*/
			totalBytes = (ulong)SoundCloudFS.FileTree.Node.TotalTrackSize;
			freeBytesAvailable = 0;
			totalFreeBytes = 0;
			
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
			Logging.Write("Unmount");
            return 0;
        }
		
		public void Start()
		{
			DokanOptions opt = new DokanOptions();
			opt.DebugMode = this.DebugMode;
			opt.MountPoint = this.MountPoint;
			opt.VolumeLabel = this.VolumeLabel;
			opt.ThreadCount = this.ThreadCount;
			
			btEngine.Logging.Write("MP: " + this.MountPoint);
			btEngine.Logging.Write("VL: " + this.VolumeLabel);
			
			
			int status = DokanNet.DokanMain(opt, new DK("C:"));
            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
					Logging.Write("Drive letter error");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    Logging.Write("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Logging.Write("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Logging.Write("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Logging.Write("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Logging.Write("Success");
                    break;
                default:
                    Logging.Write(string.Format ("Unknown status: %d", status));
                    break;
                       
            }
		}

        /*static void Main(string[] args)
        {
            DokanOptions opt = new DokanOptions();
            opt.DebugMode = true;
            opt.MountPoint = "n:\\";
            opt.ThreadCount = 5;
            int status = DokanNet.DokanMain(opt, new Mirror("C:"));
            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                    Console.WriteLine("Drvie letter error");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    Console.WriteLine("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Console.WriteLine("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Console.WriteLine("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Console.WriteLine("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Console.WriteLine("Success");
                    break;
                default:
                    Console.WriteLine("Unknown status: %d", status);
                    break;
                       
            }
        }*/
    }
}
