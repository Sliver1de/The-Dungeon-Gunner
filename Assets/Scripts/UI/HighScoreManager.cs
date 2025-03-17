using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class HighScoreManager : SingletonMonobehaviour<HighScoreManager>
{
    private HighScores highScores = new HighScores();

    protected override void Awake()
    {
        base.Awake();

        // ResetHighScores();
        LoadScores();
    }

    /// <summary>
    /// Load Scores From Disk       从磁盘加载分数
    /// </summary>
    private void LoadScores()
    {
        BinaryFormatter bf = new BinaryFormatter();

        if (File.Exists(Application.persistentDataPath + "/DungeonGunnerHighScores.dat"))
        {
            ClearScoreList();

            FileStream file = File.OpenRead(Application.persistentDataPath + "/DungeonGunnerHighScores.dat");

            highScores = (HighScores)bf.Deserialize(file);
            
            file.Close();
        }
    }

    /// <summary>
    /// Clear All Scores    清除所有分数
    /// </summary>
    private void ClearScoreList()
    {
        highScores.scoreList.Clear();
    }

    public void AddScore(Score score, int rank)
    {
        highScores.scoreList.Insert(rank - 1, score);
        
        //Maintain the maximum number of scores to save     保持要保存的最高分数数量上限
        if (highScores.scoreList.Count > Settings.numberOfHighScoresToSave)
        {
            highScores.scoreList.RemoveAt(Settings.numberOfHighScoresToSave);
        }

        SaveScores();
    }

    /// <summary>
    /// Save Scores To Disk     将分数保存到磁盘
    /// </summary>
    public void SaveScores()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/DungeonGunnerHighScores.dat");
        bf.Serialize(file, highScores);
        file.Close();
    }

    /// <summary>
    /// Get highscores  获取最高分
    /// </summary>
    /// <returns></returns>
    public HighScores GetHighScores()
    {
        return highScores;
    }

    /// <summary>
    /// Return the rank of the playerScore compared to the other high scores
    /// (returns 0 if the score isn't higher than any in the high scores list)
    /// 返回 playerScore 相对于其他高分的排名（如果分数未超过高分列表中的任何分数，则返回 0）
    /// </summary>
    /// <param name="playerScore"></param>
    /// <returns></returns>
    public int GetRank(long playerScore)
    {
        //If there are no scores currently in the list - then this score must be ranked 1 - then return
        //如果当前列表中没有分数，则此分数必须排名第一，然后返回
        if (highScores.scoreList.Count == 0) return 1;

        int index = 0;
        
        //Loop through scores in list to find the rank of this score    遍历列表中的分数，以确定此分数的排名
        for (int i = 0; i < highScores.scoreList.Count; i++)
        {
            index++;
            if (playerScore >= highScores.scoreList[i].playerScore)
            {
                return index;
            }
        }

        if (highScores.scoreList.Count < Settings.numberOfHighScoresToSave)
        {
            return index + 1;
        }

        return 0;
    }
    
    public void ResetHighScores()
    {
        highScores.scoreList.Clear(); // 清空当前排行榜
        SaveScores(); // 重新保存空排行榜
    }
}
