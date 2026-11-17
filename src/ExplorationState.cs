namespace Cthangover.Mods.Exploration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Save-owned map namespace with no render resources or current visibility. Validation precedes generation
    /// replacement, so malformed saves cannot partially change the active playthrough.
    /// </summary>
    public sealed class ExplorationState
    {
        /// <summary>
        /// Namespaced adapter identities keep independent maps from sharing explored bits accidentally.
        /// </summary>
        public Dictionary<string, ExplorationRecord> Maps { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Checks every identity and bounded grid without publishing any of the supplied mutable data.
        /// </summary>
        public void Validate()
        {
            if (Maps == null || Maps.Count > 256)
            {
                throw new ArgumentException("Exploration save supports at most 256 maps");
            }

            foreach (var pair in Maps)
            {
                ValidateIdentity(pair.Key);
                if (pair.Value == null)
                {
                    throw new ArgumentException("Missing exploration record");
                }

                pair.Value.Validate();
            }
        }

        /// <summary>
        /// Accepts bounded namespace paths, rejecting empty segments, traversal and platform-dependent separators.
        /// </summary>
        public static void ValidateIdentity(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 256 || id.Contains('\\') || id.Contains(':'))
            {
                throw new ArgumentException("Invalid exploration identity");
            }

            foreach (var segment in id.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..")
                {
                    throw new ArgumentException("Invalid exploration identity segment");
                }
            }
        }
    }
}
