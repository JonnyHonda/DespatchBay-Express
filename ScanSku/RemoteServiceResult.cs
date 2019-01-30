namespace DespatchBayExpress
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class RemoteServiceResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("attempt")]
        public Guid Attempt { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("request_id")]
        public Guid RequestId { get; set; }
    }
}
