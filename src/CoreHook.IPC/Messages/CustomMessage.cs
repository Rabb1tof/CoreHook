using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreHook.IPC.Messages;

public abstract record CustomMessage(string SenderId)
{
    [JsonPropertyName("$type")]
    public virtual string TypeName => this.GetType().AssemblyQualifiedName!;

    public virtual Guid MessageId { get; } = Guid.NewGuid();

    //public virtual string SenderId { get; set; }

    public CustomMessage() : this(String.Empty) { }

    /// <summary>
    /// Serialize the message's properties and data to a string.
    /// </summary>
    /// <returns>The message's properties and data in a string format.</returns>
    public virtual string? Serialize()
    {
        // Have to use the overload to specify the actual type, for some reason (I'd have thought JsonSerializer would be able to figure this out on its own).
        return JsonSerializer.Serialize(this, this.GetType());
    }

    public static CustomMessage? Deserialize(string body)
    {
        var json = JsonDocument.Parse(body);
        var type = Type.GetType(json.RootElement.GetProperty("$type").GetString(), false);

        if (type is null)
        {
            return null;
        }

        return (CustomMessage?)json.Deserialize(type);
    }
}
