using System;

[Serializable]
public class NikkeSaveData
{
    public string nikkeId;
    public string nameOverride;
    public int level;
    public int exp;
    public int weaponLevel;
    public int armorLevel;
    public int[] skillLevels;
    public int[] activeSkillIndices;
    public int[] activeCampSkillIndices;
    public string[] trinketNames;
    public string[] posQuirkNames;
    public string[] negQuirkNames;
    public string[] diseaseNames;
}
