//
// HelloFS.cs
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2006 Jonathan Pryor
//
// Mono.Fuse example program
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;
using btEngine;

namespace Mono.Fuse.SoundCloud {
	class SoundCloudFS : Mono.Fuse.FileSystem 
	{
		const string hello_path = "/hello";
		const string data_path  = "/data";
		const string data_im_path  = "/data.im";

		const int data_size = 100000000;

		Dictionary<string, byte[]> hello_attrs = new Dictionary<string, byte[]>();
		
		
		public SoundCloudFS ()
		{
			Logging.Write(Engine.EngineName + " " + Engine.EngineVersion);
			//hello_attrs ["foo"] = Encoding.UTF8.GetBytes ("bar");
			
		}
		

		//protected override Errno OnGetPathStatus (string path, ref Stat stbuf)
		protected override Errno OnGetPathStatus (string path, out Stat stbuf)
		{
			Trace.WriteLine ("(OnGetPathStatus {0})", path);

			stbuf = new Stat ();
			//Logging.Write("OnGetPathStatus: Path: " + path);
			if(path == "/")
			{
				stbuf.st_mode = FilePermissions.S_IFDIR |
					NativeConvert.FromOctalPermissionString("0755");
				stbuf.st_nlink = 2;
				return 0;
			}
			else
			{
				for(int etrack = 0; etrack < Engine.Tracks.Length; etrack++)
				{
					if(Engine.Tracks[etrack] != null && path == "/" + Engine.Tracks[etrack].Filename)
					{
						
						stbuf.st_mode = FilePermissions.S_IFREG |
							NativeConvert.FromOctalPermissionString("0644");
						stbuf.st_nlink = 1;
						int size = Engine.Tracks[etrack].Filesize;
						stbuf.st_size = size;
						return 0;
					}
				}
				
				return Errno.ENOENT;
			}
			
			/*switch (path) {
				case "/":
					stbuf.st_mode = FilePermissions.S_IFDIR | 
						NativeConvert.FromOctalPermissionString ("0755");
					stbuf.st_nlink = 2;
					return 0;
				case hello_path:
				case data_path:
				case data_im_path:
					stbuf.st_mode = FilePermissions.S_IFREG |
						NativeConvert.FromOctalPermissionString ("0444");
					stbuf.st_nlink = 1;
					int size = 0;
					switch (path) {
						case hello_path:   size = hello_str.Length; break;
						case data_path:
						case data_im_path: size = data_size; break;
					}
					stbuf.st_size = size;
					return 0;
				default:
					return Errno.ENOENT;
			}*/
		}
		 
		protected override Errno OnReadDirectory (string path, OpenedPathInfo fi,
				out IEnumerable<DirectoryEntry> paths)
		{
			Console.WriteLine ("(OnReadDirectory {0})", path);
			paths = null;
			if (path != "/")
				return Errno.ENOENT;

			paths = GetEntries ();
			return 0;
		}
		 
		private IEnumerable<DirectoryEntry> GetEntries ()
		{
			yield return new DirectoryEntry (".");
			yield return new DirectoryEntry ("..");
			
			for(int etrack = 0; etrack < Engine.Tracks.Length; etrack++)
			{
				if(Engine.Tracks[etrack] != null) { yield return new DirectoryEntry(Engine.Tracks[etrack].Filename); }
			}
			
		}

		protected override Errno OnOpenHandle (string path, OpenedPathInfo fi)
		{
			Console.WriteLine (string.Format ("(OnOpen {0} Flags={1})", path, fi.OpenFlags));
			
			//if(fi.OpenAccess == OpenFlags.O_TRUNC & OpenFlags.O_WRONLY) { Logging.Write("File opened for truncation."); return 0; }
			
			if(fi.OpenAccess != OpenFlags.O_RDONLY) {
				Logging.Write("Somethign being opened as not READ ONLY!");
				return 0;
				//return Errno.EACCES;
			}
			
			for(int etrack = 0; etrack < Engine.Tracks.Length; etrack++)
			{
				if(Engine.Tracks[etrack] != null)
				{
					if("/" + Engine.Tracks[etrack].Filename == path)
					{
						
						return 0;
					}
				}
			}
			
			return Errno.ENOENT;
			/*if (path != hello_path && path != data_path && path != data_im_path)
				return Errno.ENOENT;
			if (path == data_im_path && !have_data_im)
				return Errno.ENOENT;
			if (fi.OpenAccess != OpenFlags.O_RDONLY)
				return Errno.EACCES;
			return 0;*/
		}

