using CoreHook.IPC.Messages;
using CoreHook.IPC.NamedPipes;

using Microsoft.Extensions.Logging;

using Moq;

using System.Threading.Tasks;

using Xunit;

namespace CoreHook.Tests;

public class NamedPipeTest
{
    private readonly ILogger logger = Mock.Of<ILogger>();

    [Fact]
    private async void ShouldConnectToServer()
    {
        string namedPipe = Resources.GetUniquePipeName();
        var request = new StringMessage("Ping");
        var response = new StringMessage("Pong");

        async void func(INamedPipe pipe, CustomMessage message)
        {
            Assert.Equal(request.Message, (message as StringMessage)?.Message);
            await pipe.TryWrite(response);
        }

        using var server = new NamedPipeServer(namedPipe, PipePlatformBase.Instance, func, logger);
        using var pipeClient = new NamedPipeClient(namedPipe, true);

        Assert.True(await pipeClient.TryWrite(request));
        Assert.Equal(response.Message, await GetMessage(pipeClient));
    }

    [Fact]
    private async void ShouldConnectToServerAndReceiveMultipleResponses()
    {
        string namedPipe = Resources.GetUniquePipeName();

        var testMessage1 = new StringMessage("TestMessage1");
        var testMessage2 = new StringMessage("TestMessage2");
        var testMessage3 = new StringMessage("TestMessage3");

        async void func(INamedPipe pipe, CustomMessage message) => await pipe.TryWrite(message);

        using var server = new NamedPipeServer(namedPipe, PipePlatformBase.Instance, func, logger);
        using var pipeClient = new NamedPipeClient(namedPipe, true);

        Assert.True(await pipeClient.TryWrite(testMessage1));
        Assert.True(await pipeClient.TryWrite(testMessage2));
        Assert.True(await pipeClient.TryWrite(testMessage3));

        Assert.Equal(testMessage1.Message, await GetMessage(pipeClient));
        Assert.Equal(testMessage2.Message, await GetMessage(pipeClient));
        Assert.Equal(testMessage3.Message, await GetMessage(pipeClient));
    }

    private static async Task<string> GetMessage(NamedPipeClient pipeClient) => (await pipeClient.Read() as StringMessage)?.Message;
}
