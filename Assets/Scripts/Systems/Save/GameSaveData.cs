using System;

[Serializable]
public class RosterSaveData
{
    public NikkeSaveData[] nikkes;
}
[Serializable]
public class GameSaveData
{
    public RosterSaveData roster;
    public int credit;
    public int battleData;
    public int core;
    public int gems;
    public int[] relics;
}
