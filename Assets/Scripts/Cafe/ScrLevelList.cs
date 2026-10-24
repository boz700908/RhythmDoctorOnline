using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RhythmCafe.Level;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using RDOnline.ScnLobby;

public class ScrLevelList : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInput;
    public Transform cardContainer;
    public GameObject cardPrefab;
    public ScrollRect scrollRect;

    [Header("Settings")]
    public int perPage = 15;
    public float searchDelay = 0.5f;
    public float loadMoreThreshold = 0.1f;
    public int instantiatePerFrame = 2;
    public bool useDomesticApi = true;

    // 地址统一由 rdol.config.json 提供（ModConfig.ChartListUrl）。
    // 原来的两个源都已失效：cafe.rhythmdoctor.top 返回 502，orchardb.fly.dev DNS 已不存在。
    // 自建服务与官方上游 rhythm.cafe 同步，所以国内外不再分开，两条路都指向它。
    private const string API_KEY = "nicolebestgirl";

    private List<GameObject> cardInstances = new List<GameObject>();
    private Queue<GameObject> cardPool = new Queue<GameObject>();
    private Coroutine searchCoroutine;
    private Coroutine populateCoroutine;
    private int currentPage = 1;
    private int totalFound = 0;
    private bool isLoading = false;
    private bool hasMoreData = true;
    private string currentQuery = "";

    /// <summary>当前来源。社区谱面走网络，另外两个走本地。</summary>
    private ChartSourceKind _source = ChartSourceKind.Community;

    /// <summary>谱面预览组件。切换来源时要清掉它里面的旧选择。</summary>
    private RDOnline.Component.ChartPreview _preview;

    public ChartSourceKind Source => _source;

    void Start()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        // 谱面面板就是挂着本组件的节点，标签条贴在它顶行的空档里
        var panel = transform as RectTransform;
        var tabs = panel != null ? ChartSourceTabs.Attach(this, panel) : null;

        // 谱面面板的无障碍导航：来源标签、谱面列表、操作按钮三组
        _preview = FindChartPreview();
        if (tabs != null)
            RDOnline.A11y.ChartPanelA11y.Attach(this, tabs, _preview);

        Search("");
    }

    private void OnScrollChanged(Vector2 position)
    {
        // 本地来源没有分页，不触发加载更多
        if (_source != ChartSourceKind.Community) return;
        if (isLoading || !hasMoreData) return;

        if (position.y <= loadMoreThreshold)
        {
            LoadMore();
        }
    }

    private void OnSearchChanged(string value)
    {
        if (searchCoroutine != null)
            StopCoroutine(searchCoroutine);

        searchCoroutine = StartCoroutine(SearchWithDelay(value));
    }

    private IEnumerator SearchWithDelay(string query)
    {
        yield return new WaitForSeconds(searchDelay);
        Search(query);
    }

    public void Search(string query)
    {
        if (populateCoroutine != null)
            StopCoroutine(populateCoroutine);

        currentQuery = query;
        currentPage = 1;
        hasMoreData = true;
        RecycleAllCards();

        if (_source == ChartSourceKind.Community)
            StartCoroutine(FetchLevels(query, 1, false));
        else
            StartCoroutine(PopulateLocalAsync(query));
    }

    /// <summary>
    /// 切换谱面来源。列表整块重建，旧卡片全部回收，
    /// 分页/滚动/搜索状态一并归零，不残留上一个来源的内容。
    /// </summary>
    public void SetSource(ChartSourceKind source)
    {
        if (_source == source) return;
        _source = source;

        // 中断所有进行中的任务，避免网络返回后把新来源的列表覆盖掉
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        if (populateCoroutine != null)
        {
            StopCoroutine(populateCoroutine);
            populateCoroutine = null;
        }
        StopAllCoroutines();

        isLoading = false;
        hasMoreData = true;
        currentPage = 1;
        totalFound = 0;

        // 换来源等于换了整套谱面，旧的选择必须清掉，否则预览还挂着上一个来源的谱面
        if (_preview != null)
            _preview.ResetSelection();

        // 社区选择是全局状态，切到本地来源时一并清掉，防止 Update 把它又同步回来
        if (source != ChartSourceKind.Community)
            SelectedLevel.Set(null);

        // 搜索框只对社区谱面有意义，本地来源没有搜索后端
        if (searchInput != null)
        {
            searchInput.gameObject.SetActive(source == ChartSourceKind.Community);
            if (source != ChartSourceKind.Community)
            {
                searchInput.SetTextWithoutNotify("");
                currentQuery = "";
            }
        }

        // 回到列表顶部，否则新来源从上一个来源的滚动位置开始
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        RecycleAllCards();

        if (source == ChartSourceKind.Community)
            StartCoroutine(FetchLevels(currentQuery, 1, false));
        else
            StartCoroutine(PopulateLocalAsync(currentQuery));
    }

    private void LoadMore()
    {
        if (isLoading || !hasMoreData) return;
        currentPage++;
        StartCoroutine(FetchLevels(currentQuery, currentPage, true));
    }

    // ---------- 社区谱面（原逻辑保持不动） ----------

    private IEnumerator FetchLevels(string query, int page, bool append)
    {
        isLoading = true;
        ScrAlert.Show("加载中...", false);

        string queryBy = "song,authors,artist,tags,description";
        string queryByWeights = "12,8,6,5,4";
        string facetBy = "authors,tags,source,difficulty,artist";
        string maxFacetValues = "10";
        string numTypos = "2,1,1,1,0";
        string sortBy = "_text_match:desc,indexed:desc,last_updated:desc";
        string filterBy = "approval:=[-1..20]";

        bool useDomestic = useDomesticApi;
        string baseUrl = RDOnline.ModConfig.ChartListUrl;

        string url = $"{baseUrl}?q={UnityEngine.Networking.UnityWebRequest.EscapeURL(query)}" +
            $"&page={page}" +
            $"&per_page={perPage}" +
            $"&query_by={UnityEngine.Networking.UnityWebRequest.EscapeURL(queryBy)}" +
            $"&query_by_weights={UnityEngine.Networking.UnityWebRequest.EscapeURL(queryByWeights)}" +
            $"&facet_by={UnityEngine.Networking.UnityWebRequest.EscapeURL(facetBy)}" +
            $"&max_facet_values={maxFacetValues}" +
            $"&num_typos={UnityEngine.Networking.UnityWebRequest.EscapeURL(numTypos)}" +
            $"&sort_by={UnityEngine.Networking.UnityWebRequest.EscapeURL(sortBy)}" +
            $"&filter_by={UnityEngine.Networking.UnityWebRequest.EscapeURL(filterBy)}";

        var request = UnityEngine.Networking.UnityWebRequest.Get(url);

        // 国外 API 需要添加 API Key
        if (!useDomestic)
        {
            request.SetRequestHeader("x-typesense-api-key", API_KEY);
        }
        yield return request.SendWebRequest();

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API Error: {request.error}");
            request.Dispose();
            isLoading = false;
            yield break;
        }

        var response = JsonConvert.DeserializeObject<LevelSearchResponse>(request.downloadHandler.text);
        request.Dispose();

        if (response != null)
        {
            totalFound = response.found;

            if (response.hits != null && response.hits.Count > 0)
            {
                populateCoroutine = StartCoroutine(PopulateCardsAsync(response.hits, append));
                yield return populateCoroutine;
                hasMoreData = cardInstances.Count < totalFound;
            }
            else
            {
                hasMoreData = false;
            }
        }

        isLoading = false;
        ScrAlert.Hide();
    }

    private IEnumerator PopulateCardsAsync(List<LevelHit> hits, bool append)
    {
        if (!append)
        {
            RecycleAllCards();
        }

        int count = 0;
        foreach (var hit in hits)
        {
            if (hit.document == null) continue;

            var cardObj = GetCardFromPool();
            var card = cardObj.GetComponent<ScrLevelCard>();

            if (card != null)
            {
                card.SetData(hit.document);
            }

            cardInstances.Add(cardObj);

            count++;
            if (count >= instantiatePerFrame)
            {
                count = 0;
                yield return null;
            }
        }
    }

    // ---------- 本地来源（官方关卡 / 玩家关卡库） ----------

    private IEnumerator PopulateLocalAsync(string query)
    {
        isLoading = true;

        List<ChartSourceEntry> entries = _source == ChartSourceKind.Official
            ? ChartSource.GetOfficialLevels()
            : ChartSource.GetLibraryLevels();

        // 本地列表也支持按显示名过滤，搜索框在本地来源下是隐藏的，这里主要防外部调用
        if (!string.IsNullOrEmpty(query))
        {
            string q = query.Trim();
            entries = entries.FindAll(e =>
                (!string.IsNullOrEmpty(e.Display) && e.Display.IndexOf(q, System.StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(e.Detail) && e.Detail.IndexOf(q, System.StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        totalFound = entries.Count;
        hasMoreData = false;

        int count = 0;
        foreach (var entry in entries)
        {
            var cardObj = GetCardFromPool();
            var card = cardObj.GetComponent<ScrLevelCard>();
            if (card != null)
            {
                ChartSourceEntry captured = entry;
                card.SetEntry(captured, OnEntryClicked);
            }

            cardInstances.Add(cardObj);

            count++;
            if (count >= instantiatePerFrame)
            {
                count = 0;
                yield return null;
            }
        }

        isLoading = false;
        ScrAlert.Hide();
    }

    private void OnEntryClicked(ChartSourceEntry entry)
    {
        if (entry == null) return;

        var preview = _preview != null ? _preview : FindChartPreview();
        if (preview == null)
        {
            Debug.LogWarning("[ScrLevelList] 找不到谱面预览组件，无法应用所选条目");
            return;
        }

        if (entry.IsOfficial)
            preview.SelectOfficialLevel(entry.Id);
        else
            preview.SelectLibraryLevel(entry.Directory);
    }

    private RDOnline.Component.ChartPreview FindChartPreview()
    {
        // 预览组件与列表同在创建房间面板里，从上级找，范围小且有界
        Transform root = transform.parent;
        for (int i = 0; i < 4 && root != null; i++)
        {
            var preview = root.GetComponentInChildren<RDOnline.Component.ChartPreview>(true);
            if (preview != null) return preview;
            root = root.parent;
        }
        return null;
    }

    // ---------- 卡片池 ----------

    private GameObject GetCardFromPool()
    {
        GameObject cardObj;
        if (cardPool.Count > 0)
        {
            cardObj = cardPool.Dequeue();
            cardObj.SetActive(true);
            cardObj.transform.SetAsLastSibling();
        }
        else
        {
            cardObj = Instantiate(cardPrefab, cardContainer);
        }
        return cardObj;
    }

    private void RecycleAllCards()
    {
        foreach (var card in cardInstances)
        {
            if (card != null)
            {
                card.SetActive(false);
                cardPool.Enqueue(card);
            }
        }
        cardInstances.Clear();
    }
}
