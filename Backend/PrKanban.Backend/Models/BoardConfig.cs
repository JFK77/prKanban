using System.Text.Json.Serialization;

namespace PrKanban.Models
{
    /// <summary>
    /// Represents board layout and configuration (1:1 with User)
    /// </summary>
    public class BoardConfig
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = $"config_{Guid.NewGuid()}";

        /// <summary>
        /// User ID (foreign key, unique constraint)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Column order as JSON array
        /// </summary>
        [JsonIgnore]
        public string? ColumnOrderJson { get; set; }

        /// <summary>
        /// Sub-column mapping as JSON
        /// </summary>
        [JsonIgnore]
        public string? SubColumnMappingJson { get; set; }

        /// <summary>
        /// WIP limits as JSON
        /// </summary>
        [JsonIgnore]
        public string? WipLimitsJson { get; set; }

        /// <summary>
        /// Tags configuration as JSON
        /// </summary>
        [JsonIgnore]
        public string? TagsConfigJson { get; set; }

        /// <summary>
        /// Theme preference: "light" or "dark"
        /// </summary>
        public string Theme { get; set; } = "light";

        /// <summary>
        /// UTC timestamp when config was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when config was last modified
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        /// <summary>
        /// Reference to the User owning this configuration
        /// </summary>
        public virtual ApplicationUser? User { get; set; }

        // Non-mapped properties for API serialization
        [JsonPropertyName("columnOrder")]
        public string[] ColumnOrder
        {
            get
            {
                if (string.IsNullOrEmpty(ColumnOrderJson))
                    return Array.Empty<string>();
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<string[]>(ColumnOrderJson) ?? Array.Empty<string>();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
            set
            {
                ColumnOrderJson = System.Text.Json.JsonSerializer.Serialize(value ?? Array.Empty<string>());
            }
        }

        [JsonPropertyName("subColumnMapping")]
        public Dictionary<string, string[]> SubColumnMapping
        {
            get
            {
                if (string.IsNullOrEmpty(SubColumnMappingJson))
                    return new Dictionary<string, string[]>();
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(SubColumnMappingJson) ?? new Dictionary<string, string[]>();
                }
                catch
                {
                    return new Dictionary<string, string[]>();
                }
            }
            set
            {
                SubColumnMappingJson = System.Text.Json.JsonSerializer.Serialize(value ?? new Dictionary<string, string[]>());
            }
        }

        [JsonPropertyName("wipLimits")]
        public Dictionary<string, int> WipLimits
        {
            get
            {
                if (string.IsNullOrEmpty(WipLimitsJson))
                    return new Dictionary<string, int>();
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(WipLimitsJson) ?? new Dictionary<string, int>();
                }
                catch
                {
                    return new Dictionary<string, int>();
                }
            }
            set
            {
                WipLimitsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new Dictionary<string, int>());
            }
        }
    }
}
