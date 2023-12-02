using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CoreHook.Loader;

[StructLayout(LayoutKind.Sequential)]
public struct ManagedRemoteInfo
{
    public int RemoteProcessId;

    public string InjectionChannelName;

    public string UserLibrary;

    public string ClassName;

    public string MethodName;

    public object?[] UserParams;

    public string?[]? UserParamsTypeNames;

    //TODO: use a typed userParams object to avoid losing null object types?
    public ManagedRemoteInfo(int remoteProcessId, string channelName, string userLibrary, string className, string methodName, params object?[] userParams)
    {
        InjectionChannelName = channelName;
        UserLibrary = userLibrary;
        RemoteProcessId = remoteProcessId;
        ClassName = className;
        MethodName = methodName;
        UserParams = userParams;
        UserParamsTypeNames = userParams?.Select(param => param?.GetType().AssemblyQualifiedName).ToArray();
    }
}
