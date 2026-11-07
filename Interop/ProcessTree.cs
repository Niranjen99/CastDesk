using System.Runtime.InteropServices;

namespace CastDesk.Interop;

/// <summary>Finds a process's descendants (WebView2 runs its audio in a child of its browser process).</summary>
internal static class ProcessTree
{
    /// <summary>The IDs of <paramref name="rootProcessId"/> and every process below it.</summary>
    public static HashSet<int> GetDescendantsAndSelf(int rootProcessId)
    {
        var parentOf = new Dictionary<int, int>();
        var snapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapProcess, 0);
        if (snapshot == NativeMethods.InvalidHandle) return new HashSet<int> { rootProcessId };
        try
        {
            var entry = new NativeMethods.ProcessEntry32 { Size = (uint)Marshal.SizeOf<NativeMethods.ProcessEntry32>() };
            if (NativeMethods.Process32First(snapshot, ref entry))
            {
                do parentOf[(int)entry.ProcessId] = (int)entry.ParentProcessId;
                while (NativeMethods.Process32Next(snapshot, ref entry));
            }
        }
        finally
        {
            NativeMethods.CloseHandle(snapshot);
        }

        var result = new HashSet<int> { rootProcessId };
        bool added;
        do
        {
            added = false;
            foreach (var (child, parent) in parentOf)
            {
                if (child != parent && result.Contains(parent) && result.Add(child)) added = true;
            }
        }
        while (added);
        return result;
    }

    private static class NativeMethods
    {
        public const uint SnapProcess = 0x00000002;
        public static readonly IntPtr InvalidHandle = new(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ProcessEntry32
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int PriorityClassBase;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
