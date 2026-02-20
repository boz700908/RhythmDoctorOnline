using System;
using System.Collections.Generic;

namespace Credits
{
    /// <summary>
    /// All display strings for the credits. Port of string_defs.data_strings.
    /// Builds strings at runtime using FuckUpText, SplitWordTemplate, WorkOutDate to match Python.
    /// </summary>
    public static class CreditsStringTable
    {
        private static readonly Dictionary<string, object> Data = new Dictionary<string, object>();

        static CreditsStringTable()
        {
            Data["ocean_b_0"] = "Now for the official national~ weather~ service~ forecast\n"
                + "~~~~for~~ Eastern Massachusetts~ inside of~~ I-~~~~4~~~~9~~~~~~~5~~,\n"
                + "~~~~~~~~    including Boston,\n\n"
                + "~~~~~~~~issued at 7~~~~:2~~1~~~~ PM~~~~, ~~~~~~~~~~~~~~Thursday, October~~ 2~~2~~nd.";

            Data["ocean_b_1"] = "Tonight:\n\n~~~~~Mostly cloudy with isolated~ showers~ until~~ mid~~~~night,\n"
                + "~~~~~~~~~then mostly clear~~ after~~ mid~night.\n"
                + "~~~~~~~~~Lows in the lower 4~~~~0~~~~s.\n"
                + "~~~~~~~~~West winds 10~ to~~ 1~~5~~ miles~~ an~~ hour\n"
                + "~~~~~with~ gusts~~ up~ to~ 2~~~~5~~~~ miles~~ an~~ hour~~.\n"
                + "~~~~~~~~~~Chance of rain:~~~~ 2~~0~~~~ per~cent.";

            Data["ocean_b_2"] = "Friday:\n\n~~Sunny.\n~~~~~~~~~~~~~~~~Lush colour with highs in the low~er 5~~~0~s.\n"
                + "~~~~~~~~~~Northwest~~ winds~~ 10~~-~1~~5~~ miles~~ an~~ hour\n"
                + "~~~~with gusts up to~~ 2~~~~5~~~~ miles~~ an~~ hour~~.\n"
                + "~~~~~~~~~~~~~~~~Friday night,~~ mostly~ clear.\n"
                + "~~~~~~~~~~~~~~Lows in the mid-3~~~~0~~s.\n"
                + "~~~~~~~~~~~~~~~~North winds 10-1~~~5~~ miles~~ an~~ hour~~.";

            Data["ocean_b_3"] = "Saturday:\n\n~~~~~~~~Partly sunny.\n"
                + "~~~~~~~~High-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-@igh-i";

            Data["ocean_c_0"] = "Here~ are~ the 7~~~~ P~~~~M~~~~ ob~ser~va~tions for~~ the\n"
                + "~~~Bos~ton~~ metro~po~li~tan~~~ ar~ea.\n\n"
                + "~~~~~~~~~~~~~~~At Logan~~~~ Airport,~~~~~~ it was clou~~~~dy.\n"
                + "~~~~~~~~~~~~~~~The tem~per~a~ture was 6~~~~8~~~~ de~grees,\n"
                + "~~~~~~~~the dew point,~~ 4~~~~7~~ -\n"
                + "~~~~~~and~ the~ re~la~tive~~ hu~mi~di~ty,~~~~ 4~~~~6~~~~ per~~cent.\n"
                + "~~~~~~~~~~~~~~~~The~ wind~ was~ south~~west~ at 1~~~~3~ miles~~~~ an~~~~ ho~ur.\n"
                + "~~~~~~~~~~~~The~ pres~~sure~~ was~~ 2~~~~9~~~~.~~~~~~~~9~~~~~~9~~~~~~ in~ches and ri~sing.\n"
                + "~~~~~~~~Elsewher" + AnimationFunctions.FuckUpText("rrrrrrrrr\n", 800);

            Data["ocean_c_1"] = AnimationFunctions.FuckUpText(
                "Here~ are~ the 7~~~~ P~~~~M~~~~ ob~ser~va~tions for~~ the\n"
                + "~~~Bos~ton~~ metro~po~li~tan~~~ ar~ea.\n\n"
                + "~~~~~~~~~~~~~~~At Logan~~~~ Airport,~~~~~~ it was clou~~~~dy.\n"
                + "~~~~~~~~~~~~~~~The tem~per~a~ture was 6~~~~8~~~~ de~grees,\n"
                + "~~~~~~~~the dew point,~~ 4~~~~7~~ -\n"
                + "~~~~~~and~ the~ re~la~tive~~ hu~mi~di~ty,~~~~ 4~~~~6~~~~ per~~cent.\n"
                + "~~~~~~~~~~~~~~~~The~ wind~ was~ south~~west~ at 1~~~~3~ miles~~~~ an~~~~ ho~ur.\n"
                + "~~~~~~~~~~~~The~ pres~~sure~~ was~~ 2~~~~9~~~~.~~~~~~~~9~~~~~~9~~~~~~ in~ches and ri~sing.\n"
                + "~~~~~~~~Elsewher", 100) + AnimationFunctions.FuckUpText("rrrrrrrrr\n", 900);

            Data["ocean_c_2"] = AnimationFunctions.FuckUpText(
                "Here~ are~ the 7~~~~ P~~~~M~~~~ ob~ser~va~tions for~~ the\n"
                + "~~~Bos~ton~~ metro~po~li~tan~~~ ar~ea.\n\n"
                + "~~~~~~~~~~~~~~~At Logan~~~~ Airport,~~~~~~ it was clou~~~~dy.\n"
                + "~~~~~~~~~~~~~~~The tem~per~a~ture was 6~~~~8~~~~ de~grees,\n"
                + "~~~~~~~~the dew point,~~ 4~~~~7~~ -\n"
                + "~~~~~~and~ the~ re~la~tive~~ hu~mi~di~ty,~~~~ 4~~~~6~~~~ per~~cent.\n"
                + "~~~~~~~~~~~~~~~~The~ wind~ was~ south~~west~ at 1~~~~3~ miles~~~~ an~~~~ ho~ur.\n"
                + "~~~~~~~~~~~~The~ pres~~sure~~ was~~ 2~~~~9~~~~.~~~~~~~~9~~~~~~9~~~~~~ in~ches and ri~sing.\n"
                + "~~~~~~~~Elsewher", 200) + AnimationFunctions.FuckUpText("rrrrrrrrr\n", 1000);

            Data["ocean_c_3"] = AnimationFunctions.FuckUpText(
                "Here~ are~ the 7~~~~ P~~~~M~~~~ ob~ser~va~tions for~~ the\n"
                + "~~~Bos~ton~~ metro~po~li~tan~~~ ar~ea.\n\n"
                + "~~~~~~~~~~~~~~~At Logan~~~~ Airport,~~~~~~ it was clou~~~~dy.\n"
                + "~~~~~~~~~~~~~~~The tem~per~a~ture was 6~~~~8~~~~ de~grees,\n"
                + "~~~~~~~~the dew point,~~ 4~~~~7~~ -\n"
                + "~~~~~~and~ the~ re~la~tive~~ hu~mi~di~ty,~~~~ 4~~~~6~~~~ per~~cent.\n"
                + "~~~~~~~~~~~~~~~~The~ wind~ was~ south~~west~ at 1~~~~3~ miles~~~~ an~~~~ ho~ur.\n"
                + "~~~~~~~~~~~~The~ pres~~sure~~ was~~ 2~~~~9~~~~.~~~~~~~~9~~~~~~9~~~~~~ in~ches and ri~sing.\n"
                + "~~~~~~~~Elsewher", 250) + AnimationFunctions.FuckUpText("rrrrrrrrr\n", 1000);

            Data["funding_0"] = SplitWordTemplateDouble(
                "Fun####ding#### for#### this#### pro####gram#### was#### made#### pos####si####ble###~\n"
                + "    Nexus  jlj1102  ReinaHikari0404###\n      \u662f\u82e9\u51ac\u5440  \u5802\u7960  7unSgELE###\n        liseeliot  \u30d6\u30eb\u30fc\u30a8\u30c3\u30c1\u30f3\u30b0  \u9b54mo###\n          \u665a\u98ce~  \u7965\u6faa  \u7c89\u7ea2\u732b#\n"
                + "Fun#\nFunding#\n       by\n         by\n"
                + "           by\n             by\nFunding for\nFunding for thi#i#i#i#i#\n"
                + "Funding for this pro####gram###\nFunding for this pro####gram###\n                   pro\n"
                + "                     pro\n                       pro\nFunding for this pro#gram.#~\n"
                + "Fun####ding#### for#\n           by#\n             by#\n"
                + "Funding made#### pos####si####ble#### by#### view####ers#### like#### you.###~\n"
                + "####like#### you.##\n" + "####like#### you.##\n" + "####like#### you.##\n"
                + "####like#### you.##\n" + "####like#### you.##\n" + "####like#### you.\n Fu\n  Fu\n");

            Data["funding_1"] = BuildFunding1();

            Data["funding_2"] = AnimationFunctions.SplitWordTemplate(
                "--####--####-- ####-i####-a-####---- ####-up####-o-#\n"
                + "-n####nu####-l ####fi####nan####cial ####sup####por#\n"
                + "An####nu####al ####fi####nan####cial ####sup####por#\n" + "An####nu####al ####fi####nan####cial ####sup####por#\n" + "An####nu####al ####fi####nan####cial ####sup####por#\n"
                + "An####nu####al ####fi####nan####cial ####sup####por#\n" + "An####nu####al ####fi####nan####cial ####sup####por#\n"
                + "An####nu###\nAn####nu###\n");

            Data["fundingx2_0"] = BuildFundingX2_0();
            Data["fundingx2_1"] = BuildFundingX2_1();
            Data["fundingx2_2"] = BuildFundingX2_2();
            Data["fdg_single_0"] = BuildFdgSingle0();
            Data["fdg_down_0"] = AnimationFunctions.SplitWordTemplate(
                "Fun####ding#### for#### this#### pro####gram#### was#\n"
                + "made#### made#### made#### made#### made#### made#### made#### made#### made###\n"
                + "pos####sible#### by#### view####ers#### like#### you.#######");
            Data["fdg_down_1"] = AnimationFunctions.SplitWordTemplate(
                "---####----#### ---#### ----#### ---####----#### ---#\n"
                + "----#### ----#### ----#### ----#### ----#### ----#### ----#### ----#### ----###\n"
                + "---####-----#### --#### ----####---###### ----######## -##-##-##.#######");
            Data["fdg_down_2"] = AnimationFunctions.SplitWordTemplate(
                "Fun####ding#### for#### this#### pro####gram#### was#\n"
                + "made#### made#### made#### made#### made#### made#### made#### made#### made###\n"
                + "pos####sible#### by#### view####ers###### like######## y##o##u##.#######");
        }

