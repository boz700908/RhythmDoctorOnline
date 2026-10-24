using System.Collections.Generic;
using UnityEngine;
using Component = UnityEngine.Component;
using UnityEngine.EventSystems;

namespace RDOnline.A11y
{
    /// <summary>
    /// 键盘导航控制器。全部界面用同一套按键模型：
    ///   上下光标   在「组」之间移动（原来的 Tab 职责）
    ///   左右光标   在当前组内移动（原来的上下光标职责）
    ///   回车       输入框：打开编辑；再按一次关闭编辑
    ///              按钮：触发；开关：翻转
    ///   Esc        离开编辑状态；不在编辑时调用 BackAction（返回）
    ///
    /// 组的划分完全由代码里加入的次序决定，不读取也不假设屏幕位置；
    /// 播报里的位置交给 Narration 自己的序号机制（第 N 项，共 M 项）。
    /// 一个组只有一个控件时（登录界面即如此），上下就等于逐项移动，左右不做事。
    /// </summary>
    public class A11yNavigator : MonoBehaviour
    {
        private class Row
        {
            public string Name;
            public readonly List<A11yNavItem> Items = new List<A11yNavItem>();
            /// <summary>记住这个组上次停在哪一项，返回该组时回到原位。</summary>
            public int Last;
        }

        private readonly List<Row> _rows = new List<Row>();
        private int _rowIndex = -1;
        private int _colIndex = -1;
        private bool _editing;

        /// <summary>不在编辑状态时按 Esc 的行为，一般是「返回上一级」。</summary>
        public System.Action BackAction;

        /// <summary>导航容器的名称，朗读列表整体信息时使用。</summary>
        public string ListName;

        public bool IsActive { get; private set; }

        /// <summary>
        /// 当前占用键盘的控制器。弹出面板（密码框等）临时接管时，底下的
        /// 控制器继续存在但不再收键，避免同一个按键被两处处理。
        /// </summary>
        public static A11yNavigator Active { get; private set; }

        private string _keepRowName;
        private int _keepRow = -1;
        private GameObject _keepRoot;
        private bool _keepValid;

        /// <summary>
        /// 接管期间关掉 EventSystem 自带的方向键/提交导航，否则同一按键会被
        /// Unity 和这里各处理一次，焦点会乱跳。文字输入不受影响。
        /// </summary>
        private bool _savedNavigationEvents = true;
        private bool _navigationSuppressed;

        private Row CurrentRow =>
            _rowIndex >= 0 && _rowIndex < _rows.Count ? _rows[_rowIndex] : null;

        private A11yNavItem Current
        {
            get
            {
                Row row = CurrentRow;
                if (row == null || _colIndex < 0 || _colIndex >= row.Items.Count) return null;
                return row.Items[_colIndex];
            }
        }

        public void Clear()
        {
            _rows.Clear();
            _rowIndex = -1;
            _colIndex = -1;
        }

        /// <summary>开一个新组；随后的 Add 都归到这个组。</summary>
        public void StartRow(string name = "")
        {
            _rows.Add(new Row { Name = name });
        }

        /// <summary>加入当前组；没有组时自动开一个。</summary>
        public void Add(A11yNavItem item)
        {
            if (item == null || item.Root == null) return;
            if (_rows.Count == 0) StartRow();

            Row row = _rows[_rows.Count - 1];
            // 同一控件重复加入会让移动卡住，这里直接忽略
            for (int i = 0; i < row.Items.Count; i++)
            {
                if (row.Items[i].Root == item.Root) return;
            }
            row.Items.Add(item);
        }

        /// <summary>单独成组地加入若干控件（登录界面每个控件一组）。</summary>
        public void AddRow(params A11yNavItem[] items)
        {
            StartRow();
            for (int i = 0; i < items.Length; i++) Add(items[i]);
        }

        /// <summary>加入一个具名组，成员由调用方给出（动态列表用）。</summary>
        public void AddNamedRow(string name, IEnumerable<A11yNavItem> items)
        {
            StartRow(name);
            if (items == null) return;
            foreach (A11yNavItem item in items) Add(item);
        }

