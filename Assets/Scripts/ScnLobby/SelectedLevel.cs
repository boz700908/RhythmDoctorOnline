using RhythmCafe.Level;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 当前在关卡浏览器中选中的社区关卡（供 ChartPreview、RoomCreator 使用）
    /// </summary>
    public static class SelectedLevel
    {
        public static LevelDocument Current { get; private set; }

        public static void Set(LevelDocument doc)
        {
            Current = doc;
        }

        /// <summary>谱面下载 URL，默认优先 url2，否则 url</summary>
        public static string ChartUrl
        {
            get
            {
                if (Current == null) return null;
                return !string.IsNullOrEmpty(Current.url2) ? Current.url2 : Current.url;
            }
        }

        /// <summary>谱面显示名称，用于创建房间的 chartName</summary>
        public static string ChartName
        {
            get
            {
                if (Current == null) return null;
                return !string.IsNullOrEmpty(Current.song) ? Current.song : Current.id;
            }
        }
    }
}
