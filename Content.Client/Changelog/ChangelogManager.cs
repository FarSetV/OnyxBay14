using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;


namespace Content.Client.Changelog
{
    public sealed class ChangelogManager
    {
        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        public bool NewChangelogEntries { get; private set; }
        public int LastReadId { get; private set; }
        public int MaxId { get; private set; }

        public event Action? NewChangelogEntriesChanged;

        /// <summary>
        ///     Ran when the user opens ("read") the changelog,
        ///     stores the new ID to disk and clears <see cref="NewChangelogEntries"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="LastReadId"/> is NOT cleared
        ///     since that's used in the changelog menu to show the "since you last read" bar.
        /// </remarks>
        public void SaveNewReadId()
        {
            NewChangelogEntries = false;
            NewChangelogEntriesChanged?.Invoke();

            using var sw = _resource.UserData.OpenWriteText(new ResourcePath($"/changelog_last_seen_{_configManager.GetCVar(CCVars.ServerId)}"));

            sw.Write(MaxId.ToString());
        }

        public async void Initialize()
        {
            // Open changelog purely to compare to the last viewed date.
            var changelog = await LoadChangelog();

            if (changelog.Count == 0)
            {
                return;
            }

            MaxId = changelog.Max(c => c.Id);

            var path = new ResourcePath($"/changelog_last_seen_{_configManager.GetCVar(CCVars.ServerId)}");
            if(_resource.UserData.TryReadAllText(path, out var lastReadIdText))
            {
                LastReadId = int.Parse(lastReadIdText);
            }

            NewChangelogEntries = LastReadId < MaxId;

            NewChangelogEntriesChanged?.Invoke();
        }

        public Task<List<ChangelogEntry>> LoadChangelog()
        {
            return Task.Run(() =>
            {
                var yamlData = _resource.ContentFileReadYaml(new ResourcePath("/Changelog/Changelog.yml"));

                if (yamlData.Documents.Count == 0)
                    return new List<ChangelogEntry>();

                var node = (MappingDataNode)yamlData.Documents[0].RootNode.ToDataNode();
                return _serialization.Read<List<ChangelogEntry>>(node["entries"]);
            });
        }

        [DataDefinition]
        public sealed class ChangelogEntry : ISerializationHooks
        {
            [DataField("id")]
            public int Id { get; set; }

            [DataField("author")]
            public string Author { get; set; } = "";

            [DataField("time")] private string _time = default!;

            public DateTime Time { get; set; }

            [DataField("changes")]
            public List<ChangelogChange> Changes { get; set; } = default!;

            void ISerializationHooks.AfterDeserialization()
            {
                Time = DateTime.Parse(_time, null, DateTimeStyles.RoundtripKind);
            }
        }

        [DataDefinition]
        public sealed class ChangelogChange : ISerializationHooks
        {
            [DataField("type")]
            public ChangelogLineType Type { get; set; }

            [DataField("message")]
            public string Message { get; set; } = "";
        }

        public enum ChangelogLineType
        {
            Add,
            Remove,
            Fix,
            Tweak,
        }
    }
}
