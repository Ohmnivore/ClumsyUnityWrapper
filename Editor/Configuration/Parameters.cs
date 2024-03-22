using System;
using System.Collections.Generic;

namespace Clumsy.Editor
{
    public class ParametersDefinition
    {
        public int NumEntries => m_Entries.Count;

        public IReadOnlyList<string> Entries => m_Entries;

        private List<string> m_Entries = new List<string>();

        public void Parse(string filter)
        {
            var entries = new HashSet<string>();

            var isMatching = false;
            var entry = string.Empty;

            foreach (var c in filter)
            {
                if (c == '{')
                {
                    isMatching = true;
                    entry = string.Empty;
                }
                else if (c == '}')
                {
                    isMatching = false;
                    entries.Add(entry);
                }
                else if (isMatching)
                {
                    entry += c;
                }
            }

            m_Entries = new List<string>(entries);
        }
    }

    public class ParametersProcessor
    {
        public string Filter => m_Filter;

        private string m_Filter;

        private ParametersDefinition m_Definition;

        private readonly Dictionary<string, string> m_Entries = new Dictionary<string, string>();
        private HashSet<string> m_ValidNames;

        public void Initialize(string filter, ParametersDefinition definition)
        {
            m_Filter = filter;
            m_Definition = definition;
            m_Entries.Clear();

            m_ValidNames = new HashSet<string>(m_Definition.Entries);
        }

        public void SetParameter(string name, string value)
        {
            if (!m_ValidNames.Contains(name))
                throw new ArgumentException($"Clumsy: \"{name}\" is not a known parameter in the filter \"{m_Filter}\"");

            m_Entries.TryAdd(name, value);
        }

        public bool IsComplete => m_Entries.Count == m_ValidNames.Count;

        public string GetProcessedFilter()
        {
            var filter = m_Filter;

            foreach (var pair in m_Entries)
            {
                var toBeReplaced = $"{{{pair.Key}}}";
                filter = filter.Replace(toBeReplaced, pair.Value);
            }

            return filter;
        }

        public List<string> GetMissingParameters()
        {
            var missing = new List<string>();

            foreach (var name in m_ValidNames)
            {
                if (!m_Entries.ContainsKey(name))
                    missing.Add(name);
            }

            return missing;
        }

        internal Dictionary<string, string> GetParameters()
        {
            return new Dictionary<string, string>(m_Entries);
        }

        internal void TransferParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            foreach (var entry in parameters)
            {
                if (m_ValidNames.Contains(entry.Key))
                {
                    m_Entries[entry.Key] = entry.Value;
                }
            }
        }
    }
}