        /// <summary>以一个有名字的组加入若干控件（大厅的列表组、按钮组）。</summary>
        public void AddRow(string name, params A11yNavItem[] items)
        {
            StartRow(name);
            for (int i = 0; i < items.Length; i++) Add(items[i]);
        }

        /// <summary>
        /// 读进入界面时的一段说明（窗口标题、整体数量、可用按键）。
        /// 走 Instruction 类别，之后按 F1 可以重复听到。
        /// </summary>
        public void SayIntro(string speech, params NarrationActionName[] actions)
        {
            if (string.IsNullOrEmpty(speech)) return;
            if (!HasNarration) return;
            Narration.SetInstructions(speech, actions ?? NavigationActions());
        }

        /// <summary>进入这个界面：清掉旧状态，把焦点放到第一项并朗读。</summary>
        public void FocusFirst(bool announce = true)
        {
            IsActive = true;
            Active = this;
            _editing = false;
            _rowIndex = -1;
            _colIndex = -1;
            SuppressUnityNavigation(true);
            MoveRow(1, announce);
        }

        /// <summary>
        /// 重建清单前调用：记下当前停在哪一组、哪一项，然后清空。
        /// 用于房间列表、关卡列表这类内容随时会变的界面。
        /// </summary>
        public void BeginRebuild()
        {
            _keepValid = true;
            _keepRow = _rowIndex;
            Row row = CurrentRow;
            _keepRowName = row != null ? row.Name : null;
            A11yNavItem item = Current;
            _keepRoot = item != null ? item.Root : null;

            _rows.Clear();
            _rowIndex = -1;
            _colIndex = -1;
        }

