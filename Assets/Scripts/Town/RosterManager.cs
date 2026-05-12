using UnityEngine;
using System.Collections.Generic;
public class RosterManager : Singleton<RosterManager>
{
    [SerializeField] private NikkeData[] m_InitialRoster;
    private List<NikkeInstance> m_Roster;

    public IReadOnlyList<NikkeInstance> Roster => m_Roster;
    protected override void Awake()
    {
        base.Awake();
        m_Roster = new List<NikkeInstance>();
        GameSaveData save = SaveSystem.Load();
        if (save != null) LoadFromSave(save);
        else InitDefault();
    }
    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }
    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }


    private void OnGameStateChanged(GameState previous, GameState current)
    {
        if (current == GameState.Town)
            SaveGame();
    }
    private void InitDefault()
    {
        for (int i = 0; i < m_InitialRoster.Length; ++i)
        {
            if (m_InitialRoster[i] == null) continue;
            m_Roster.Add(new NikkeInstance(m_InitialRoster[i]));
        }
    }
    private void LoadFromSave(GameSaveData save)
    {
        if (save?.roster?.nikkes == null) { InitDefault(); return; }
        for (int i = 0; i < save.roster.nikkes.Length; ++i)
        {
            NikkeSaveData nd = save.roster.nikkes[i];
            if (string.IsNullOrEmpty(nd.nikkeId)) continue;
            NikkeData data = DataManager.Instance.GetNikkeData(nd.nikkeId);
            if (data == null) continue;
            m_Roster.Add(new NikkeInstance(data, nd));
        }
        ResourceManager.Instance.LoadFromSave(save);
    }
    public void SaveGame()
    {
        GameSaveData save = new GameSaveData();
        save.roster = new RosterSaveData();
        save.roster.nikkes = new NikkeSaveData[m_Roster.Count];
        for (int i = 0; i < m_Roster.Count; ++i)
            save.roster.nikkes[i] = m_Roster[i].ToSaveData();

        ResourceManager.Instance.FillSaveData(save);  

        SaveSystem.Save(save);
    }
}