        private static string[][] SplitWordTemplateDouble(string s)
        {
            var a = AnimationFunctions.SplitWordTemplate(s);
            var list = new List<string[]>(a);
            list.AddRange(list);
            return list.ToArray();
        }

        private static string[][] BuildFunding1()
        {
            string baseStr = "Broad####cast####\nBroadcast Cor####por##a####tion.~#####\n"
                + "Cor####po##ra####tion.#####\n" + "Cor####po##ra####tion.#####\n" + " Cor###po\n  Cor###po\n    Co\n      Co\n"
                + "Cor####po##ra####tion.#####\n" + "Cor####po##ra####tion.#####\n" + "Cor####po##ra####tion.#####\n"
                + " Cor###po\n  Cor###po\n    Co\n      Co\nCor####po##ra###tion.#\n Co\n  Co\nCor####po##ra###tion.#\n Co\n  Co\n"
                + "Cor####po##ra###tion.#\n Co\n  Co\nCor####po##ra###tion.#\n Co\n  Co\nCor####po##ra###tion.#\n Co\n  Co\n"
                + " Cor###po\n  Cor###po\n    Cor###po\n      Cor###po\n...#######\n Cor###po#\n     cor##\n  cor###po#\n"
                + "    cor###po#\n cor##\n      cor###po#\n  cor###po#\n         cor##\n     cor###po#\n cor##\n   cor##\n"
                + "     cor##\n       co#\n     co#\n cor###po#\n   cor##\n cor###po#\n   cor###po#\n     cor##\n       cor##\n"
                + "         cor###po#\n" + AnimationFunctions.FuckUpText(" cor###po#\n   cor#\n cor###po#\n   cor#\n     cor#\n", 100, "# ")
                + " ...#\n" + AnimationFunctions.FuckUpText(" co#\n  co#\n cor###po#\n   cor##\n     cor###po#\n   cor###po#\n            cor##\n      cor###po#\n", 280, "# ")
                + AnimationFunctions.FuckUpText("             cor###po#\n    cor##\n                  cor###po#\n  cor#\n          cor#\n                    cor#\n", 500, "# ")
                + AnimationFunctions.FuckUpText(" co#\n                co#\n     cor###po#\n                        cor#\n  cor###po#\n           cor###po#\n             cor#\n  cor###po#\n", 650, "# ")
                + " ???###p?#\n   ??r#\n           ???###??#\n                       co?#\n                                       ???#\n"
                + "....#....#....#....#....#....#....#....#...#...#...#...#..#..\n"
                + "              <##C##O##N##N##E##C##T##I##O##N## ##L##O##S##T##>"
                + "##########################################################################################################"
                + "##########################################################################################################"
                + "##########################################################################################################"
                + "##########################################################################################################";
            return AnimationFunctions.SplitWordTemplate(baseStr);
        }

