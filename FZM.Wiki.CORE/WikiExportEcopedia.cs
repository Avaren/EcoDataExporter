﻿using Eco.Gameplay.Systems.Chat;
using System.Collections.Generic;
using Eco.Gameplay.EcopediaRoot;
using System.Text;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Eco.Shared.Localization;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;

namespace FZM.Wiki
{
    public partial class WikiDetails
    {
        // dictionary of pages and their entries
        private static SortedDictionary<string, Dictionary<string, string>> EveryPage = new SortedDictionary<string, Dictionary<string, string>>();

        public static void EcopediaDetails()
        {
            // dictionary of page details
            Dictionary<string, string> entry = new Dictionary<string, string>()
            {
                { "displayName", "nil" },
                { "displayNameUntranslated", "nil" },
                { "summary", "nil" },
                { "subpages", "nil" },
                { "associatedTypes", "nil" },
                { "sectionsText", "nil" },
            };

            foreach (var cat in Ecopedia.Obj.Categories.Values)
            {
                foreach (var page in cat.Pages)
                {                 
                    EcopediaPage p = page.Value;
                    string pageName = p.DisplayName;
                    if (!EveryPage.ContainsKey(p.DisplayName))
                    {
                        EveryPage.Add(pageName, new Dictionary<string, string>(entry));

                        StringBuilder sb = new StringBuilder();
                        if (p.Sections != null)
                        {
                            foreach (var sec in p.Sections)
                            {                                
                                if (sec is EcopediaBanner || sec is EcopediaButton)
                                    continue;                               

                                sb.Append($"{{'{sec.GetType().Name}', '{Regex.Replace(JSONStringSafe(CleanTags(sec.Text)),"[\n\r]+", "\\n\\n")}'}}");

                                if (sec != p.Sections.Last())
                                    sb.Append(", ");
                            }
                            EveryPage[pageName]["sectionsText"] = $"{{{sb}}}";
                        }

                        if (p.SubPages != null)
                        {

                            sb = new StringBuilder();
                            foreach (var sp in p.SubPages)
                            {
                                sb.Append($"'{Localizer.DoStr(sp.Key)}'");

                                if (sp.Key != p.SubPages.Last().Key)
                                    sb.Append(", ");
                            }
                            EveryPage[pageName]["subpages"] = $"{{{sb}}}";
                        }

                        EveryPage[pageName]["displayName"] =  $"'{p.DisplayName}'";
                        EveryPage[pageName]["displayNameUntranslated"] = p.DisplayName.NotTranslated != null ? $"'{p.DisplayName.NotTranslated}'" : $"nil";

                        if (p.Summary != null && p.Summary != "")
                        {
                            var sum = p.Summary.Trim().TrimEnd('\r', '\n').Trim();
                            EveryPage[pageName]["summary"] = $"'{sum}'";
                        }

                        // There appears to be no need for the generated data as it's world specific info
                        /*
                        EveryPage[pageName]["hasGeneratedData"] = p.HasGeneratedData? Localizer.DoStr("Yes") : Localizer.DoStr("No");

                        sb = new StringBuilder();
                        var genData = (List<IEcopediaGeneratedData>)GetFieldValue(p, "generatedData");
                        if (genData != null)
                        {
                            foreach (var gd in genData)
                            {
                                sb.Append(gd.GetEcopediaData(user.Player, p));

                                if (gd != genData.Last())
                                    sb.Append(", ");
                            }
                            EveryPage[pageName]["generatedData"] = $"{sb}";
                        }
                        */

                        if (p.AssociatedTypes != null && p.AssociatedTypes.Count > 0)
                        {
                            sb = new StringBuilder();
                            foreach (var (Type, Display) in p.AssociatedTypes)
                            {
                                sb.Append($"'{Localizer.DoStr(Type.Name)}'");

                                if (Type.Name != p.AssociatedTypes.Last().Type.Name)
                                    sb.Append(", ");
                            }
                            EveryPage[pageName]["associatedTypes"] = $"{{{sb}}}";
                        }
                    }
                }
            }

            // writes to WikiItems.txt to the Eco Server directory.
            WriteDictionaryToFile("Wiki_Module_Ecopedia.txt", "ecopedia", EveryPage);
        }
    }
}
