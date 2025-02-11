using System;
using System.Collections.Generic;
using System.IO;
using DataProcessor.Common;

namespace DataProcessor.Publications
{
    internal class PublicationEntry
    {
        public PublicationEntryType Type { get; }
        public string Key { get; }
        public PublicationLanguage OutputLanguage { get; }
        public Dictionary<string, string> Properties { get; }

        public PublicationEntry(PublicationEntryType type, string key, PublicationLanguage outputLanguage, Dictionary<string, string> properties)
        {
            Type = type;
            Key = key;
            OutputLanguage = outputLanguage;
            Properties = properties;
        }

        public static PublicationEntry Read(PublicationLanguage outputLanguage, StreamReader reader)
        {
            if (reader.Peek() == -1)
                return null;
            var firstLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(firstLine))
                return null;
            var firstLineSplit = firstLine.Substring(1, firstLine.Length - 2).Split('{');
            var type = (PublicationEntryType) Enum.Parse(typeof(PublicationEntryType), firstLineSplit[0], true);
            var key = firstLineSplit[1];
            var properties = new Dictionary<string, string>();
            while (true)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line == "}")
                    return new PublicationEntry(type, key, outputLanguage, properties);
                var equalIndex = line.IndexOf("=", StringComparison.Ordinal);
                if (equalIndex == -1)
                    continue;
                var propertyName = line.Substring(0, equalIndex).Trim().ToLowerInvariant();
                var propertyValue = line.Substring(equalIndex + 1).Trim(' ', '{', '}', ',').Replace("{\\_}", "_")
                    .Replace("{\\%}", "%").Replace("{\\&}", "&");
                properties[propertyName] = propertyValue;
            }
        }

        public static List<PublicationEntry> ReadAll(PublicationLanguage outputLanguage, string fileName)
        {
            var entries = new List<PublicationEntry>();
            using (var reader = new StreamReader(Path.Combine(DirectoryDetector.GetDataRawDirectory(), fileName)))
            {
                while (true)
                {
                    var entry = Read(outputLanguage, reader);
                    if (entry != null)
                        entries.Add(entry);
                    else
                        break;
                }
            }

            return entries;
        }

        public static List<PublicationEntry> ReadAll(PublicationLanguage outputLanguage, params string[] fileNames)
        {
            var entries = new List<PublicationEntry>();
            foreach (var fileName in fileNames)
                entries.AddRange(ReadAll(outputLanguage, fileName));
            return entries;
        }
    }
}