        private static string[][] BuildFundingX2_0()
        {
            string s = " By\n  ci\n    po\n      po\n  cor#\n    cor#\n        by#\n          by#\n"
                + "rr#rr#rr#ro#oo#oo#aa#aa#aa#\n##  wers#\n ble#\n       b\n         b\n           F#\n         f\n       fi\n     i\n"
                + "na#aa#aa#aa#aa#aa#aa#aa#\n Fun\nFun####ding#\n Fu#\n  Fun#\n fu#\nFun####ding#\n Fu#\n  Fun#\n fu#\n"
                + "Fun####ding#\n Fu#\n  Fun#\n fu#\nFun####ding#\nF\n    Fi\n   Fin\n          Fina\n      Finan\n"
                + "po\npo\ncor#\nby#\n  por\nPor#tio##\nPor#tio##\nPor#tion# nn#nn#nn\nb\n b\nby###\n---- by###\n"
                + "-------- by###\n------------ by###\n---------------- by#######\n>>#>>###\nBy#\n  by#\nfi#nan#\n b#\n  b#\n   nc#\n    nc#\n"
                + "Corr#rr#rr#\nView####ers#### like#### you.#\n      like#### you.#\n    like## you.#\nFun####ding###\nFun####ding###\n"
                + "  Fun###\n    Fu#\nFun####ding###\n  fu#\n    fu#\n      fu#\n        fu#\n          fu#\nCor####por####a####tion\n"
                + "The#### cor####por####a####tion#### for#### pub####lic#### broad####cast####ing#### and#### bi####-an####nual#### fii#ii#i\n"
                + "of#\nnan#\nfor#\nfin####an####cial#### su#\nfor#### fin####an####cial#### ass#ss#ss\nin#\nview####ers###\n"
                + "you#####\n| ########This######## is######## P####B####S!############~\n";
            return AnimationFunctions.SplitWordTemplate(s);
        }