		protected override Errno OnReadHandle (string path, OpenedPathInfo fi, byte[] buf, long offset, out int bytesWritten)
		{
			Console.WriteLine ("(OnRead: Path {0}, buflen {1}, offset {2})", path, buf.Length, offset);
			bytesWritten = 0;
			int size = buf.Length;
			//if(7 != 32) { return Errno.ENOENT; }
			bool found = false;
			for(int etrack = 0; etrack < Engine.Tracks.Length; etrack++)
			{
				if(Engine.Tracks[etrack] != null && path == "/" + Engine.Tracks[etrack].Filename)
				{
					if(!Engine.Tracks[etrack].Retrieved) 
					{
						
						if(!Engine.Tracks[etrack].Retrieve()) { return Errno.EAGAIN; }
					}
					
					if(offset < Engine.Tracks[etrack].RawData.Length)
					{
						if(offset + (long)size > (long)Engine.Tracks[etrack].RawData.Length) { size = (int)((long)Engine.Tracks[etrack].RawData.Length - offset); }
						Buffer.BlockCopy(Engine.Tracks[etrack].RawData, (int)offset, buf, 0, size);
						found = true;
						break;
					}
					else
					{
						size = 0;
					}
				}
			}
			if(!found) { Logging.Write("ReadFile: File Not Found"); return Errno.ENOENT; }
			bytesWritten = size;
			/*if (path == data_im_path)
				FillData ();
			if (path == hello_path || path == data_im_path) {
				byte[] source = path == hello_path ? hello_str : data_im_str;
				if (offset < (long) source.Length) {
					if (offset + (long) size > (long) source.Length)
						size = (int) ((long) source.Length - offset);
					Buffer.BlockCopy (source, (int) offset, buf, 0, size);
				}
				else
					size = 0;
			}
			else if (path == data_path) {
				int max = System.Math.Min ((int) data_size, (int) (offset + buf.Length));
				for (int i = 0, j = (int) offset; j < max; ++i, ++j) {
					if ((j % 27) == 0)
						buf [i] = (byte) '\n';
					else
						buf [i] = (byte) ((j % 26) + 'a');
				}
			}
			else
				return Errno.ENOENT;
			*/
			//bytesWritten = size;
			return 0;
		}

		protected override Errno OnGetPathExtendedAttribute (string path, string name, byte[] value, out int bytesWritten)
		{
			Console.WriteLine ("(OnGetPathExtendedAttribute {0})", path);
			bytesWritten = 0;
			if (path != hello_path) {
				return 0;
			}
			byte[] _value;
			lock (hello_attrs) {
				if (!hello_attrs.ContainsKey (name))
					return 0;
				_value = hello_attrs [name];
			}
			if (value.Length < _value.Length) {
				return Errno.ERANGE;
			}
			Array.Copy (_value, value, _value.Length);
			bytesWritten = _value.Length;
			return 0;
		}

		protected override Errno OnSetPathExtendedAttribute (string path, string name, byte[] value, XattrFlags flags)
		{
			Console.WriteLine ("(OnSetPathExtendedAttribute {0})", path);
			if (path != hello_path) {
				return Errno.ENOSPC;
			}
			lock (hello_attrs) {
				hello_attrs [name] = value;
			}
			return 0;
		}

		protected override Errno OnRemovePathExtendedAttribute (string path, string name)
		{
			Console.WriteLine ("(OnRemovePathExtendedAttribute {0})", path);
			if (path != hello_path)
				return Errno.ENODATA;
			lock (hello_attrs) {
				if (!hello_attrs.ContainsKey (name))
					return Errno.ENODATA;
				hello_attrs.Remove (name);
			}
			return 0;
		}

		protected override Errno OnListPathExtendedAttributes (string path, out string[] names)
		{
			Console.WriteLine ("(OnListPathExtendedAttributes {0})", path);
			if (path != hello_path) {
				names = new string[]{};
				return 0;
			}
			List<string> _names = new List<string> ();
			lock (hello_attrs) {
				_names.AddRange (hello_attrs.Keys);
			}
			names = _names.ToArray ();
			return 0;
		}
		
		protected override Errno OnRemoveFile (string file)
		{
			Logging.Write("In OnRemoveFile (" + file + ")");
			//Logging.Write("Removing: " + file);
			
			for(int etrack = 0; etrack < Engine.Tracks.Length; etrack++)
			{
				if(Engine.Tracks[etrack] != null)
				{
					if("/" + Engine.Tracks[etrack].Filename == file)
					{
						Engine.Tracks[etrack] = null;
						return 0;
					}
				}
			}
			return Errno.ENOENT;
			//return base.OnRemoveFile (file);
		}


		/*public bool ParseArguments (string[] args)
		{
			for (int i = 0; i < args.Length; ++i) {
				switch (args [i]) {
					case "--data.im-in-memory":
						have_data_im = true;
						break;
					case "-h":
					case "--help":
						FileSystem.ShowFuseHelp ("hellofs");
						Console.Error.WriteLine ("hellofs options:");
						Console.Error.WriteLine ("    --data.im-in-memory    Add data.im file");
						return false;
					default:
						base.MountPoint = args [i];
						break;
				}
			}
			return true;
		}*/

		

		
	}
}