using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace trash
{
	class Program
	{
		static void Main(string[] args)
		{
			try {
				MainMain(args);
			}  catch(Exception e) {
				string s;
				#if DEBUG
				s = "Error: "+e.ToString();
				#else
				s = "Error: "+e.Message;
				#endif
				Console.Error.WriteLine(s);
			}
		}

		static void MainMain(string[] args)
		{
			if (args.Length < 1) {
				Usage();
				return;
			}

			var fullPaths = new string[args.Length];
			for(int p = 0; p < args.Length; p++) {
				fullPaths[p] = GetFullPath(args[p]);
			}

			var lpFileOp = new SHFILEOPSTRUCT {
				wFunc = (uint)SHFileOperationType.FO_DELETE,
				fFlags = (ushort)(ShFileOperationFlags.FOF_NO_UI | ShFileOperationFlags.FOF_ALLOWUNDO),
				pFrom = GetShellPath(fullPaths),
				pTo = null,
				hNameMappings = IntPtr.Zero,
				hwnd = IntPtr.Zero,
				lpszProgressTitle = "",
			};
			int result = SHFileOperation(ref lpFileOp);
			
			if (lpFileOp.fAnyOperationsAborted == false && result == 0) {
				Console.WriteLine("Success!");
			} else {
				Console.WriteLine("Something went wrong"
					+(lpFileOp.fAnyOperationsAborted ? "\n at least one operation was aborted." : "")
					+(result == 0 ? "" : "\n operation returned a non zero status ["+result+"].")
				);
			}
		}

		static void Usage()
		{
			Console.WriteLine(
				 "Moves files to the recycle bin"
				+"\n Usage:"
				+"\n  "+nameof(trash)+" (list of files)"
				+"\n Notes:"
				+"\n  the list of files is a space seperated list."
				+"\n  you can enclose files with spaces in the name with double quotes"
				+"\n  DOS wildcards are allowed"
			);
		}

		//wildcard characters can appear in the file part of the path
		// these are considered illegal by GetFullPath so seperating
		// into parts before doing GetFullPath then rejoining
		static string GetFullPath(string path)
		{
			string file = Path.GetFileName(path);
			string dir = Path.GetDirectoryName(path);
			string fp = Path.GetFullPath(String.IsNullOrWhiteSpace(dir) ? "." : dir);
			string full = Path.Combine(fp,file);
			return full;
		}

		static string GetShellPath(string[] FullPaths)
		{
			StringBuilder stringBuilder = new StringBuilder();
			checked
			{
				for (int i = 0; i < FullPaths.Length; i++)
				{
					string str = FullPaths[i];
					stringBuilder.Append(str + "\0");
				}
				return stringBuilder.ToString();
			}
		}

		static int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp)
		{
			int result;
			if (IntPtr.Size == 4)
			{
				result = SHFileOperation32(ref lpFileOp);
			}
			else
			{
				SHFILEOPSTRUCT64 sHFILEOPSTRUCT = default(SHFILEOPSTRUCT64);
				sHFILEOPSTRUCT.hwnd = lpFileOp.hwnd;
				sHFILEOPSTRUCT.wFunc = lpFileOp.wFunc;
				sHFILEOPSTRUCT.pFrom = lpFileOp.pFrom;
				sHFILEOPSTRUCT.pTo = lpFileOp.pTo;
				sHFILEOPSTRUCT.fFlags = lpFileOp.fFlags;
				sHFILEOPSTRUCT.fAnyOperationsAborted = lpFileOp.fAnyOperationsAborted;
				sHFILEOPSTRUCT.hNameMappings = lpFileOp.hNameMappings;
				sHFILEOPSTRUCT.lpszProgressTitle = lpFileOp.lpszProgressTitle;
				int arg_97_0 = SHFileOperation64(ref sHFILEOPSTRUCT);
				lpFileOp.fAnyOperationsAborted = sHFILEOPSTRUCT.fAnyOperationsAborted;
				result = arg_97_0;
			}
			return result;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation", SetLastError = true, ThrowOnUnmappableChar = true)]
		private static extern int SHFileOperation32(ref SHFILEOPSTRUCT lpFileOp);

		[DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation", SetLastError = true, ThrowOnUnmappableChar = true)]
		private static extern int SHFileOperation64(ref SHFILEOPSTRUCT64 lpFileOp);
	}


	[Flags]
	enum ShFileOperationFlags : ushort
	{
		FOF_MULTIDESTFILES = 1,
		FOF_CONFIRMMOUSE = 2,
		FOF_SILENT = 4,
		FOF_RENAMEONCOLLISION = 8,
		FOF_NOCONFIRMATION = 16,
		FOF_WANTMAPPINGHANDLE = 32,
		FOF_ALLOWUNDO = 64,
		FOF_FILESONLY = 128,
		FOF_SIMPLEPROGRESS = 256,
		FOF_NOCONFIRMMKDIR = 512,
		FOF_NOERRORUI = 1024,
		FOF_NOCOPYSECURITYATTRIBS = 2048,
		FOF_NORECURSION = 4096,
		FOF_NO_CONNECTED_ELEMENTS = 8192,
		FOF_WANTNUKEWARNING = 16384,
		FOF_NORECURSEREPARSE = 32768,
		FOF_NO_UI = 4 | 16 | 1024 | 512 //FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR
	}
	
	enum SHFileOperationType : uint
	{
		FO_MOVE = 1u,
		FO_COPY,
		FO_DELETE,
		FO_RENAME
	}


	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
	struct SHFILEOPSTRUCT
	{
		internal IntPtr hwnd;

		internal uint wFunc;
		
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string pFrom;
		
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string pTo;

		internal ushort fFlags;
		
		internal bool fAnyOperationsAborted;
		
		internal IntPtr hNameMappings;
		
		[MarshalAs(UnmanagedType.LPTStr)]
		internal string lpszProgressTitle;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	struct SHFILEOPSTRUCT64
	{
		internal IntPtr hwnd;

		internal uint wFunc;

		[MarshalAs(UnmanagedType.LPTStr)]
		internal string pFrom;

		[MarshalAs(UnmanagedType.LPTStr)]
		internal string pTo;

		internal ushort fFlags;

		internal bool fAnyOperationsAborted;

		internal IntPtr hNameMappings;

		[MarshalAs(UnmanagedType.LPTStr)]
		internal string lpszProgressTitle;
	}
}
