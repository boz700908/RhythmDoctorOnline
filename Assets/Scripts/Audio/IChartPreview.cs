namespace RDOnline.Audio
{
    /// <summary>
    /// 谱面预览接口 - 用于统一管理不同的预览组件
    /// </summary>
    public interface IChartPreview
    {
        /// <summary>
        /// 停止预览
        /// </summary>
        void Stop();

        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }
    }
}
