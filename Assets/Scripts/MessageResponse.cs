using Newtonsoft.Json;

public class MessageResponse
{
    [JsonProperty("anchorId")] public string AnchorId { get; set; }

    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("message")] public string Message { get; set; }
}