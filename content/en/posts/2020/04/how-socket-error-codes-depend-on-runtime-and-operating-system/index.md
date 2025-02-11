---
title: "How Socket Error Codes Depend on Runtime and Operating System"
date: "2020-04-27"
tags:
- programming
- dotnet
- cs
- Rider
- Mono
- netcore
---

*This blog post was [originally posted](https://blog.jetbrains.com/dotnet/2020/04/27/socket-error-codes-depend-runtime-operating-system/) on [JetBrains .NET blog](https://blog.jetbrains.com/dotnet/).*

<a href="https://www.jetbrains.com/rider/">Rider</a> consists of several processes that send messages to each other via sockets. To ensure the reliability of the whole application, it's important to properly handle all the socket errors. In our codebase, we had the following code which was adopted from <a href="https://github.com/mono/debugger-libs/blob/master/Mono.Debugging.Soft/SoftDebuggerSession.cs#L273">Mono Debugger Libs</a> and helps us communicate with debugger processes:

```cs
protected virtual bool ShouldRetryConnection (Exception ex, int attemptNumber)
{
    var sx = ex as SocketException;
    if (sx != null) {
        if (sx.ErrorCode == 10061) //connection refused
            return true;
    }
    return false;
}
```

In the case of a failed connection because of a “ConnectionRefused” error, we are retrying the connection attempt. It works fine with .NET Framework and Mono. However, once we migrated to .NET Core, this method no longer correctly detects the "connection refused" situation on Linux and macOS. If we open the <code>SocketException</code> <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketexception?view=netframework-4.8">documentation</a>, we will learn that this class has three different properties with error codes:
<ul>
	<li><code>SocketError SocketErrorCode</code>: Gets the error code that is associated with this exception.</li>
	<li><code>int ErrorCode</code>: Gets the error code that is associated with this exception.</li>
	<li><code>int NativeErrorCode</code>: Gets the Win32 error code associated with this exception.</li>
</ul>
What's the difference between these properties? Should we expect different values on different runtimes or different operating systems? Which one should we use in production? Why do we have problems with <code>ShouldRetryConnection</code> on .NET Core? Let's figure it all out!

<!--more-->

<h2><strong>Digging into the problem</strong></h2>
Let’s start with the following program, which prints error code property values for <code>SocketError.ConnectionRefused</code>:

```cs
var se = new SocketException((int) SocketError.ConnectionRefused);
Console.WriteLine((int)se.SocketErrorCode);
Console.WriteLine(se.ErrorCode);
Console.WriteLine(se.NativeErrorCode);
```

If we run it on <strong>Windows</strong>, we will get the same value on .NET Framework, Mono, and .NET Core:
<table>
<tbody>
<tr>
<td></td>
<td><strong>SocketErrorCode</strong></td>
<td><strong>ErrorCode</strong></td>
<td><strong>NativeErrorCode</strong></td>
</tr>
<tr>
<td>.NET Framework</td>
<td>10061</td>
<td>10061</td>
<td>10061</td>
</tr>
<tr>
<td>Mono</td>
<td>10061</td>
<td>10061</td>
<td>10061</td>
</tr>
<tr>
<td>.NET Core</td>
<td>10061</td>
<td>10061</td>
<td>10061</td>
</tr>
</tbody>
</table>
10061 corresponds to the code of the connection refused socket error code <a href="https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2">in Windows</a> (also known as <code>WSAECONNREFUSED</code>).
Now let's run the same program on <strong>Linux</strong>:
<table>
<tbody>
<tr>
<td></td>
<td><strong>SocketErrorCode</strong></td>
<td><strong>ErrorCode</strong></td>
<td><strong>NativeErrorCode</strong></td>
</tr>
<tr>
<td>Mono</td>
<td>10061</td>
<td>10061</td>
<td>10061</td>
</tr>
<tr>
<td>.NET Core</td>
<td>10061</td>
<td>111</td>
<td>111</td>
</tr>
</tbody>
</table>
As you can see, Mono returns Windows-compatible error codes. The situation with .NET Core is different: it returns a Windows-compatible value for SocketErrorCode (10061) and a Linux-like value for <code>ErrorCode</code> and <code>NativeErrorCode</code> (111).
Finally, let's check <strong>macOS</strong>:
<table>
<tbody>
<tr>
<td></td>
<td><strong>SocketErrorCode</strong></td>
<td><strong>ErrorCode</strong></td>
<td><strong>NativeErrorCode</strong></td>
</tr>
<tr>
<td>Mono</td>
<td>10061</td>
<td>10061</td>
<td>10061</td>
</tr>
<tr>
<td>.NET Core</td>
<td>10061</td>
<td>61</td>
<td>61</td>
</tr>
</tbody>
</table>
Here, Mono is completely Windows-compatible again, but .NET Core returns 61 for <code>ErrorCode</code> and <code>NativeErrorCode</code>.
In the <a href="https://www.ibm.com/support/knowledgecenter/SSEPGG_11.1.0/com.ibm.db2.luw.messages.doc/doc/r0058740.html">IBM Knowledge Center</a>, we can find a few more values for the connection refused error code from the Unix world (also known as <code>ECONNREFUSED</code>):
<ul>
	<li>AIX: 79</li>
	<li>HP-UX: 239</li>
	<li>Solaris: 146</li>
</ul>
For a better understanding of what's going on, let’s check out the source code of all the properties.
<h2><strong>SocketErrorCode</strong></h2>
<code>SocketException.SocketErrorCode</code> returns a value from the <code>SocketError</code> enum. The numerical values of the enum elements are the same on all the runtimes (see its implementation in <a href="https://referencesource.microsoft.com/#System/net/System/Net/Sockets/SocketErrors.cs,cb4675d5a1a2c847">.NET Framework</a>, <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/System.Net.Primitives/src/System/Net/Sockets/SocketError.cs">.NET Core 3.1.3</a>, and <a href="https://github.com/mono/mono/blob/mono-6.8.0.105/mcs/class/referencesource/System/net/System/Net/Sockets/SocketErrors.cs">Mono 6.8.0.105</a>):

```c
public enum SocketError
{
    SocketError = -1, // 0xFFFFFFFF
    Success = 0,
    OperationAborted = 995, // 0x000003E3
    IOPending = 997, // 0x000003E5
    Interrupted = 10004, // 0x00002714
    AccessDenied = 10013, // 0x0000271D
    Fault = 10014, // 0x0000271E
    InvalidArgument = 10022, // 0x00002726
    TooManyOpenSockets = 10024, // 0x00002728
    WouldBlock = 10035, // 0x00002733
    InProgress = 10036, // 0x00002734
    AlreadyInProgress = 10037, // 0x00002735
    NotSocket = 10038, // 0x00002736
    DestinationAddressRequired = 10039, // 0x00002737
    MessageSize = 10040, // 0x00002738
    ProtocolType = 10041, // 0x00002739
    ProtocolOption = 10042, // 0x0000273A
    ProtocolNotSupported = 10043, // 0x0000273B
    SocketNotSupported = 10044, // 0x0000273C
    OperationNotSupported = 10045, // 0x0000273D
    ProtocolFamilyNotSupported = 10046, // 0x0000273E
    AddressFamilyNotSupported = 10047, // 0x0000273F
    AddressAlreadyInUse = 10048, // 0x00002740
    AddressNotAvailable = 10049, // 0x00002741
    NetworkDown = 10050, // 0x00002742
    NetworkUnreachable = 10051, // 0x00002743
    NetworkReset = 10052, // 0x00002744
    ConnectionAborted = 10053, // 0x00002745
    ConnectionReset = 10054, // 0x00002746
    NoBufferSpaceAvailable = 10055, // 0x00002747
    IsConnected = 10056, // 0x00002748
    NotConnected = 10057, // 0x00002749
    Shutdown = 10058, // 0x0000274A
    TimedOut = 10060, // 0x0000274C
    ConnectionRefused = 10061, // 0x0000274D
    HostDown = 10064, // 0x00002750
    HostUnreachable = 10065, // 0x00002751
    ProcessLimit = 10067, // 0x00002753
    SystemNotReady = 10091, // 0x0000276B
    VersionNotSupported = 10092, // 0x0000276C
    NotInitialized = 10093, // 0x0000276D
    Disconnecting = 10101, // 0x00002775
    TypeNotFound = 10109, // 0x0000277D
    HostNotFound = 11001, // 0x00002AF9
    TryAgain = 11002, // 0x00002AFA
    NoRecovery = 11003, // 0x00002AFB
    NoData = 11004, // 0x00002AFC
}
```

These values correspond to the <a href="https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2">Windows Sockets Error Codes</a>.
<h2><strong>NativeErrorCode</strong></h2>
In <a href="https://referencesource.microsoft.com/#System/net/System/Net/SocketException.cs,89">.NET Framework</a> and <a href="https://github.com/mono/mono/blob/mono-6.8.0.105/mcs/class/referencesource/System/net/System/Net/SocketException.cs#L101">Mono</a>, <code>SocketErrorCode</code> and <code>NativeErrorCode</code> always have the same values:

```cs
public SocketError SocketErrorCode {
    //
    // the base class returns the HResult with this property
    // we need the Win32 Error Code, hence the override.
    //
    get {
        return (SocketError)NativeErrorCode;
    }
}
```

In .NET Core, the native code is calculated in the constructor (see <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/System.Net.Primitives/src/System/Net/SocketException.cs#L20">SocketException.cs#L20</a>):

```cs
public SocketException(int errorCode) : this((SocketError)errorCode)
// ...
internal SocketException(SocketError socketError) : base(GetNativeErrorForSocketError(socketError))
```

The Windows implementation of <code>GetNativeErrorForSocketError</code> is trivial (see <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/System.Net.Primitives/src/System/Net/SocketException.Windows.cs">SocketException.Windows.cs</a>):

```cs
private static int GetNativeErrorForSocketError(SocketError error)
{
    // SocketError values map directly to Win32 error codes
    return (int)error;
}
```

The Unix implementation is more complicated (see <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/System.Net.Primitives/src/System/Net/SocketException.Unix.cs">SocketException.Unix.cs</a>):

```cs
private static int GetNativeErrorForSocketError(SocketError error)
{
    int nativeErr = (int)error;
    if (error != SocketError.SocketError)
    {
        Interop.Error interopErr;

        // If an interop error was not found, then don't invoke Info().RawErrno as that will fail with assert.
        if (SocketErrorPal.TryGetNativeErrorForSocketError(error, out interopErr))
        {
            nativeErr = interopErr.Info().RawErrno;
        }
    }

    return nativeErr;
}
```

<code>TryGetNativeErrorForSocketError</code> should convert <code>SocketError</code> to the native Unix error code.
Unfortunately, there exists no unequivocal mapping between Windows and Unix error codes. As such, the .NET team decided to create a <code>Dictionary</code> that maps error codes in the best possible way (see <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/Common/src/System/Net/Sockets/SocketErrorPal.Unix.cs">SocketErrorPal.Unix.cs</a>):

```cs
private const int NativeErrorToSocketErrorCount = 42;
private const int SocketErrorToNativeErrorCount = 40;

// No Interop.Errors are included for the following SocketErrors, as there's no good mapping:
// - SocketError.NoRecovery
// - SocketError.NotInitialized
// - SocketError.ProcessLimit
// - SocketError.SocketError
// - SocketError.SystemNotReady
// - SocketError.TypeNotFound
// - SocketError.VersionNotSupported

private static readonly Dictionary&lt;Interop.Error, SocketError&gt; s_nativeErrorToSocketError = new Dictionary&lt;Interop.Error, SocketError&gt;(NativeErrorToSocketErrorCount)
{
    { Interop.Error.EACCES, SocketError.AccessDenied },
    { Interop.Error.EADDRINUSE, SocketError.AddressAlreadyInUse },
    { Interop.Error.EADDRNOTAVAIL, SocketError.AddressNotAvailable },
    { Interop.Error.EAFNOSUPPORT, SocketError.AddressFamilyNotSupported },
    { Interop.Error.EAGAIN, SocketError.WouldBlock },
    { Interop.Error.EALREADY, SocketError.AlreadyInProgress },
    { Interop.Error.EBADF, SocketError.OperationAborted },
    { Interop.Error.ECANCELED, SocketError.OperationAborted },
    { Interop.Error.ECONNABORTED, SocketError.ConnectionAborted },
    { Interop.Error.ECONNREFUSED, SocketError.ConnectionRefused },
    { Interop.Error.ECONNRESET, SocketError.ConnectionReset },
    { Interop.Error.EDESTADDRREQ, SocketError.DestinationAddressRequired },
    { Interop.Error.EFAULT, SocketError.Fault },
    { Interop.Error.EHOSTDOWN, SocketError.HostDown },
    { Interop.Error.ENXIO, SocketError.HostNotFound }, // not perfect, but closest match available
    { Interop.Error.EHOSTUNREACH, SocketError.HostUnreachable },
    { Interop.Error.EINPROGRESS, SocketError.InProgress },
    { Interop.Error.EINTR, SocketError.Interrupted },
    { Interop.Error.EINVAL, SocketError.InvalidArgument },
    { Interop.Error.EISCONN, SocketError.IsConnected },
    { Interop.Error.EMFILE, SocketError.TooManyOpenSockets },
    { Interop.Error.EMSGSIZE, SocketError.MessageSize },
    { Interop.Error.ENETDOWN, SocketError.NetworkDown },
    { Interop.Error.ENETRESET, SocketError.NetworkReset },
    { Interop.Error.ENETUNREACH, SocketError.NetworkUnreachable },
    { Interop.Error.ENFILE, SocketError.TooManyOpenSockets },
    { Interop.Error.ENOBUFS, SocketError.NoBufferSpaceAvailable },
    { Interop.Error.ENODATA, SocketError.NoData },
    { Interop.Error.ENOENT, SocketError.AddressNotAvailable },
    { Interop.Error.ENOPROTOOPT, SocketError.ProtocolOption },
    { Interop.Error.ENOTCONN, SocketError.NotConnected },
    { Interop.Error.ENOTSOCK, SocketError.NotSocket },
    { Interop.Error.ENOTSUP, SocketError.OperationNotSupported },
    { Interop.Error.EPERM, SocketError.AccessDenied },
    { Interop.Error.EPIPE, SocketError.Shutdown },
    { Interop.Error.EPFNOSUPPORT, SocketError.ProtocolFamilyNotSupported },
    { Interop.Error.EPROTONOSUPPORT, SocketError.ProtocolNotSupported },
    { Interop.Error.EPROTOTYPE, SocketError.ProtocolType },
    { Interop.Error.ESOCKTNOSUPPORT, SocketError.SocketNotSupported },
    { Interop.Error.ESHUTDOWN, SocketError.Disconnecting },
    { Interop.Error.SUCCESS, SocketError.Success },
    { Interop.Error.ETIMEDOUT, SocketError.TimedOut },
};

private static readonly Dictionary&lt;SocketError, Interop.Error&gt; s_socketErrorToNativeError = new Dictionary&lt;SocketError, Interop.Error&gt;(SocketErrorToNativeErrorCount)
{
    // This is *mostly* an inverse mapping of s_nativeErrorToSocketError.  However, some options have multiple mappings and thus
    // can't be inverted directly.  Other options don't have a mapping from native to SocketError, but when presented with a SocketError,
    // we want to provide the closest relevant Error possible, e.g. EINPROGRESS maps to SocketError.InProgress, and vice versa, but
    // SocketError.IOPending also maps closest to EINPROGRESS.  As such, roundtripping won't necessarily provide the original value 100% of the time,
    // but it's the best we can do given the mismatch between Interop.Error and SocketError.

    { SocketError.AccessDenied, Interop.Error.EACCES}, // could also have been EPERM
    { SocketError.AddressAlreadyInUse, Interop.Error.EADDRINUSE  },
    { SocketError.AddressNotAvailable, Interop.Error.EADDRNOTAVAIL },
    { SocketError.AddressFamilyNotSupported, Interop.Error.EAFNOSUPPORT  },
    { SocketError.AlreadyInProgress, Interop.Error.EALREADY },
    { SocketError.ConnectionAborted, Interop.Error.ECONNABORTED },
    { SocketError.ConnectionRefused, Interop.Error.ECONNREFUSED },
    { SocketError.ConnectionReset, Interop.Error.ECONNRESET },
    { SocketError.DestinationAddressRequired, Interop.Error.EDESTADDRREQ },
    { SocketError.Disconnecting, Interop.Error.ESHUTDOWN },
    { SocketError.Fault, Interop.Error.EFAULT },
    { SocketError.HostDown, Interop.Error.EHOSTDOWN },
    { SocketError.HostNotFound, Interop.Error.EHOSTNOTFOUND },
    { SocketError.HostUnreachable, Interop.Error.EHOSTUNREACH },
    { SocketError.InProgress, Interop.Error.EINPROGRESS },
    { SocketError.Interrupted, Interop.Error.EINTR },
    { SocketError.InvalidArgument, Interop.Error.EINVAL },
    { SocketError.IOPending, Interop.Error.EINPROGRESS },
    { SocketError.IsConnected, Interop.Error.EISCONN },
    { SocketError.MessageSize, Interop.Error.EMSGSIZE },
    { SocketError.NetworkDown, Interop.Error.ENETDOWN },
    { SocketError.NetworkReset, Interop.Error.ENETRESET },
    { SocketError.NetworkUnreachable, Interop.Error.ENETUNREACH },
    { SocketError.NoBufferSpaceAvailable, Interop.Error.ENOBUFS },
    { SocketError.NoData, Interop.Error.ENODATA },
    { SocketError.NotConnected, Interop.Error.ENOTCONN },
    { SocketError.NotSocket, Interop.Error.ENOTSOCK },
    { SocketError.OperationAborted, Interop.Error.ECANCELED },
    { SocketError.OperationNotSupported, Interop.Error.ENOTSUP },
    { SocketError.ProtocolFamilyNotSupported, Interop.Error.EPFNOSUPPORT },
    { SocketError.ProtocolNotSupported, Interop.Error.EPROTONOSUPPORT },
    { SocketError.ProtocolOption, Interop.Error.ENOPROTOOPT },
    { SocketError.ProtocolType, Interop.Error.EPROTOTYPE },
    { SocketError.Shutdown, Interop.Error.EPIPE },
    { SocketError.SocketNotSupported, Interop.Error.ESOCKTNOSUPPORT },
    { SocketError.Success, Interop.Error.SUCCESS },
    { SocketError.TimedOut, Interop.Error.ETIMEDOUT },
    { SocketError.TooManyOpenSockets, Interop.Error.ENFILE }, // could also have been EMFILE
    { SocketError.TryAgain, Interop.Error.EAGAIN }, // not a perfect mapping, but better than nothing
    { SocketError.WouldBlock, Interop.Error.EAGAIN  },
};

internal static bool TryGetNativeErrorForSocketError(SocketError error, out Interop.Error errno)
{
    return s_socketErrorToNativeError.TryGetValue(error, out errno);
}
```

Once we have an instance of <code>Interop.Error</code>, we call <code>interopErr.Info().RawErrno</code>. The implementation of RawErrno can be found in <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/Common/src/CoreLib/Interop/Unix/Interop.Errors.cs#L138">Interop.Errors.cs</a>:

```cs
internal int RawErrno
{
    get { return _rawErrno == -1 ? (_rawErrno = Interop.Sys.ConvertErrorPalToPlatform(_error)) : _rawErrno; }
}

[DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ConvertErrorPalToPlatform")]
internal static extern int ConvertErrorPalToPlatform(Error error);
```

Here we are jumping to the native function <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/Native/Unix/System.Native/pal_errno.c#L210">SystemNative_ConvertErrorPalToPlatform</a> that maps Error to the native integer code that is defined in <a href="https://en.wikipedia.org/wiki/Errno.h">errno.h</a>. You can get all the values using the <a href="http://man7.org/linux/man-pages/man3/errno.3.html">errno</a> util. Here is a typical output on Linux:

```txt
$ errno -ls
EPERM 1 Operation not permitted
ENOENT 2 No such file or directory
ESRCH 3 No such process
EINTR 4 Interrupted system call
EIO 5 Input/output error
ENXIO 6 No such device or address
E2BIG 7 Argument list too long
ENOEXEC 8 Exec format error
EBADF 9 Bad file descriptor
ECHILD 10 No child processes
EAGAIN 11 Resource temporarily unavailable
ENOMEM 12 Cannot allocate memory
EACCES 13 Permission denied
EFAULT 14 Bad address
ENOTBLK 15 Block device required
EBUSY 16 Device or resource busy
EEXIST 17 File exists
EXDEV 18 Invalid cross-device link
ENODEV 19 No such device
ENOTDIR 20 Not a directory
EISDIR 21 Is a directory
EINVAL 22 Invalid argument
ENFILE 23 Too many open files in system
EMFILE 24 Too many open files
ENOTTY 25 Inappropriate ioctl for device
ETXTBSY 26 Text file busy
EFBIG 27 File too large
ENOSPC 28 No space left on device
ESPIPE 29 Illegal seek
EROFS 30 Read-only file system
EMLINK 31 Too many links
EPIPE 32 Broken pipe
EDOM 33 Numerical argument out of domain
ERANGE 34 Numerical result out of range
EDEADLK 35 Resource deadlock avoided
ENAMETOOLONG 36 File name too long
ENOLCK 37 No locks available
ENOSYS 38 Function not implemented
ENOTEMPTY 39 Directory not empty
ELOOP 40 Too many levels of symbolic links
EWOULDBLOCK 11 Resource temporarily unavailable
ENOMSG 42 No message of desired type
EIDRM 43 Identifier removed
ECHRNG 44 Channel number out of range
EL2NSYNC 45 Level 2 not synchronized
EL3HLT 46 Level 3 halted
EL3RST 47 Level 3 reset
ELNRNG 48 Link number out of range
EUNATCH 49 Protocol driver not attached
ENOCSI 50 No CSI structure available
EL2HLT 51 Level 2 halted
EBADE 52 Invalid exchange
EBADR 53 Invalid request descriptor
EXFULL 54 Exchange full
ENOANO 55 No anode
EBADRQC 56 Invalid request code
EBADSLT 57 Invalid slot
EDEADLOCK 35 Resource deadlock avoided
EBFONT 59 Bad font file format
ENOSTR 60 Device not a stream
ENODATA 61 No data available
ETIME 62 Timer expired
ENOSR 63 Out of streams resources
ENONET 64 Machine is not on the network
ENOPKG 65 Package not installed
EREMOTE 66 Object is remote
ENOLINK 67 Link has been severed
EADV 68 Advertise error
ESRMNT 69 Srmount error
ECOMM 70 Communication error on send
EPROTO 71 Protocol error
EMULTIHOP 72 Multihop attempted
EDOTDOT 73 RFS specific error
EBADMSG 74 Bad message
EOVERFLOW 75 Value too large for defined data type
ENOTUNIQ 76 Name not unique on network
EBADFD 77 File descriptor in bad state
EREMCHG 78 Remote address changed
ELIBACC 79 Can not access a needed shared library
ELIBBAD 80 Accessing a corrupted shared library
ELIBSCN 81 .lib section in a.out corrupted
ELIBMAX 82 Attempting to link in too many shared libraries
ELIBEXEC 83 Cannot exec a shared library directly
EILSEQ 84 Invalid or incomplete multibyte or wide character
ERESTART 85 Interrupted system call should be restarted
ESTRPIPE 86 Streams pipe error
EUSERS 87 Too many users
ENOTSOCK 88 Socket operation on non-socket
EDESTADDRREQ 89 Destination address required
EMSGSIZE 90 Message too long
EPROTOTYPE 91 Protocol wrong type for socket
ENOPROTOOPT 92 Protocol not available
EPROTONOSUPPORT 93 Protocol not supported
ESOCKTNOSUPPORT 94 Socket type not supported
EOPNOTSUPP 95 Operation not supported
EPFNOSUPPORT 96 Protocol family not supported
EAFNOSUPPORT 97 Address family not supported by protocol
EADDRINUSE 98 Address already in use
EADDRNOTAVAIL 99 Cannot assign requested address
ENETDOWN 100 Network is down
ENETUNREACH 101 Network is unreachable
ENETRESET 102 Network dropped connection on reset
ECONNABORTED 103 Software caused connection abort
ECONNRESET 104 Connection reset by peer
ENOBUFS 105 No buffer space available
EISCONN 106 Transport endpoint is already connected
ENOTCONN 107 Transport endpoint is not connected
ESHUTDOWN 108 Cannot send after transport endpoint shutdown
ETOOMANYREFS 109 Too many references: cannot splice
ETIMEDOUT 110 Connection timed out
ECONNREFUSED 111 Connection refused
EHOSTDOWN 112 Host is down
EHOSTUNREACH 113 No route to host
EALREADY 114 Operation already in progress
EINPROGRESS 115 Operation now in progress
ESTALE 116 Stale file handle
EUCLEAN 117 Structure needs cleaning
ENOTNAM 118 Not a XENIX named type file
ENAVAIL 119 No XENIX semaphores available
EISNAM 120 Is a named type file
EREMOTEIO 121 Remote I/O error
EDQUOT 122 Disk quota exceeded
ENOMEDIUM 123 No medium found
EMEDIUMTYPE 124 Wrong medium type
ECANCELED 125 Operation canceled
ENOKEY 126 Required key not available
EKEYEXPIRED 127 Key has expired
EKEYREVOKED 128 Key has been revoked
EKEYREJECTED 129 Key was rejected by service
EOWNERDEAD 130 Owner died
ENOTRECOVERABLE 131 State not recoverable
ERFKILL 132 Operation not possible due to RF-kill
EHWPOISON 133 Memory page has hardware error
ENOTSUP 95 Operation not supported
```

Note that <code>errno</code> may be not available by default in your Linux distro. For example, on Debian, you <a href="https://unix.stackexchange.com/questions/326766/what-are-the-standard-error-codes-in-linux#comment688800_326811">should call</a> <strong><code>sudo apt-get install moreutils</code></strong> to get this utility.
Here is a typical output on macOS:

```txt
$ errno -ls
EPERM 1 Operation not permitted
ENOENT 2 No such file or directory
ESRCH 3 No such process
EINTR 4 Interrupted system call
EIO 5 Input/output error
ENXIO 6 Device not configured
E2BIG 7 Argument list too long
ENOEXEC 8 Exec format error
EBADF 9 Bad file descriptor
ECHILD 10 No child processes
EDEADLK 11 Resource deadlock avoided
ENOMEM 12 Cannot allocate memory
EACCES 13 Permission denied
EFAULT 14 Bad address
ENOTBLK 15 Block device required
EBUSY 16 Resource busy
EEXIST 17 File exists
EXDEV 18 Cross-device link
ENODEV 19 Operation not supported by device
ENOTDIR 20 Not a directory
EISDIR 21 Is a directory
EINVAL 22 Invalid argument
ENFILE 23 Too many open files in system
EMFILE 24 Too many open files
ENOTTY 25 Inappropriate ioctl for device
ETXTBSY 26 Text file busy
EFBIG 27 File too large
ENOSPC 28 No space left on device
ESPIPE 29 Illegal seek
EROFS 30 Read-only file system
EMLINK 31 Too many links
EPIPE 32 Broken pipe
EDOM 33 Numerical argument out of domain
ERANGE 34 Result too large
EAGAIN 35 Resource temporarily unavailable
EWOULDBLOCK 35 Resource temporarily unavailable
EINPROGRESS 36 Operation now in progress
EALREADY 37 Operation already in progress
ENOTSOCK 38 Socket operation on non-socket
EDESTADDRREQ 39 Destination address required
EMSGSIZE 40 Message too long
EPROTOTYPE 41 Protocol wrong type for socket
ENOPROTOOPT 42 Protocol not available
EPROTONOSUPPORT 43 Protocol not supported
ESOCKTNOSUPPORT 44 Socket type not supported
ENOTSUP 45 Operation not supported
EPFNOSUPPORT 46 Protocol family not supported
EAFNOSUPPORT 47 Address family not supported by protocol family
EADDRINUSE 48 Address already in use
EADDRNOTAVAIL 49 Can`t assign requested address
ENETDOWN 50 Network is down
ENETUNREACH 51 Network is unreachable
ENETRESET 52 Network dropped connection on reset
ECONNABORTED 53 Software caused connection abort
ECONNRESET 54 Connection reset by peer
ENOBUFS 55 No buffer space available
EISCONN 56 Socket is already connected
ENOTCONN 57 Socket is not connected
ESHUTDOWN 58 Can`t send after socket shutdown
ETOOMANYREFS 59 Too many references: can`t splice
ETIMEDOUT 60 Operation timed out
ECONNREFUSED 61 Connection refused
ELOOP 62 Too many levels of symbolic links
ENAMETOOLONG 63 File name too long
EHOSTDOWN 64 Host is down
EHOSTUNREACH 65 No route to host
ENOTEMPTY 66 Directory not empty
EPROCLIM 67 Too many processes
EUSERS 68 Too many users
EDQUOT 69 Disc quota exceeded
ESTALE 70 Stale NFS file handle
EREMOTE 71 Too many levels of remote in path
EBADRPC 72 RPC struct is bad
ERPCMISMATCH 73 RPC version wrong
EPROGUNAVAIL 74 RPC prog. not avail
EPROGMISMATCH 75 Program version wrong
EPROCUNAVAIL 76 Bad procedure for program
ENOLCK 77 No locks available
ENOSYS 78 Function not implemented
EFTYPE 79 Inappropriate file type or format
EAUTH 80 Authentication error
ENEEDAUTH 81 Need authenticator
EPWROFF 82 Device power is off
EDEVERR 83 Device error
EOVERFLOW 84 Value too large to be stored in data type
EBADEXEC 85 Bad executable (or shared library)
EBADARCH 86 Bad CPU type in executable
ESHLIBVERS 87 Shared library version mismatch
EBADMACHO 88 Malformed Mach-o file
ECANCELED 89 Operation canceled
EIDRM 90 Identifier removed
ENOMSG 91 No message of desired type
EILSEQ 92 Illegal byte sequence
ENOATTR 93 Attribute not found
EBADMSG 94 Bad message
EMULTIHOP 95 EMULTIHOP (Reserved)
ENODATA 96 No message available on STREAM
ENOLINK 97 ENOLINK (Reserved)
ENOSR 98 No STREAM resources
ENOSTR 99 Not a STREAM
EPROTO 100 Protocol error
ETIME 101 STREAM ioctl timeout
EOPNOTSUPP 102 Operation not supported on socket
ENOPOLICY 103 Policy not found
ENOTRECOVERABLE 104 State not recoverable
EOWNERDEAD 105 Previous owner died
EQFULL 106 Interface output queue is full
ELAST 106 Interface output queue is full
```

Hooray! We’ve finished our fascinating journey into the internals of socket error codes. Now you know where .NET is getting the native error code for each <code>SocketException</code> from!
<h2><strong>ErrorCode</strong></h2>
The <code>ErrorCode</code> property is the most boring one, as it always returns <code>NativeErrorCode</code>.
<a href="https://referencesource.microsoft.com/#System/net/System/Net/SocketException.cs,67">.NET Framework</a>, <a href="https://github.com/mono/mono/blob/mono-6.8.0.105/mcs/class/referencesource/System/net/System/Net/SocketException.cs#L79">Mono 6.8.0.105</a>:

```cs
public override int ErrorCode {
    //
    // the base class returns the HResult with this property
    // we need the Win32 Error Code, hence the override.
    //
    get {
        return NativeErrorCode;
    }
}
```

In <a href="https://github.com/dotnet/corefx/blob/v3.1.3/src/System.Net.Primitives/src/System/Net/SocketException.cs#L51">.NET Core 3.1.3</a>:

```cs
public override int ErrorCode =&gt; base.NativeErrorCode;
```

<h2><strong>Writing cross-platform socket error handling</strong></h2>
Circling back to the original method we started this post with, we rewrote ShouldRetryConnection as follows:

```cs
protected virtual bool ShouldRetryConnection(Exception ex)
{
    if (ex is SocketException sx)
        return sx.SocketErrorCode == SocketError.ConnectionRefused;
    return false;
}
```

There was a lot of work involved in tracking down the error code to check against, but in the end, our code is much more readable now. Adding to that, this method is now also completely cross-platform, and works correctly on any runtime.
<h2><strong>Overview of the native error codes</strong></h2>
In some situations, you may want to have a table with native error codes on different operating systems. We can get these values with the following code snippet:

```cs
var allErrors = Enum.GetValues(typeof(SocketError)).Cast&lt;SocketError&gt;().ToList();
var maxNameWidth = allErrors.Select(x =&gt; x.ToString().Length).Max();
foreach (var socketError in allErrors)
{
    var name = socketError.ToString().PadRight(maxNameWidth);
    var code = new SocketException((int) socketError).NativeErrorCode.ToString().PadLeft(7);
    Console.WriteLine("| {name} | {code} |");
}
```

We executed this program on Windows, Linux, and macOS. Here are the aggregated results:
<table>
<tbody>
<tr>
<td><strong>SocketError</strong></td>
<td><strong>Windows</strong></td>
<td><strong>Linux</strong></td>
<td><strong>macOS</strong></td>
</tr>
<tr>
<td>Success</td>
<td>0</td>
<td>0</td>
<td>0</td>
</tr>
<tr>
<td>OperationAborted</td>
<td>995</td>
<td>125</td>
<td>89</td>
</tr>
<tr>
<td>IOPending</td>
<td>997</td>
<td>115</td>
<td>36</td>
</tr>
<tr>
<td>Interrupted</td>
<td>10004</td>
<td>4</td>
<td>4</td>
</tr>
<tr>
<td>AccessDenied</td>
<td>10013</td>
<td>13</td>
<td>13</td>
</tr>
<tr>
<td>Fault</td>
<td>10014</td>
<td>14</td>
<td>14</td>
</tr>
<tr>
<td>InvalidArgument</td>
<td>10022</td>
<td>22</td>
<td>22</td>
</tr>
<tr>
<td>TooManyOpenSockets</td>
<td>10024</td>
<td>23</td>
<td>23</td>
</tr>
<tr>
<td>WouldBlock</td>
<td>10035</td>
<td>11</td>
<td>35</td>
</tr>
<tr>
<td>InProgress</td>
<td>10036</td>
<td>115</td>
<td>36</td>
</tr>
<tr>
<td>AlreadyInProgress</td>
<td>10037</td>
<td>114</td>
<td>37</td>
</tr>
<tr>
<td>NotSocket</td>
<td>10038</td>
<td>88</td>
<td>38</td>
</tr>
<tr>
<td>DestinationAddressRequired</td>
<td>10039</td>
<td>89</td>
<td>39</td>
</tr>
<tr>
<td>MessageSize</td>
<td>10040</td>
<td>90</td>
<td>40</td>
</tr>
<tr>
<td>ProtocolType</td>
<td>10041</td>
<td>91</td>
<td>41</td>
</tr>
<tr>
<td>ProtocolOption</td>
<td>10042</td>
<td>92</td>
<td>42</td>
</tr>
<tr>
<td>ProtocolNotSupported</td>
<td>10043</td>
<td>93</td>
<td>43</td>
</tr>
<tr>
<td>SocketNotSupported</td>
<td>10044</td>
<td>94</td>
<td>44</td>
</tr>
<tr>
<td>OperationNotSupported</td>
<td>10045</td>
<td>95</td>
<td>45</td>
</tr>
<tr>
<td>ProtocolFamilyNotSupported</td>
<td>10046</td>
<td>96</td>
<td>46</td>
</tr>
<tr>
<td>AddressFamilyNotSupported</td>
<td>10047</td>
<td>97</td>
<td>47</td>
</tr>
<tr>
<td>AddressAlreadyInUse</td>
<td>10048</td>
<td>98</td>
<td>48</td>
</tr>
<tr>
<td>AddressNotAvailable</td>
<td>10049</td>
<td>99</td>
<td>49</td>
</tr>
<tr>
<td>NetworkDown</td>
<td>10050</td>
<td>100</td>
<td>50</td>
</tr>
<tr>
<td>NetworkUnreachable</td>
<td>10051</td>
<td>101</td>
<td>51</td>
</tr>
<tr>
<td>NetworkReset</td>
<td>10052</td>
<td>102</td>
<td>52</td>
</tr>
<tr>
<td>ConnectionAborted</td>
<td>10053</td>
<td>103</td>
<td>53</td>
</tr>
<tr>
<td>ConnectionReset</td>
<td>10054</td>
<td>104</td>
<td>54</td>
</tr>
<tr>
<td>NoBufferSpaceAvailable</td>
<td>10055</td>
<td>105</td>
<td>55</td>
</tr>
<tr>
<td>IsConnected</td>
<td>10056</td>
<td>106</td>
<td>56</td>
</tr>
<tr>
<td>NotConnected</td>
<td>10057</td>
<td>107</td>
<td>57</td>
</tr>
<tr>
<td>Shutdown</td>
<td>10058</td>
<td>32</td>
<td>32</td>
</tr>
<tr>
<td>TimedOut</td>
<td>10060</td>
<td>110</td>
<td>60</td>
</tr>
<tr>
<td>ConnectionRefused</td>
<td>10061</td>
<td>111</td>
<td>61</td>
</tr>
<tr>
<td>HostDown</td>
<td>10064</td>
<td>112</td>
<td>64</td>
</tr>
<tr>
<td>HostUnreachable</td>
<td>10065</td>
<td>113</td>
<td>65</td>
</tr>
<tr>
<td>ProcessLimit</td>
<td>10067</td>
<td>10067</td>
<td>10067</td>
</tr>
<tr>
<td>SystemNotReady</td>
<td>10091</td>
<td>10091</td>
<td>10091</td>
</tr>
<tr>
<td>VersionNotSupported</td>
<td>10092</td>
<td>10092</td>
<td>10092</td>
</tr>
<tr>
<td>NotInitialized</td>
<td>10093</td>
<td>10093</td>
<td>10093</td>
</tr>
<tr>
<td>Disconnecting</td>
<td>10101</td>
<td>108</td>
<td>58</td>
</tr>
<tr>
<td>TypeNotFound</td>
<td>10109</td>
<td>10109</td>
<td>10109</td>
</tr>
<tr>
<td>HostNotFound</td>
<td>11001</td>
<td>-131073</td>
<td>-131073</td>
</tr>
<tr>
<td>TryAgain</td>
<td>11002</td>
<td>11</td>
<td>35</td>
</tr>
<tr>
<td>NoRecovery</td>
<td>11003</td>
<td>11003</td>
<td>11003</td>
</tr>
<tr>
<td>NoData</td>
<td>11004</td>
<td>61</td>
<td>96</td>
</tr>
<tr>
<td>SocketError</td>
<td>-1</td>
<td>-1</td>
<td>-1</td>
</tr>
</tbody>
</table>
This table may be useful if you work with native socket error codes.
<h2><strong>Summary</strong></h2>
From this investigation, we’ve learned the following:
<ul>
	<li><code>SocketException.SocketErrorCode</code> returns a value from the <code>SocketError</code> enum. The numerical values of the enum elements always correspond to the <a href="https://docs.microsoft.com/en-us/windows/win32/winsock/">Windows socket error codes</a>.</li>
	<li><code>SocketException.ErrorCode</code> always returns <code>SocketException.NativeErrorCode</code>.</li>
	<li><code>SocketException.NativeErrorCode</code> on .NET Framework and Mono always corresponds to the Windows error codes (even if you are using Mono on Unix). On .NET Core, <code>SocketException.NativeErrorCode</code> equals the corresponding native error code from the current operating system.</li>
</ul>

{{< img dotnet-SocketErrors-Blog >}}

A few practical recommendations:
<ul>
	<li>If you want to write portable code, always use <code>SocketException.SocketErrorCode</code> and compare it with the values of <code>SocketError</code>. Never use raw numerical error codes.</li>
	<li>If you want to get the native error code on .NET Core (e.g., for passing to another native program), use <code>SocketException.NativeErrorCode</code>. Remember that different Unix-based operating systems (e.g., Linux, macOS, Solaris) have different native code sets. You can get the exact values of the native error codes by using the errno command.</li>
</ul>
<h2><strong>References</strong></h2>
<ul>
	<li><a href="https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2">Microsoft Docs: Windows Sockets Error Codes</a></li>
	<li><a href="https://www.ibm.com/support/knowledgecenter/SSEPGG_11.1.0/com.ibm.db2.luw.messages.doc/doc/r0058740.html">IBM Knowledge Center: TCP/IP error codes</a></li>
	<li><a href="https://mariadb.com/kb/en/operating-system-error-codes/">MariaDB: Operating System Error Codes</a></li>
	<li><a href="https://www.gnu.org/software/libc/manual/html_node/Error-Codes.html">gnu.org: Error Codes</a></li>
	<li><a href="https://stackoverflow.com/q/961465/184842">Stackoverflow: Identical Error Codes</a></li>
</ul>