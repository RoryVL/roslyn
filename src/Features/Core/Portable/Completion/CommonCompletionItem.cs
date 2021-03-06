﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CommonCompletionItem 
    {
        public static CompletionItem Create(
            string displayText,
            TextSpan span,
            Glyph? glyph = null,
            ImmutableArray<SymbolDisplayPart> description = default(ImmutableArray<SymbolDisplayPart>),
            string sortText = null,
            string filterText = null,
            bool preselect = false,
            bool showsWarningIcon = false,
            bool shouldFormatOnCommit = false,
            bool isArgumentName = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            CompletionItemRules rules = null)
        {
            tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;

            if (glyph != null)
            {
                // put glyph tags first
                tags = GlyphTags.GetTags(glyph.Value).AddRange(tags);
            }

            if (showsWarningIcon)
            {
                tags = tags.Add(CompletionTags.Warning);
            }

            if (isArgumentName)
            {
                tags = tags.Add(CompletionTags.ArgumentName);
            }

            properties = properties ?? ImmutableDictionary<string, string>.Empty;
            if (!description.IsDefault && description.Length > 0)
            {
                properties = properties.Add("Description", EncodeDescription(description));
            }

            rules = rules ?? CompletionItemRules.Default;
            rules = rules.WithPreselect(preselect)
                         .WithFormatOnCommit(shouldFormatOnCommit);

            return CompletionItem.Create(
                displayText: displayText,
                filterText: filterText,
                sortText: sortText,
                span: span,
                properties: properties,
                tags: tags,
                rules: rules);
        }

        public static bool HasDescription(CompletionItem item)
        {
            return item.Properties.ContainsKey("Description");
        }

        public static CompletionDescription GetDescription(CompletionItem item)
        {
            string encodedDescription;
            if (item.Properties.TryGetValue("Description", out encodedDescription))
            {
                return DecodeDescription(encodedDescription);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }

        private static char[] s_descriptionSeparators = new char[] { '|' };

        private static string EncodeDescription(ImmutableArray<SymbolDisplayPart> description)
        {
            return EncodeDescription(description.Select(d => new TaggedText(SymbolDisplayPartKindTags.GetTag(d.Kind), d.ToString())).ToImmutableArray());
        }

        private static string EncodeDescription(ImmutableArray<TaggedText> description)
        {
            if (description.Length > 0)
            {
                return string.Join("|",
                    description
                        .SelectMany(d => new string[] { d.Tag, d.Text })
                        .Select(t => t.Escape('\\', s_descriptionSeparators)));
            }
            else
            {
                return null;
            }
        }

        private static CompletionDescription DecodeDescription(string encoded)
        {
            var parts = encoded.Split(s_descriptionSeparators).Select(t => t.Unescape('\\')).ToArray();

            var builder = ImmutableArray<TaggedText>.Empty.ToBuilder();
            for (int i = 0; i < parts.Length; i += 2)
            {
                builder.Add(new TaggedText(parts[i], parts[i + 1]));
            }

            return CompletionDescription.Create(builder.ToImmutable());
        } 
    }
}
