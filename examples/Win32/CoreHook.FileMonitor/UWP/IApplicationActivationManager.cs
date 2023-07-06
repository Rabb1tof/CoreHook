using System;
using System.Runtime.InteropServices;

namespace CoreHook.FileMonitor.Uwp;

[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IApplicationActivationManager
{
    // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
    // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
    nint ActivateApplication([In] string appUserModelId, [In] string arguments, [In] ActivateOptions options, [Out] out uint processId);

    nint ActivateForFile([In] string appUserModelId, [In] nint /*IShellItemArray* */ itemArray, [In] string verb, [Out] out uint processId);

    nint ActivateForProtocol([In] string appUserModelId, [In] nint /* IShellItemArray* */itemArray, [Out] out uint processId);
}
