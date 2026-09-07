// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Runtime.InteropServices;
using RSSAM.API;

namespace RSSAM.UnitTests;

public sealed class NativeWrapperTests
{
    [Fact]
    public void SetupFunctions_RejectsNullObjectPointer()
    {
        var wrapper = new TestNativeWrapper();

        var exception = Assert.Throws<InvalidOperationException>(
            () => wrapper.SetupFunctions(IntPtr.Zero));

        Assert.Contains(nameof(TestFunctions), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupFunctions_RejectsNullVirtualTable()
    {
        var objectPointer = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            Marshal.WriteIntPtr(objectPointer, IntPtr.Zero);
            var wrapper = new TestNativeWrapper();

            var exception = Assert.Throws<InvalidOperationException>(
                () => wrapper.SetupFunctions(objectPointer));

            Assert.Contains("virtual table", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Marshal.FreeHGlobal(objectPointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestFunctions
    {
        public IntPtr First;
    }

    private sealed class TestNativeWrapper : NativeWrapper<TestFunctions>
    {
    }
}
