using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class AnchorResponse
{
    [JsonProperty("anchorList")]
    public AnchorList[] AnchorList { get; set; }
}

public class AnchorList
{
    [JsonProperty("anchorId")]
    public string AnchorId { get; set; }

    [JsonProperty("lattitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }
}