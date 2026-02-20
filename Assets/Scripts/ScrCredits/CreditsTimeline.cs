using System;
using System.Collections.Generic;
using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Builds the full event timeline. Port of credits.py controller events.
    /// </summary>
    public static class CreditsTimeline
    {
        public static CreditsEvent[] Build()
        {
            var list = new List<CreditsEvent>();

            list.Add(new CreditsEvent(0, CreditsEvent.SwapScene("wipe")));
            list.Add(new CreditsEvent(58, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(60, CreditsEvent.SwapScene("ocean_b")));

            // ocean_events
            list.Add(new CreditsEvent(60, sm => sm.SetGeneratorData("ocean_b", 1, "text", CreditsStringTable.GetString("ocean_b_0"))));
            list.Add(new CreditsEvent(310, sm => sm.SetGeneratorData("ocean_b", 1, "text", "[##CLEAR|60;6")));
            list.Add(new CreditsEvent(310, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 3)));
            list.Add(new CreditsEvent(310, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.Blue)));
            list.Add(new CreditsEvent(312, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.BrightBlue)));
            list.Add(new CreditsEvent(314, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.Blue)));
            list.Add(new CreditsEvent(320, sm => sm.SetGeneratorData("ocean_b", 1, "text", CreditsStringTable.GetString("ocean_b_1"), "offset", 0)));
            list.Add(new CreditsEvent(590, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.BrightBlack)));
            list.Add(new CreditsEvent(646, sm => sm.SetGeneratorData("ocean_b", 1, "text", "[##CLEAR|60;8")));
            list.Add(new CreditsEvent(652, sm => sm.SetGeneratorData("ocean_b", 1, "text", CreditsStringTable.GetString("ocean_b_2"), "offset", 0)));
            list.Add(new CreditsEvent(656, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.Yellow)));
            list.Add(new CreditsEvent(666, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.BrightYellow)));
            list.Add(new CreditsEvent(666, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 6)));
            list.Add(new CreditsEvent(850, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.Yellow)));
            list.Add(new CreditsEvent(999, sm => sm.SetGeneratorData("ocean_b", 1, "text", "[##CLEAR|60;10")));
            list.Add(new CreditsEvent(1000, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_col", CanvasColor.BrightBlack)));
            list.Add(new CreditsEvent(1000, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 13)));
            list.Add(new CreditsEvent(1000, sm => sm.SetGeneratorData("ocean_b", 1, "text", CreditsStringTable.GetString("ocean_b_3"), "offset", 0)));
            list.Add(new CreditsEvent(1044, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 102)));
            list.Add(new CreditsEvent(1048, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 230)));
            list.Add(new CreditsEvent(1052, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 500)));
            list.Add(new CreditsEvent(1056, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 760)));
            list.Add(new CreditsEvent(1060, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 1600)));
            list.Add(new CreditsEvent(1064, sm => sm.SetGeneratorData("ocean_b", 0, "ocean_glitch", 2500)));

            list.Add(new CreditsEvent(1079, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(1080, CreditsEvent.SwapScene("beats")));
            list.Add(new CreditsEvent(1080, CreditsEvent.LayerScene("title")));
            list.Add(new CreditsEvent(1080, CreditsEvent.SwapScene("beats")));
            list.Add(new CreditsEvent(1336, CreditsEvent.SwapScene("beats_lr")));
            list.Add(new CreditsEvent(1844, CreditsEvent.RemoveScene("title")));
            list.Add(new CreditsEvent(1848, CreditsEvent.SwapScene("funding")));
            list.Add(new CreditsEvent(1848, CreditsEvent.LayerScene("dates")));
            list.Add(new CreditsEvent(1848, CreditsEvent.LayerScene("weather")));
            list.Add(new CreditsEvent(1848, sm => sm.SetData("history", new List<(string, CanvasColor)>(), "refresh", true)));
            list.Add(new CreditsEvent(1848, sm => sm.SetGeneratorData("funding", 0, "text", CreditsStringTable.Get("funding_0"))));

            list.Add(new CreditsEvent(2348, sm => sm.SetGeneratorData("funding", 0, "text", new[] { new[] { "" }, new[] { "" } })));
            list.Add(new CreditsEvent(2348, sm => sm.SetData("history", new List<(string, CanvasColor)>(), "refresh", true)));
            list.Add(new CreditsEvent(2352, sm => sm.SetGeneratorData("funding", 0, "text", CreditsStringTable.Get("funding_1"), "offset", 0, "lineno", 0)));

            list.Add(new CreditsEvent(2976, CreditsEvent.RemoveScene("dates")));
            list.Add(new CreditsEvent(2976, CreditsEvent.RemoveScene("weather")));
            list.Add(new CreditsEvent(2976, CreditsEvent.SwapScene("clear_wipe")));
            list.Add(new CreditsEvent(3007, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(3132, CreditsEvent.SwapScene("loadingbar")));
            list.Add(new CreditsEvent(3132, sm => sm.SetGeneratorData("loadingbar", 6, "text", CreditsStringTable.Get("funding_2"))));

            list.Add(new CreditsEvent(3376, CreditsEvent.SwapScene("ocean_d")));
            list.Add(new CreditsEvent(3380, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(3380, CreditsEvent.SwapScene("fastload")));
            list.Add(new CreditsEvent(3388, CreditsEvent.SwapScene("clear")));

            list.Add(new CreditsEvent(3390, CreditsEvent.SwapScene("error")));
            list.Add(new CreditsEvent(3390, CreditsEvent.LayerScene("fundingx2")));
            list.Add(new CreditsEvent(3390, sm => sm.SetData("history", new List<(string, CanvasColor)>(), "refresh", true)));
            list.Add(new CreditsEvent(3390, sm => sm.SetGeneratorData("fundingx2", 0, "text", CreditsStringTable.Get("fundingx2_0"))));
            list.Add(new CreditsEvent(3390, sm => sm.SetGeneratorData("fundingx2", 1, "text", CreditsStringTable.Get("fundingx2_1"))));
            list.Add(new CreditsEvent(3390, sm => sm.SetGeneratorData("fundingx2", 3, "text", CreditsStringTable.Get("fundingx2_2"))));

            list.Add(new CreditsEvent(3895, CreditsEvent.RemoveScene("fundingx2")));
            list.Add(new CreditsEvent(3895, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(3896, CreditsEvent.SwapScene("ocean_c")));
            list.Add(new CreditsEvent(3896, sm => sm.SetGeneratorData("ocean_c", 1, "text", CreditsStringTable.GetString("ocean_c_0"))));
            list.Add(new CreditsEvent(3896, sm => sm.SetGeneratorData("ocean_c", 2, "text", CreditsStringTable.GetString("ocean_c_1"))));
            list.Add(new CreditsEvent(3896, sm => sm.SetGeneratorData("ocean_c", 3, "text", CreditsStringTable.GetString("ocean_c_2"))));
            list.Add(new CreditsEvent(3896, sm => sm.SetGeneratorData("ocean_c", 4, "text", CreditsStringTable.GetString("ocean_c_3"))));
            list.Add(new CreditsEvent(4400, sm => sm.SetGeneratorData("ocean_c", 1, "text", "[##CLEAR|60;10")));
            for (int i = 0; i < 5; i++)
            {
                int ii = i;
                list.Add(new CreditsEvent(4401 + i * 2, sm => sm.SetGeneratorData("ocean_c", 0, "ocean_col",
                    UnityEngine.Random.Range(0, 2) == 0 ? CanvasColor.BrightBlack : new CanvasColor(CanvasColor.Black.Color, false))));
            }
            list.Add(new CreditsEvent(4411, sm => sm.SetGeneratorData("ocean_c", 0, "ocean_col", CanvasColor.Black)));

            list.Add(new CreditsEvent(4460, CreditsEvent.SwapScene("accesspoints")));
            list.Add(new CreditsEvent(4460, CreditsEvent.LayerScene("fdg_single")));
            list.Add(new CreditsEvent(4534, sm => sm.SetGeneratorData("fdg_single", 1, "text", CreditsStringTable.Get("fdg_single_0"))));

            list.Add(new CreditsEvent(5500, CreditsEvent.RemoveScene("fdg_single")));
            list.Add(new CreditsEvent(5500, CreditsEvent.SwapScene("clear")));
            list.Add(new CreditsEvent(5500, CreditsEvent.SwapScene("beats")));
            list.Add(new CreditsEvent(5500, CreditsEvent.LayerScene("fdg_down")));
            list.Add(new CreditsEvent(5788, sm => sm.SetGeneratorData("fdg_down", 0, "text", CreditsStringTable.Get("fdg_down_0"))));
            list.Add(new CreditsEvent(5916, sm => sm.SetGeneratorData("fdg_down", 0, "text", CreditsStringTable.Get("fdg_down_1"), "lineno", 0, "offset", 0)));
            list.Add(new CreditsEvent(5916, sm => sm.SetGeneratorData("fdg_down", 1, "text", CreditsStringTable.Get("fdg_down_2"))));

            list.Add(new CreditsEvent(6270, CreditsEvent.SwapScene("beats_lr")));
            list.Add(new CreditsEvent(6508, CreditsEvent.RemoveScene("fdg_down")));
            list.Add(new CreditsEvent(6508, CreditsEvent.SwapScene("clear")));

            return list.ToArray();
        }
    }
}
