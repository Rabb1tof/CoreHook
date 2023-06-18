using CoreHook.BinaryInjection;
using CoreHook.BinaryInjection.IPC;
using CoreHook.IPC.NamedPipes;

using Microsoft.Extensions.Logging;

using Moq;

using System;
using System.Threading.Tasks;

using Xunit;

namespace CoreHook.Tests;

public class InjectionHelperTest
{
    private readonly int _targetProcessId = Environment.ProcessId;
    private readonly ILogger logger = Mock.Of<ILogger>();

    [Fact]
    public async void InjectionHelperCompleted()
    {
        var injectionHelperPipeName = "InjectionHelperPipeTest";
        using var injectionHelper = new InjectionHelper(injectionHelperPipeName, PipePlatformBase.Instance, logger);

        injectionHelper.BeginInjection(_targetProcessId);

        bool injectionComplete;
        try
        {
            await SendInjectionComplete(injectionHelperPipeName, _targetProcessId);

            injectionHelper.WaitForInjection(_targetProcessId);
        }
        finally
        {
            injectionHelper.InjectionCompleted(_targetProcessId);

            injectionComplete = true;
        }

        Assert.True(injectionComplete);
    }

    [Fact]
    public void InjectionHelperDidNotComplete()
    {
        using var injectionHelper = new InjectionHelper("InjectionHelperFailedPipeTest", PipePlatformBase.Instance, logger);
        
        injectionHelper.BeginInjection(_targetProcessId);
        
        try
        {
            Assert.Throws<TimeoutException>(() => injectionHelper.WaitForInjection(_targetProcessId, 500));
        }
        finally
        {
            injectionHelper.InjectionCompleted(_targetProcessId);
        }

    }

    private static async Task<bool> SendInjectionComplete(string pipeName, int pid)
    {
        using var pipeClient = new NamedPipeClient(pipeName, true);

        try
        {
            return await pipeClient.TryWrite(new InjectionCompleteMessage(pid, true));
        }
        catch
        {
            return false;
        }
    }
}
