using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;

using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CoreHook.HookDefinition;
public class HookBase
{
    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddSimpleConsole(conf => conf.SingleLine = true));

    protected NamedPipeClient _pipe;

    // The number of arguments in the constructor and Run method must be equal to the number passed during injection in the FileMonitor application.
    public HookBase(string pipeName)
    {
        //_pipe = new NamedPipeClient(pipeName, true);
    }

    public void Run(string pipeName)
    {
        try
        {
            _pipe = new NamedPipeClient(pipeName, loggerFactory.CreateLogger(this.GetType()), true);

            _ = _pipe.TryWrite(new LogMessage("Hook pipe is connected, creating hooks."));

            CreateHooks().Wait();

            _ = _pipe.TryWrite(new LogMessage("Success!"));
        }
        catch (Exception e)
        {
            _ = _pipe?.TryWrite(new LogMessage(e.Message, LogLevel.Error));
        }
    }

    private async Task CreateHooks()
    {
        var hookMethods = this.GetType().GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(method => method.GetCustomAttributes<HookAttribute>()?.Any() ?? false)
                                        .ToList();

        await _pipe.TryWrite(new LogMessage($"{hookMethods.Count} hook methods found."));

        foreach (var hookMethod in hookMethods)
        {
            var attr = hookMethod.GetCustomAttributes<HookAttribute>().First();
            var _createFileHook = LocalHook.Create(attr.TargetDllName, attr.TargetMethod, hookMethod.CreateDelegate(attr.DelegateType, this), this);

            await _pipe.TryWrite(new LogMessage($"Success, mapped {attr.TargetDllName}!{attr.TargetMethod} from 0x{_createFileHook.OriginalAddress:x} to 0x{_createFileHook.TargetAddress:x}."));
        }
    }

    private Delegate CreateDelegate(MethodInfo hookMethod)
    {
        var parameters = hookMethod.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();//.Append(hookMethod.ReturnType).ToArray();
        var call = Expression.Convert(Expression.Call(Expression.Constant(this), hookMethod, parameters), hookMethod.ReturnType);
        var deleg = Expression.Lambda(call, parameters).Compile();

        return deleg;
    }
}
