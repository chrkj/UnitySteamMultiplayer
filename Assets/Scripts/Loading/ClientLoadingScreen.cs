using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ClientLoadingScreen : MonoBehaviour
{
    protected class LoadingProgressBar
    {
        public Text NameText { get; set; }
        public Text ProgressText { get; set; }
        public Slider ProgressBar { get; set; }

        public LoadingProgressBar(Slider otherPlayerProgressBar, Text otherPlayerNameText, Text progressText)
        {
            ProgressBar = otherPlayerProgressBar;
            NameText = otherPlayerNameText;
            ProgressText = progressText;
        }

        public void UpdateProgress(float value, float newValue)
        {
            ProgressBar.value = newValue;
            ProgressText.text = $"{((int)(newValue * 100))} %";
        }
    }

    private bool m_LoadingScreenRunning;
    private Coroutine m_FadeOutCoroutine;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private float m_FadeOutDuration = 0.1f;
    [SerializeField] private float m_DelayBeforeFadeOut = 0.5f;
    [SerializeField] private Text m_ProgressText;
    [SerializeField] private Slider m_ProgressBar;
    [SerializeField] private Text m_SceneName;
    [SerializeField] private List<Text> m_OtherPlayerNamesTexts;
    [SerializeField] private List<Text> m_OtherPlayerProgressTexts;
    [SerializeField] private List<Slider> m_OtherPlayersProgressBars;
    
    [SerializeField] protected LoadingProgressManager m_LoadingProgressManager;
    protected readonly Dictionary<ulong, LoadingProgressBar> m_LoadingProgressBars = new Dictionary<ulong, LoadingProgressBar>();

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Assert.AreEqual(m_OtherPlayersProgressBars.Count, m_OtherPlayerNamesTexts.Count, "There should be the same number of progress bars, name labels and progress texts");
        Assert.AreEqual(m_OtherPlayersProgressBars.Count, m_OtherPlayerProgressTexts.Count, "There should be the same number of progress bars, name labels and progress texts");
    }

    private void Start()
    {
        m_CanvasGroup.alpha = 0;
        m_LoadingProgressManager.onTrackersUpdated += OnProgressTrackersUpdated;
    }

    private void OnDestroy()
    {
        m_LoadingProgressManager.onTrackersUpdated -= OnProgressTrackersUpdated;
    }

    private void Update()
    {
        if (!m_LoadingScreenRunning) return;
        
        m_ProgressBar.value = m_LoadingProgressManager.LocalProgress;
        m_ProgressText.text = $"{(int)(m_ProgressBar.value * 100)} %";
    }

    private void OnProgressTrackersUpdated()
    {
        var clientIdsToRemove = new List<ulong>();
        foreach (var clientId in m_LoadingProgressBars.Keys)
        {
            if (!m_LoadingProgressManager.ProgressTrackers.ContainsKey(clientId))
                clientIdsToRemove.Add(clientId);
        }

        foreach (var clientId in clientIdsToRemove)
            RemoveOtherPlayerProgressBar(clientId);
        
        foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
        {
            var clientId = progressTracker.Key;
            if (clientId != NetworkManager.Singleton.LocalClientId && !m_LoadingProgressBars.ContainsKey(clientId))
                AddOtherPlayerProgressBar(clientId, progressTracker.Value);
        }
    }

    public void StopLoadingScreen()
    {
        if (!m_LoadingScreenRunning) return;
        
        if (m_FadeOutCoroutine != null)
            StopCoroutine(m_FadeOutCoroutine);
        m_FadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    public void StartLoadingScreen(string sceneName)
    {
        m_CanvasGroup.alpha = 1;
        m_LoadingScreenRunning = true;
        UpdateLoadingScreen(sceneName);
        ReinitializeProgressBars();
    }

    private void ReinitializeProgressBars()
    {
        var clientIdsToRemove = new List<ulong>();
        foreach (var clientId in m_LoadingProgressBars.Keys)
        {
            if (!m_LoadingProgressManager.ProgressTrackers.ContainsKey(clientId))
                clientIdsToRemove.Add(clientId);
        }

        foreach (var clientId in clientIdsToRemove)
            RemoveOtherPlayerProgressBar(clientId);

        for (var i = 0; i < m_OtherPlayersProgressBars.Count; i++)
        {
            m_OtherPlayerNamesTexts[i].gameObject.SetActive(false);
            m_OtherPlayersProgressBars[i].gameObject.SetActive(false);
            m_OtherPlayerProgressTexts[i].gameObject.SetActive(false);
        }

        var index = 0;
        foreach (var progressTracker in m_LoadingProgressManager.ProgressTrackers)
        {
            var clientId = progressTracker.Key;
            if (clientId != NetworkManager.Singleton.LocalClientId)
                UpdateOtherPlayerProgressBar(clientId, index++);
        }
    }

    protected virtual void UpdateOtherPlayerProgressBar(ulong clientId, int progressBarIndex)
    {
        m_LoadingProgressBars[clientId].ProgressBar = m_OtherPlayersProgressBars[progressBarIndex];
        m_LoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
        
        m_LoadingProgressBars[clientId].NameText = m_OtherPlayerNamesTexts[progressBarIndex];
        m_LoadingProgressBars[clientId].NameText.gameObject.SetActive(true);
        
        m_LoadingProgressBars[clientId].ProgressText = m_OtherPlayerProgressTexts[progressBarIndex];
        m_LoadingProgressBars[clientId].ProgressText.gameObject.SetActive(true);
    }

    protected virtual void AddOtherPlayerProgressBar(ulong clientId, NetworkTrackerLoadingProgress progressTracker)
    {
        if (m_LoadingProgressBars.Count >= m_OtherPlayersProgressBars.Count || m_LoadingProgressBars.Count >= m_OtherPlayerNamesTexts.Count)
            throw new Exception("There are not enough progress bars to track the progress of all the players.");
        
        var index = m_LoadingProgressBars.Count;
        m_LoadingProgressBars[clientId] = new LoadingProgressBar(m_OtherPlayersProgressBars[index], m_OtherPlayerNamesTexts[index], m_OtherPlayerProgressTexts[index]);
        progressTracker.Progress.OnValueChanged += m_LoadingProgressBars[clientId].UpdateProgress;
        
        m_LoadingProgressBars[clientId].ProgressBar.value = progressTracker.Progress.Value;
        m_LoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
        
        m_LoadingProgressBars[clientId].NameText.text = $"Client {clientId}";
        m_LoadingProgressBars[clientId].NameText.gameObject.SetActive(true);
        
        m_LoadingProgressBars[clientId].ProgressText.text = $"{((int)(progressTracker.Progress.Value * 100))} %";
        m_LoadingProgressBars[clientId].ProgressText.gameObject.SetActive(true);
    }

    private void RemoveOtherPlayerProgressBar(ulong clientId, NetworkTrackerLoadingProgress progressTracker = null)
    {
        if (progressTracker != null)
            progressTracker.Progress.OnValueChanged -= m_LoadingProgressBars[clientId].UpdateProgress;
        
        m_LoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(false);
        m_LoadingProgressBars[clientId].NameText.gameObject.SetActive(false);
        m_LoadingProgressBars[clientId].ProgressText.gameObject.SetActive(false);
        m_LoadingProgressBars.Remove(clientId);
    }

    public void UpdateLoadingScreen(string sceneName)
    {
        if (!m_LoadingScreenRunning) return;
        
        m_SceneName.text = sceneName;
        if (m_FadeOutCoroutine != null)
            StopCoroutine(m_FadeOutCoroutine);
    }

    private IEnumerator FadeOutCoroutine()
    {
        yield return new WaitForSeconds(m_DelayBeforeFadeOut);
        m_LoadingScreenRunning = false;

        float currentTime = 0;
        while (currentTime < m_FadeOutDuration)
        {
            m_CanvasGroup.alpha = Mathf.Lerp(1, 0, currentTime / m_FadeOutDuration);
            yield return null;
            currentTime += Time.deltaTime;
        }
        m_CanvasGroup.alpha = 0;
    }
    
}