        /// <summary>
        /// 重建完成后调用：把焦点放回原来的项；那一项不在了就停在同一组的第一项。
        /// 只有原来的组整组消失时才换组。
        /// </summary>
        public void EndRebuild(bool announce = false)
        {
            if (_rows.Count == 0)
            {
                _keepValid = false;
                return;
            }

            int row = -1;
            if (_keepValid && !string.IsNullOrEmpty(_keepRowName))
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Name == _keepRowName) { row = i; break; }
                }
            }
            if (row < 0 && _keepValid && _keepRow >= 0 && _keepRow < _rows.Count) row = _keepRow;
            if (row < 0) row = 0;

            _rowIndex = row;
            Row target = _rows[row];

            int col = 0;
            if (_keepValid && _keepRoot != null)
            {
                for (int i = 0; i < target.Items.Count; i++)
                {
                    if (target.Items[i].Root == _keepRoot) { col = i; break; }
                }
            }

            _colIndex = ClampAlive(target, col);
            target.Last = _colIndex;
            _keepValid = false;

            if (!RowHasAlive(target)) MoveRow(1, false);

            if (announce && Active == this) ReannounceCurrent();
        }

        /// <summary>
        /// 现在能不能拿键盘：自己正占着，或者根本没人占。
        /// 被弹窗接管期间为 false，底部界面重建清单时就不能抢焦点。
        /// </summary>
        public bool CanTakeKeyboard => IsActive || Active == null || Active == this;

        /// <summary>临时接管的界面关闭后，把键盘交回来，不改变原位置。</summary>
        public void Resume(bool announce = false)
        {
            IsActive = true;
            Active = this;
            SuppressUnityNavigation(true);
            if (announce) ReannounceCurrent();
        }

        public void Deactivate()
        {
            IsActive = false;
            if (Active == this) Active = null;
            StopEditing(false);
            SuppressUnityNavigation(false);
        }

        private void SuppressUnityNavigation(bool suppress)
        {
            if (EventSystem.current == null) return;
            if (suppress == _navigationSuppressed) return;

            if (suppress)
            {
                _savedNavigationEvents = EventSystem.current.sendNavigationEvents;
                EventSystem.current.sendNavigationEvents = false;
                _navigationSuppressed = true;
            }
            else
            {
                EventSystem.current.sendNavigationEvents = _savedNavigationEvents;
                _navigationSuppressed = false;
            }
        }

        private void OnDisable()
        {
            SuppressUnityNavigation(false);
            if (Active == this) Active = null;
        }

        private void Update()
        {
            if (!IsActive || Active != this) return;

            // 编辑状态：按键交给输入框，只保留「回车关闭编辑」和「Esc 退出」
            if (_editing)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    StopEditing(true);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StopEditing(true);
                    return;
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow)) MoveRow(1, true);
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveRow(-1, true);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveColumn(1, true);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveColumn(-1, true);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
            else if (Input.GetKeyDown(KeyCode.Escape)) GoBack();
        }

        private static bool RowHasAlive(Row row)
        {
            for (int i = 0; i < row.Items.Count; i++)
            {
                if (row.Items[i].IsAlive) return true;
            }
            return false;
        }

        /// <summary>上下：换组。跳过整体不可见的组，换组后停在该组上次的位置。</summary>
        private void MoveRow(int direction, bool announce)
        {
            if (_rows.Count == 0) return;

            int start = _rowIndex;
            for (int step = 0; step < _rows.Count; step++)
            {
                _rowIndex += direction;
                if (_rowIndex < 0) _rowIndex = _rows.Count - 1;
                if (_rowIndex >= _rows.Count) _rowIndex = 0;

                Row row = CurrentRow;
                if (row != null && RowHasAlive(row))
                {
                    _colIndex = ClampAlive(row, row.Last);
                    row.Last = _colIndex;
                    A11yNavItem item = Current;
                    if (item != null && announce) Announce(item);
                    return;
                }
                if (_rowIndex == start) break;
            }

            _rowIndex = start;
        }

        /// <summary>
        /// 左右：在组内换项。组里只有一项且是数值控件时直接改值，
        /// 这样「人数」这类孤立滑块不必再单独设计按键。
        /// </summary>
        private void MoveColumn(int direction, bool announce)
        {
            Row row = CurrentRow;
            if (row == null || row.Items.Count == 0) return;

            if (row.Items.Count == 1)
            {
                A11yNavItem only = row.Items[0];
                if (only != null && only.Kind == A11yItemKind.Slider && only.IsAlive)
                {
                    only.Step(direction);
                    if (announce) Announce(only);
                }
                return;
            }

            int start = _colIndex;
            for (int step = 0; step < row.Items.Count; step++)
            {
                _colIndex += direction;
                if (_colIndex < 0) _colIndex = row.Items.Count - 1;
                if (_colIndex >= row.Items.Count) _colIndex = 0;

                A11yNavItem item = row.Items[_colIndex];
                if (item != null && item.IsAlive)
                {
                    row.Last = _colIndex;
                    if (announce) Announce(item);
                    return;
                }
                if (_colIndex == start) break;
            }

            _colIndex = start;
        }

        /// <summary>把索引挪到该组里最近的可见项。</summary>
        private static int ClampAlive(Row row, int wanted)
        {
            if (wanted >= 0 && wanted < row.Items.Count && row.Items[wanted].IsAlive) return wanted;
            for (int i = 0; i < row.Items.Count; i++)
            {
                if (row.Items[i].IsAlive) return i;
            }
            return 0;
        }

        /// <summary>
        /// 朗读当前项。位置与总数取自清单自身，不涉及任何屏幕坐标。
        /// 是否真的出声由用户在游戏无障碍选项里的设置决定，这里不做干预。
        /// </summary>
        private void Announce(A11yNavItem item)
        {
            string text = item.GetSpeechText();
            if (string.IsNullOrEmpty(text)) return;
            if (!HasNarration) return;

            Row row = CurrentRow;
            int index = _colIndex + 1;
            int total = row != null ? row.Items.Count : 1;
            // 组名作为补充说明读出，让用户知道自己在哪一块
            string description = row != null && !string.IsNullOrEmpty(row.Name) ? row.Name : null;

            switch (item.Kind)
            {
                case A11yItemKind.Input:
                    Narration.Say(text, NarrationCategory.Navigation, index, total,
                        ElementType.Inputfield, null, description, false, NavigationActions());
                    break;
                case A11yItemKind.Button:
                    Narration.Say(text, NarrationCategory.Navigation, index, total,
                        ElementType.Button, null, description, false, NavigationActions());
                    break;
                case A11yItemKind.Toggle:
                    // 开关用 Selection 上报当前值，读法为「名称，开/关」
                    Narration.Say(text, NarrationCategory.Navigation, index, total,
                        ElementType.Selection, item.IsOn ? A11yPhrases.On : A11yPhrases.Off, description, false, NavigationActions());
                    break;
                default:
                    Narration.Say(text, NarrationCategory.Navigation, index, total,
                        ElementType.Label, null, description, false, NavigationActions());
                    break;
            }
        }

        private static Narration _narrationCache;

        /// <summary>
        /// 场景里是否有旁白组件。没有就静音运行，不报错，也不替用户改设置：
        /// 是否真的发声由用户的无障碍选项（Persistence.GetNarration）决定，
        /// 这里只调用 Narration.Say，它内部会按设置门控。
        /// </summary>
        private static bool HasNarration
        {
            get
            {
                if (_narrationCache == null) _narrationCache = Object.FindObjectOfType<Narration>();
                return _narrationCache != null;
            }
        }

        private static NarrationActionName[] NavigationActions()
        {
            return new[]
            {
                NarrationActionName.ToNavigateVertical,
                NarrationActionName.ToNavigateHorizontal,
                NarrationActionName.ToOpen,
                NarrationActionName.ToConfirm,
                NarrationActionName.ToGoBack,
                NarrationActionName.ForContextInstructions,
                NarrationActionName.ToRepeatLastMessage,
                NarrationActionName.ToToggleNarration
            };
        }

        /// <summary>朗读一条独立信息（错误提示、状态变化），不移动焦点。</summary>
        public static void SayInfo(string text, NarrationCategory category = NarrationCategory.Notification)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!HasNarration) return;
            Narration.Say(text, category, false);
        }

        private void Confirm()
        {
            A11yNavItem item = Current;
            if (item == null || !item.IsAlive) return;

            switch (item.Kind)
            {
                case A11yItemKind.Input:
                    BeginEditing(item);
                    break;
                case A11yItemKind.Toggle:
                    item.Flip();
                    // 翻转后立刻把新状态读出来，用户才能确认结果
                    SayInfo(item.GetSpeechText(), NarrationCategory.Navigation);
                    break;
                default:
                    item.Activate();
                    break;
            }
        }

        private void BeginEditing(A11yNavItem item)
        {
            var input = item.InputField;
            if (input == null || !input.interactable) return;

            _editing = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
            input.Select();

            SayInfo(A11yPhrases.Editing, NarrationCategory.Instruction);
        }

        private void StopEditing(bool announce)
        {
            if (!_editing) return;
            _editing = false;

            var input = Current != null ? Current.InputField : null;
            if (input != null)
            {
                input.DeactivateInputField();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                if (announce && Current != null)
                {
                    // 退出编辑后重读该项，内容变化能听出来
                    SayInfo(Current.GetSpeechText(), NarrationCategory.Navigation);
                }
            }
        }

        private void GoBack()
        {
            if (BackAction != null)
            {
                BackAction();
                return;
            }
            SayInfo(A11yPhrases.CannotGoBack, NarrationCategory.Notification);
        }

        /// <summary>外部（如网络回调）通知某处发生了变化，重读当前项。</summary>
        public void ReannounceCurrent()
        {
            A11yNavItem item = Current;
            if (item == null || !item.IsAlive) return;
            Announce(item);
        }

        /// <summary>外部通知某项文字变了，若正停在它上面就重读。</summary>
        public void ReannounceIfCurrent(UnityEngine.Component root)
        {
            if (root == null) return;
            A11yNavItem item = Current;
            if (item != null && item.Root == root.gameObject) Announce(item);
        }
    }
}