        private static string[][] BuildFundingX2_1()
        {
            var lines = new List<string>();
            for (int i = 0; i < 26; i++)
                lines.Add(AnimationFunctions.WorkOutDate(64 * 3390 + i * 64).Replace(".", ".####") + " #### Unknown###");
            lines.Add("\n| ########This######## is######## P####B####S!############~\n");
            return AnimationFunctions.SplitWordTemplate(string.Join("\n", lines));
        }

        private static string[][] BuildFundingX2_2()
        {
            string s = "S#e#a#r#c#h#i#n#g# #f#o#r# #a#c#c#e#s##s## ##p##o##i##n##t"
                + "########.########.########.########\n"
                + "Searching for access point########.########.########.########\n";
            var sb = new System.Text.StringBuilder(s);
            for (int i = 0; i < 11; i++)
                sb.Append("Searching for access point########.########.########.########\n");
            sb.Append("Found:################ P#B#S# #O#f#f#i#c#i#a#l# #1#1#.#### #R#e#s#t#a#r#t#i#n#g#.####.####.####");
            return AnimationFunctions.SplitWordTemplate(sb.ToString());
        }

        private static string[][] BuildFdgSingle0()
        {
            string line = "########.########.########.########.########.########.########.#######\n";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 6; i++) sb.Append(line);
            sb.Append("Fun####ding#### for#### this#### pro####gram#### was#### made#### pos####sible#\n");
            sb.Append(" 7 > > > by###\n 7 > > > > by###\n 8 > > > by###\n 8 > > > > by###\n 9 > > > by###\n");
            sb.Append("Fun##\n     by\n       by\n         by\n           by\nFunding#\n     by\n       by\n         by\n           by\n");
            sb.Append("for\nthi#i#i#i\nPro####gram.#\nPro####gram.#\nPro\n  pro\n    pro\n      pro\nPro####gram.###\n");
            sb.Append("Fun####ding#### for####\n            by#\n              by#\n");
            sb.Append("Funding for made#### pos####sible#### by#### view####ers#### like#### you.###\n");
            sb.Append("like#### you.#####\n##like#### you.#####\n##like#### you.#####\n##like#### you.#####\n##like#### you.#####\n##like#### you.###\n");
            sb.Append("Fu\n  Fu\nFun####ding#### for#### this#### pro####gram#### was#### made#### pos####sible#### by#\n");
            sb.Append("                                         by###\n                                       by###\n                                     by###\n                                   by#\n");
            sb.Append("Fun###\n     by\n       by\n         by\n           by\nFunding#\n     by\n       by\n         by\n           by\n");
            sb.Append("for\nthi#i#i#i\nPro####gram.###\nPro####gram.###\nPro\n  pro\n    pro\n      pro\nPro####gram.#\n");
            sb.Append("Fun####ding#### for####\n            by#\n              by#\n");
            sb.Append("Funding for made#### pos####sible#### by#### view####ers#### like#### you.###\n");
            sb.Append("like#### you.#####\n##like#### you.#####\n##like#### you.#####\n##like#### you.#####\n##like#### you.#####\n");
            sb.Append("####< RET 200################################################################\n");
            return AnimationFunctions.SplitWordTemplate(sb.ToString());
        }

        public static object Get(string key)
        {
            return Data.TryGetValue(key, out var v) ? v : null;
        }

        public static string GetString(string key) => Get(key) as string;

        public static string[][] GetWordTemplate(string key) => Get(key) as string[][];
    }
}
