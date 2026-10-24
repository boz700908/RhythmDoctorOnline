using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RDOnline.A11y;

namespace RDOnline.A11y
{
    /// <summary>
    /// 检查更新首屏（scnCheckUpdate）的无障碍。
    ///
    /// 这个场景的脚本由 CheckUpdate.dll 提供，里面没有无障碍代码，界面字段
    /// 又全是私有的，所以这里用反射把它读出来，不改那个程序集：
    ///   标题、当前步骤、进度百分比、公告 —— 变化时直接读，不占焦点；
    ///   进入游戏按钮 —— 进导航，回车触发。
    ///
    /// 检查还没跑完时，导航里只放一个「状态」项，读出来就是屏幕上的内容；
    /// 检查结束后换成「进入游戏」按钮。
    /// </summary>
    public class CheckUpdateA11y : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 这个场景的脚本由 CheckUpdate.dll 提供，按类型名找最稳
            UnityEngine.Component checkUpdate = FindCheckUpdate();
            if (checkUpdate == null) return;
            if (_instance != null) return;

            var go = new GameObject("CheckUpdateA11y");
            go.AddComponent<CheckUpdateA11y>()._target = checkUpdate;
        }

        private static CheckUpdateA11y _instance;

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            // 场景卸载时要让掉单例，否则下次进这个场景挂不上
            if (_instance == this) _instance = null;
            if (_nav != null) _nav.Deactivate();
        }

        private UnityEngine.Component _target;
        private A11yNavigator _nav;

        private TMP_Text _title;
        private TMP_Text _step;
        private TMP_Text _progress;
        private TMP_Text _announcement;
        private Button _enterGame;

        private string _lastTitle;
        private string _lastStep;
        private string _lastProgress;
        private string _lastAnnouncement;
        private bool _buttonUsable;
        private bool _announcedButton;
        private float _nextScan;

        private void Start()
        {
            _nav = gameObject.AddComponent<A11yNavigator>();
            _nav.ListName = A11yPhrases.Text("检查更新", "Update check");

            RefreshRefs();

            // 进来就接管键盘，落点在第一项；先不念项名，下面统一念屏幕内容
            _buttonUsable = IsButtonUsable();
            RebuildNav();
            _nav.SayIntro(A11yPhrases.Text(
                "检查更新。上下光标换项，回车确认。",
                "Update check. Up and down switch items, Enter confirms."));

            // 进来先把屏幕上已有的内容读一遍
            AnnounceIfChanged(true);
            SyncButton(false);
        }

        private void Update()
        {
            if (_target == null) return;

            // 进度条一直在动，半秒看一次就够，读太勤会把别的话挤掉
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.5f;

            RefreshRefs();
            AnnounceIfChanged(false);
            SyncButton(false);
        }

        // ---------- 引用 ----------

        private static UnityEngine.Component FindCheckUpdate()
        {
            MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetType().Name == "ScnCheckUpdate") return all[i];
            }
            return null;
        }

        private void RefreshRefs()
        {
            if (_target == null) return;
            if (_title == null) _title = Field<TMP_Text>("txtTitle");
            if (_step == null) _step = Field<TMP_Text>("txtStep");
            if (_progress == null) _progress = Field<TMP_Text>("txtProgress");
            if (_announcement == null) _announcement = Field<TMP_Text>("txtAnnouncement");
            if (_enterGame == null) _enterGame = Field<Button>("btnEnterGame");
        }

        /// <summary>读私有字段；类型里没有就返回 null，不抛异常。</summary>
        private T Field<T>(string name) where T : class
        {
            if (_target == null) return null;

            FieldInfo info = _target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return info != null ? info.GetValue(_target) as T : null;
        }

        // ---------- 播报 ----------

        private string TitleText() => _title != null ? _title.text : null;
        private string StepText() => _step != null ? _step.text : null;
        private string ProgressText() => _progress != null ? _progress.text : null;
        private string AnnouncementText() => _announcement != null ? _announcement.text : null;

        /// <summary>
        /// 有变化就读。结论（标题）优先，其次当前步骤，最后才是纯百分比。
        /// </summary>
        private void AnnounceIfChanged(bool force)
        {
            string title = TitleText();
            string step = StepText();
            string progress = ProgressText();
            string announcement = AnnouncementText();

            bool titleChanged = !string.Equals(title, _lastTitle);
            bool stepChanged = !string.Equals(step, _lastStep);
            bool progressChanged = !string.Equals(progress, _lastProgress);
            bool announcementChanged = !string.Equals(announcement, _lastAnnouncement);

            if (!force && !titleChanged && !stepChanged && !progressChanged && !announcementChanged)
                return;

            _lastTitle = title;
            _lastStep = step;
            _lastProgress = progress;
            _lastAnnouncement = announcement;

            if (!string.IsNullOrEmpty(announcement) && (force || announcementChanged))
            {
                A11yNavigator.SayInfo(
                    A11yPhrases.Text("公告", "Announcement") + A11yPhrases.Separator + announcement,
                    NarrationCategory.Notification);
            }

            // 标题带结论（「发现新版本」「更新失败」「欢迎来到 RD Online」）
            if (!string.IsNullOrEmpty(title) && (force || titleChanged))
            {
                A11yNavigator.SayInfo(title, NarrationCategory.Notification);
            }

            // 步骤里同时有文件序号与传输量，比百分比信息多
            if (!string.IsNullOrEmpty(step) && (force || stepChanged))
            {
                A11yNavigator.SayInfo(step, NarrationCategory.Notification);
            }
            else if (!string.IsNullOrEmpty(progress) && progressChanged)
            {
                A11yNavigator.SayInfo(
                    A11yPhrases.Text("进度", "Progress") + " " + progress,
                    NarrationCategory.Notification);
            }
        }

        // ---------- 导航 ----------

        /// <summary>
        /// 检查跑完才会亮出「进入游戏」。按钮可用后进导航，用户按上下光标就能找到。
        /// </summary>
        private void SyncButton(bool force)
        {
            if (_enterGame == null) return;

            bool usable = IsButtonUsable();
            if (!force && usable == _buttonUsable) return;

            _buttonUsable = usable;
            RebuildNav();

            if (usable && !_announcedButton)
            {
                _announcedButton = true;
                A11yNavigator.SayInfo(
                    A11yPhrases.Text("更新完成，按回车进入游戏", "Update finished, press enter to start the game"),
                    NarrationCategory.Notification);
            }
        }

        private void RebuildNav()
        {
            if (_nav == null) return;

            _nav.BeginRebuild();
            _nav.Clear();

            if (_buttonUsable && _enterGame != null)
            {
                _nav.StartRow(A11yPhrases.Text("操作", "Actions"));
                _nav.Add(new A11yNavItem(_enterGame,
                    A11yPhrases.Text("进入游戏", "Enter the game"),
                    null,
                    () => _enterGame.onClick.Invoke()));
            }
            else
            {
                // 还在检查更新：给一个只念不做事的落点，屏幕信息也顺便读得到
                _nav.StartRow(A11yPhrases.Text("状态", "Status"));
                _nav.Add(A11yNavItem.Custom(gameObject, A11yItemKind.Label,
                    A11yPhrases.Text("更新状态", "Update status"),
                    StatusSpeech, null));
            }

            // 自己还没占键盘就先占住；被别人占着（例如弹窗）就只更新清单
            if (_nav.CanTakeKeyboard) _nav.FocusFirst(false);
            else _nav.EndRebuild(false);
        }

        private bool IsButtonUsable()
        {
            return _enterGame != null
                && _enterGame.gameObject.activeInHierarchy
                && _enterGame.interactable;
        }

        private string StatusSpeech()
        {
            var parts = new List<string>(3);

            string title = TitleText();
            string step = StepText();
            string progress = ProgressText();

            if (!string.IsNullOrEmpty(title)) parts.Add(title);
            if (!string.IsNullOrEmpty(step)) parts.Add(step);
            if (!string.IsNullOrEmpty(progress))
                parts.Add(A11yPhrases.Text("进度", "Progress") + " " + progress);

            if (parts.Count == 0) return A11yPhrases.Text("正在准备", "Getting ready");
            return string.Join(A11yPhrases.Separator, parts.ToArray());
        }
    }
}
