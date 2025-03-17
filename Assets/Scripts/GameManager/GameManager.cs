using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameManager : SingletonMonobehaviour<GameManager>
{
    #region Header GAMEOBJECT REFERENCES

    [Space(10)]
    [Header("GAME OBJECT REFERENCES")]

    #endregion

    #region Tooltip
    //在层级中填充暂停菜单游戏对象
    [Tooltip("Populate with pause menu gameobject in hierarchy")]

    #endregion
    
    [SerializeField]
    private GameObject pauseMenu;
    
    #region Tooltip
    //在 FadeScreenUI 中填充 MessageText 的 TextMeshPro 组件
    [Tooltip("Populate with the MessageText textmeshpro component in the FadeScreenUI")]

    #endregion

    [SerializeField]
    private TextMeshProUGUI messageTextTMP;

    #region Tooltip
    //在 FadeScreenUI 中填充 FadeImage 的 CanvasGroup 组件
    [Tooltip("Populate with the FadeImage canvasgroup component in the FadeScreenUI")]

    #endregion

    [SerializeField]
    private CanvasGroup canvasGroup;

    #region Header UI

    [Space(10)]
    [Header("UI REFERENCES")]

    #endregion
    
    [SerializeField]
    private Button pauseButton;
    
    #region Header DUNGEON LEVELS

    [Space(10)]
    [Header("Dungeon Levels")]

    #endregion

    #region Tooltip
    //填充地牢关卡的可编程对象（Scriptable Objects）
    [Tooltip("Populate with the dungeon level scriptable objects")]

    #endregion

    [SerializeField]
    private List<DungeonLevelSO> dungeonLevelList;

    #region Tootip
    //填充用于测试的起始地下城级别，第一级别 = 0
    [Tooltip("Populate with the starting dungeon level for testing, first level = 0")]

    #endregion

    [SerializeField]
    private int currentDungeonLevelListIndex = 0;
    private Room currentRoom;
    private Room previousRoom;
    private PlayerDetailsSO playerDetails;
    private Player player;

    [HideInInspector] public GameState gameState;
    [HideInInspector] public GameState previousGameState;
    private long gameScore;
    private int scoreMultiplier;
    private InstantiatedRoom bossRoom;
    private bool isFading = false;

    protected override void Awake()
    {
        base.Awake();
        
        //Set player details - saved in current player scriptable object from the main menu
        //设置玩家详情 —— 从主菜单保存到当前玩家的 ScriptableObject
        playerDetails = GameResources.Instance.currentPlayer.playerDetails;
        
        //Instantiate player
        InstantiatePlayer();
    }

    /// <summary>
    /// Create player in scene at position 在场景中指定位置创建玩家
    /// </summary>
    private void InstantiatePlayer()
    {
        //Instantiate player
        GameObject playerGameObject = Instantiate(playerDetails.playerPrefab);
        //Debug.Log(playerGameObject.name.ToString());
        
        //Initialize player
        player = playerGameObject.GetComponent<Player>();
        
        player.Initialize(playerDetails);
    }

    private void OnEnable()
    {
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;

        StaticEventHandler.OnRoomEnemiesDefeated += StaticEventHandler_OnRoomEnemiesDefeated;

        StaticEventHandler.OnPointsScored += StaticEventHandler_OnPointsScored;

        StaticEventHandler.OnMultiplier += StaticEventHandler_OnMultiplier;

        player.destroyedEvent.OnDestroyed += Player_OnDestroyed;
    }

    private void OnDisable()
    {
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
        
        StaticEventHandler.OnRoomEnemiesDefeated -= StaticEventHandler_OnRoomEnemiesDefeated;

        StaticEventHandler.OnPointsScored -= StaticEventHandler_OnPointsScored;

        StaticEventHandler.OnMultiplier -= StaticEventHandler_OnMultiplier;
        
        player.destroyedEvent.OnDestroyed -= Player_OnDestroyed;
    }

    /// <summary>
    /// Handle room changed event   处理房间变化事件
    /// </summary>
    /// <param name="roomChangedEventArgs"></param>
    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        SetCurrentRoom(roomChangedEventArgs.room);
    }

    /// <summary>
    /// Handle room enemies defeated event  处理房间内敌人被击败事件
    /// </summary>
    /// <param name="roomEnemiesDefeatedArgs"></param>
    private void StaticEventHandler_OnRoomEnemiesDefeated(RoomEnemiesDefeatedArgs roomEnemiesDefeatedArgs)
    {
        RoomEnemiesDefeated();
    }

    /// <summary>
    /// Handle points scored event
    /// </summary>
    /// <param name="pointsScoredArgs"></param>
    private void StaticEventHandler_OnPointsScored(PointsScoredArgs pointsScoredArgs)
    {
        //Trigger score
        gameScore += pointsScoredArgs.points * scoreMultiplier;
        
        //Call score changed event
        StaticEventHandler.CallScoreChangedEvent(gameScore, scoreMultiplier);
    }

    /// <summary>
    /// Handle score multiplier event
    /// </summary>
    /// <param name="multiplierArgs"></param>
    private void StaticEventHandler_OnMultiplier(MultiplierArgs multiplierArgs)
    {
        if (multiplierArgs.multiplier)
        {
            scoreMultiplier++;
        }
        else
        {
            scoreMultiplier--;
        }
        
        //clame between 1 and 30
        scoreMultiplier = Mathf.Clamp(scoreMultiplier, 1, 30);
        
        //Call score changed event
        StaticEventHandler.CallScoreChangedEvent(gameScore, scoreMultiplier);
    }

    /// <summary>
    /// Handle player destroyed event   处理玩家被销毁事件
    /// </summary>
    /// <param name="destroyedEvent"></param>
    /// <param name="destroyedEventArgs"></param>
    private void Player_OnDestroyed(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
    {
        previousGameState = gameState;
        gameState = GameState.gameLost;
    }

    void Start()
    {
        previousGameState = GameState.gameStarted;
        gameState = GameState.gameStarted;
        
        //Set score to zero
        gameScore = 0;
        
        //Set multiplier to 1
        scoreMultiplier = 1;
        
        //Set screen to black
        StartCoroutine(Fade(0f, 1f, 0f, Color.black));

        pauseButton.onClick.AddListener(PauseGameMenu);
    }

    void Update()
    {
        HandleGameState();

        //for testing rebuilding the dungeon
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            gameState = GameState.gameStarted;
        }
    }

    /// <summary>
    /// Handle game state   处理游戏状态
    /// </summary>
    private void HandleGameState()
    {
        //Handle game state
        switch (gameState)
        {
            case GameState.gameStarted:
                //Play first level
                PlayDungeonLevel(currentDungeonLevelListIndex);
                
                gameState = GameState.playingLevel;
                
                //Trigger room enemies defeated since we start in the entrance where there are no enemies
                //(just in case you have a level with just a boss room!)
                //触发房间敌人被击败事件，因为我们从入口开始，那里没有敌人（以防有一个只有 boss 房间的关卡）
                RoomEnemiesDefeated();
                
                break;
            
            //While playing the level handle the tap key for the dungeon overview map   在游戏进行时处理地下城概览地图的tap点击键
            case GameState.playingLevel:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    DisplayDungeonOverviewMap();
                }
                
                break;
            
            case GameState.engagingEnemies:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }
                
                break;

            //if in the dungeon overview map handle the release of the tap key to clear the map
            //如果在地下城概览地图中，处理tap点击键释放以清除地图
            case GameState.dungeonOverviewMap:
                
                //Key released
                if (Input.GetKeyUp(KeyCode.Tab))
                {
                    //Clear dungeonOverviewMap  清除地下城概览地图
                    DungeonMap.Instance.ClearDungeonOverViewMap();
                }
                
                break;
            
            //While playing the level and before the boss is engaged, handle the tap key for the dungeon overview map
            //在关卡进行中且未与Boss对战之前，处理地下城概览地图的点击键
            case GameState.bossStage:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    DisplayDungeonOverviewMap();
                }
                
                break;
            
            case GameState.engagingBoss:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                break;

            //handle the level being completed      处理关卡完成的情况
            case GameState.levelCompleted:
                
                //Display level completed text      显示关卡完成文本
                StartCoroutine(LevelCompleted());
                break;
            
            //handle the game being won (only trigger this once - test the previous game state to do this)
            //处理游戏胜利（仅触发一次 - 测试先前的游戏状态以进行此操作）
            case GameState.gameWon:

                if (previousGameState != GameState.gameWon)
                {
                    StartCoroutine(GameWon());
                }
                break;
            
            //handle the game being lost (only trigger this once - test the previous game state to do this)
            //处理游戏失败（仅触发一次 - 测试先前的游戏状态以进行此操作）
            case GameState.gameLost:

                if (previousGameState != GameState.gameLost)
                {
                    //Prevent message if you clear the level just as you get killed     如果在你被击败的同时清除关卡，防止显示消息
                    StopAllCoroutines();
                    StartCoroutine(GameLost());
                }
                break;
            
            case GameState.restartGame:

                RestartGame();
                break;
            
            //if the game is paused and the pause menu showing, then pressing escape again will clear the pause menu
            case GameState.gamePaused:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                break;
        }
    }

    /// <summary>
    /// Set the current room the player to in   设置玩家当前所在的房间
    /// </summary>
    /// <param name="room"></param>
    public void SetCurrentRoom(Room room)
    {
        previousRoom = currentRoom;
        currentRoom = room;
        
        //debug
        //Debug.Log(room.prefab.name.ToString());
    }

    /// <summary>
    /// Room enemies defeated - test if all dungeon rooms have been cleared of enemies - if so load next dungeon game level
    /// 房间敌人被击败 - 检测是否所有地牢房间的敌人都已清除 - 如果是，则加载下一个地牢游戏关卡
    /// </summary>
    private void RoomEnemiesDefeated()
    {
        //Initialise dungeon as being cleared - but then test each room     将地牢初始化为已清除状态，但随后测试每个房间
        bool isDungeonClearOfRegularEnemies = true;
        bossRoom = null;
        
        //Loop through all dungeon rooms to see if cleared of enemies   循环遍历所有地牢房间，以检查是否清除了敌人
        foreach (KeyValuePair<string, Room> keyValuePair in DungeonBuilder.Instance.dungeonBuilderRoomDictionary)
        {
            //skip boss room for time being     暂时跳过Boss房间
            if (keyValuePair.Value.roomNodeType.isBossRoom)
            {
                bossRoom = keyValuePair.Value.instantiatedRoom;
                continue;
            }
            
            //check if other rooms have been cleared of enemies     检查其他房间是否已清除敌人
            if (!keyValuePair.Value.isClearedOfEnemies)
            {
                isDungeonClearOfRegularEnemies = false;
                break;
            }
        }
        
        //Set game state
        //If dungeon level completely cleared (i.e. dungeon cleared apart from boss
        //and there is no boss room OR dungeon cleared apart from boss and boss room is also cleared)
        //如果地牢等级已完全清除（即地牢已清除，仅剩Boss，并且没有Boss房间，或者地牢已清除，仅剩Boss，并且Boss房间也已清除）
        if ((isDungeonClearOfRegularEnemies && bossRoom == null) ||
            (isDungeonClearOfRegularEnemies && bossRoom.room.isClearedOfEnemies))
        {
            //Are there more dungeon levels then    是否还有更多的地下城关卡？
            if (currentDungeonLevelListIndex < dungeonLevelList.Count - 1)
            {
                gameState = GameState.levelCompleted;
            }
            else
            {
                gameState = GameState.gameWon;
            }
        }
        //else if dungeon level cleared apart from boss room    否则，如果地下城关卡除了 boss 房间外都已清除
        else if (isDungeonClearOfRegularEnemies)
        {
            gameState = GameState.bossStage;

            StartCoroutine(BossStage());
        }
    }

    /// <summary>
    /// Pause game menu - also called from resume game button on pause menu     暂停游戏菜单——也可从暂停菜单中的“继续游戏”按钮调用
    /// </summary>
    public void PauseGameMenu()
    {
        if (gameState != GameState.gamePaused)
        {
            pauseMenu.SetActive(true);
            GetPlayer().playerControl.DisablePlayer();
            
            //Set game state
            previousGameState = gameState;
            gameState = GameState.gamePaused;
        }
        else if (gameState == GameState.gamePaused)
        {
            pauseMenu.SetActive(false);
            GetPlayer().playerControl.EnablePlayer();
            
            //Set game state
            gameState = previousGameState;
            previousGameState = GameState.gamePaused;
        }
        
        pauseButton.interactable = false; // 先禁用
        pauseButton.interactable = true;  // 再启用
    }

    /// <summary>
    /// Enter boss stage
    /// </summary>
    /// <returns></returns>
    private IEnumerator BossStage()
    {
        //Activate boss room    激活 boss 房间
        bossRoom.gameObject.SetActive(true);
        
        //Unlock boss room
        bossRoom.UnlockDoors(0f);
        
        //Wait 2 seconds
        yield return new WaitForSeconds(2f);
        
        //Fade in canvas to display text message    淡入画布以显示文本消息
        yield return StartCoroutine(Fade(0f, 1f, 2f, new Color(0f, 0f, 0f, 0.4f)));
        
        //Display boss message
        yield return StartCoroutine(
            DisplayMessageRoutine("WELL DONE " + GameResources.Instance.currentPlayer.playerName +
                                  "! YOU'VE SURVIVED ....SO FAR\n\nNOW FIND DEFEAT THE BOSS ....GOOD LUCK!",
                Color.white, 5f));

        yield return StartCoroutine(Fade(1f, 0f, 2f, new Color(0f, 0f, 0f, 0.4f)));
        
        // yield return StartCoroutine(
        //     DisplayMessageRoutine(
        //         "干得好，" + GameResources.Instance.currentPlayer.playerName + "！你已经活下来...到目前为止\n\n现在去找到并击败boss...祝你好运！",
        //         Color.white, 5f));

        //Debug.Log("Boss stage - find and destroy the boss");
    }

    /// <summary>
    /// Show level as being completed - load next level     显示关卡已完成，并加载下一个关卡
    /// </summary>
    /// <returns></returns>
    private IEnumerator LevelCompleted()
    {
        //play next level
        gameState = GameState.playingLevel;
        
        //wait 2 seconds
        yield return new WaitForSeconds(2f);
        
        //Debug.Log("Level Completed - Press Return To Process To The Next Level");
        
        //Fade in canvas to display text message    淡入画布以显示文本消息
        yield return StartCoroutine(Fade(0f, 1f, 2f, new Color(0f, 0f, 0f, 0.4f)));
        
        //Display level completed
        yield return StartCoroutine(DisplayMessageRoutine(
            "WELL DONE " + GameResources.Instance.currentPlayer.playerName + "! \n\nYOU'VE SURVIVED THIS DUNGEON LEVEL",
            Color.white, 5f));
        
        // yield return StartCoroutine(DisplayMessageRoutine(
        //     "干得好，" + GameResources.Instance.currentPlayer.playerName + "! \n\n你已经成功通过了这个地下城关卡！",
        //     Color.white, 5f));

        yield return StartCoroutine(DisplayMessageRoutine(
            "COLLECT ANY LOOT ....THEN PRESS RETURN\n\nTO DESCEND FURTHER INTO THE DUNGEON", Color.white, 5f));
        
        // yield return StartCoroutine(DisplayMessageRoutine(
        //     "收集任何战利品……然后按返回键\n\n深入地下城更深层", Color.white, 5f));
        
        //Fade out canvas
        yield return StartCoroutine(Fade(1f, 0f, 2f, new Color(0f, 0f, 0f, 0.4f)));
        
        //When player presses the return key processed to the next level    当玩家按下回车键时，处理进入下一个关卡
        while (!Input.GetKeyDown(KeyCode.Return))
        {
            yield return null;
        }
        
        //to avoid center being detected twice  为了避免中心被检测两次
        yield return null;
        
        //Increase index to next level  将索引增加到下一个关卡
        currentDungeonLevelListIndex++;
        
        PlayDungeonLevel(currentDungeonLevelListIndex);
    }

    /// <summary>
    /// Fade Canvas Group
    /// </summary>
    /// <param name="startFadeAlpha"></param>
    /// <param name="targetFadeAlpha"></param>
    /// <param name="fadeSeconds"></param>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public IEnumerator Fade(float startFadeAlpha, float targetFadeAlpha, float fadeSeconds, Color backgroundColor)
    {
        isFading = true;
        
        Image image = canvasGroup.GetComponent<Image>();
        image.color = backgroundColor;

        float time = 0;

        while (time <= fadeSeconds)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startFadeAlpha, targetFadeAlpha, time / fadeSeconds);
            yield return null;
        }
        
        isFading = false;
    }

    /// <summary>
    /// Game Won
    /// </summary>
    /// <returns></returns>
    private IEnumerator GameWon()
    {
        previousGameState = GameState.gameWon;
        
        //Debug.Log("Game Won - All levels completed and bossed defeated. Game will restart in 10 seconds.");
        
        //Wait 10 seconds
        // yield return new WaitForSeconds(10f);
        
        //Disable player
        GetPlayer().playerControl.DisablePlayer();
        
        //Get rank
        int rank = HighScoreManager.Instance.GetRank(gameScore);

        string rankText;
        
        //Test if the score is in the rankings  测试该分数是否在排行榜内
        if (rank > 0 && rank <= Settings.numberOfHighScoresToSave)
        {
            rankText = "YOUR SCORE IS RANKED" + rank.ToString("#0") + " IN THE TOP " +
                       Settings.numberOfHighScoresToSave.ToString("#0");

            string name = GameResources.Instance.currentPlayer.playerName;

            if (name == "")
            {
                name = playerDetails.playerCharacterName.ToUpper();
            }
            
            //Update scores     更新分数
            HighScoreManager.Instance.AddScore(new Score()
            {
                playerName = name,
                levelDescription = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + " - " +
                                   GetCurrentDungeonLevel().levelName.ToUpper(),
                playerScore = gameScore
            }, rank);
        }
        else
        {
            rankText = "YOU SCORE ISN'T RANKED IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");
        }

        yield return new WaitForSeconds(1f);

        //Fade Out  淡出
        yield return StartCoroutine(Fade(0f, 1f, 2f, Color.black));
        
        //Display game won
        yield return StartCoroutine(DisplayMessageRoutine(
            "WELL DONE" + GameResources.Instance.currentPlayer.playerName + "! YOU HAVE DEFEATED THE DUNGEON",
            Color.white, 3f));
        
        // yield return StartCoroutine(DisplayMessageRoutine(
        //     "干得好，" + GameResources.Instance.currentPlayer.playerName + "！你已经通关了地下城",
        //     Color.white, 3f));

        yield return StartCoroutine(DisplayMessageRoutine(
            "YOUR SCORE " + gameScore.ToString("###,###0") + "\n\n" + rankText, Color.white,
            4f));
        
        // yield return StartCoroutine(DisplayMessageRoutine("你的得分 " + gameScore.ToString("###,###0"), Color.white,
        //     4f));

        yield return StartCoroutine(DisplayMessageRoutine("PRESS RETURN TO RESTART THE GAME", Color.white, 0f));
        
        // yield return StartCoroutine(DisplayMessageRoutine("按下回车重新开始游戏", Color.white, 0f));
        
        //Set game state to restart game    将游戏状态设置为重启游戏
        gameState = GameState.restartGame;
    }

    /// <summary>
    /// Game Lost
    /// </summary>
    /// <returns></returns>
    private IEnumerator GameLost()
    {
        previousGameState = GameState.gameLost;
        
        // Debug.Log("Game Lost - Bad luck!. Game will restart in 10 seconds.");
        
        //Wait 10 seconds
        // yield return new WaitForSeconds(10f);
        
        //Display player
        GetPlayer().playerControl.DisablePlayer();
        
        //Get rank
        int rank = HighScoreManager.Instance.GetRank(gameScore);
        string rankText;
        
        //Test if the score is in the rankings      测试该分数是否在排行榜内
        if (rank > 0 && rank <= Settings.numberOfHighScoresToSave)
        {
            rankText = "YOUR SCORE IS RANKED" + rank.ToString("#0") + " IN THE TOP " +
                       Settings.numberOfHighScoresToSave.ToString("#0");

            string name = GameResources.Instance.currentPlayer.playerName;

            if (name == "")
            {
                name = playerDetails.playerCharacterName.ToUpper();
            }
            
            //Update scores
            HighScoreManager.Instance.AddScore(new Score()
            {
                playerName = name,
                levelDescription = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + " - " +
                                   GetCurrentDungeonLevel().levelName.ToUpper(),
                playerScore = gameScore
            }, rank);
        }
        else
        {
            rankText = "YOU SCORE ISN'T RANKED IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");
        }
        
        //Wait 1 seconds
        yield return new WaitForSeconds(1f);
        
        //Fade Out
        yield return StartCoroutine(Fade(0f, 1f, 2f, Color.black));
        
        //Disable enemies (FindObjectOfType is resource hungry - but ok to use in this end of game situation)
        Enemy[] enemyArray = GameObject.FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemyArray)
        {
            enemy.gameObject.SetActive(false);
        }
        
        //Display game lost
        yield return StartCoroutine(DisplayMessageRoutine(
            "BAD LUCK " + GameResources.Instance.currentPlayer.playerName + "! YOU HAVE SUCCUMBED TO THE DUNGEON",
            Color.white, 2f));
        
        // yield return StartCoroutine(DisplayMessageRoutine(
        //     "很遗憾 " + GameResources.Instance.currentPlayer.playerName + "！你已在地下城中陨命",
        //     Color.white, 2f));

        yield return StartCoroutine(DisplayMessageRoutine(
            "YOUR SCORED " + gameScore.ToString("###,###0") + "\n\n" + rankText, Color.white,
            4f));
        
        // yield return StartCoroutine(DisplayMessageRoutine("你的得分 " + gameScore.ToString("###,###0"), Color.white,
        //     4f));
        
        yield return StartCoroutine(DisplayMessageRoutine("PRESS RETURN TO RESTART GAME", Color.white, 0f));
        
        //yield return StartCoroutine(DisplayMessageRoutine("按回车重新开始游戏", Color.white, 0f));

        //Set game state to restart game    将游戏状态设置为重启游戏
        gameState = GameState.restartGame;
    }

    /// <summary>
    /// Restart the game
    /// </summary>
    private void RestartGame()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// Dungeon Map Screen Display  地下城地图屏幕显示
    /// </summary>
    private void DisplayDungeonOverviewMap()
    {
        //return if fading  如果正在渐变，则返回
        if (isFading) return;
        
        //Display dungeonOverviewMap    显示地下城概览地图
        DungeonMap.Instance.DisplayDungeonOverViewMap();
    }

    private void PlayDungeonLevel(int dungeonLevelListIndex)
    {
        //Build dungeon for level1
        bool dungeonBuiltSuccessfully =
            DungeonBuilder.Instance.GenerateDungeon(dungeonLevelList[dungeonLevelListIndex]);

        if (!dungeonBuiltSuccessfully)
        {
            Debug.LogError("Couldn't build dungeon from specified rooms and node graphs");
        }
        
        //Call static event that room has changed   调用静态事件，表示房间已更改
        StaticEventHandler.CallRoomChangedEvent(currentRoom);
        
        //Set player roughly mid-room   将玩家大致设置在房间中间
        player.gameObject.transform.position = new Vector3((currentRoom.lowerBounds.x + currentRoom.upperBounds.x) / 2f,
            (currentRoom.lowerBounds.y + currentRoom.upperBounds.y) / 2f, 0f);
        
        //Get nearest spawn point in room nearest to player
        //获取房间中最靠近玩家的最近生成点
        player.gameObject.transform.position =
            HelperUtilities.GetSpawnPositionNearestToPlayer(player.gameObject.transform.position);
        
        //Display Dungeon Level Text
        StartCoroutine(DisplayDungeonLevelText());
        
        //** Demo code
        // RoomEnemiesDefeated();
    }

    /// <summary>
    /// Display the dungeon level text  显示地牢关卡文本
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayDungeonLevelText()
    {
        //Set screen to black
        StartCoroutine(Fade(0f, 1f, 0f, Color.black));
        
        GetPlayer().playerControl.DisablePlayer();

        string messageText = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + "\n\n" +
                             dungeonLevelList[currentDungeonLevelListIndex].levelName.ToUpper();
        
        yield return StartCoroutine(DisplayMessageRoutine(messageText,Color.white, 2f));
        
        GetPlayer().playerControl.EnablePlayer();
        
        //fade in
        yield return StartCoroutine(Fade(1f, 0f, 2f, Color.black));
    }

    /// <summary>
    /// Display the message text for displaySeconds - if displaySeconds = 0 then the message is displayed until the return key is pressed
    /// 显示消息文本，持续 displaySeconds 秒。如果 displaySeconds = 0，则消息会一直显示，直到按下回车键。
    /// </summary>
    /// <param name="text"></param>
    /// <param name="textColor"></param>
    /// <param name="displaySeconds"></param>
    /// <returns></returns>
    private IEnumerator DisplayMessageRoutine(string text, Color textColor, float displaySeconds)
    {
        //Set text
        messageTextTMP.SetText(text);
        messageTextTMP.color = textColor;
        
        //Display the message for the given time    按指定时间显示消息
        if (displaySeconds > 0)
        {
            float timer = displaySeconds;

            while (timer > 0f && !Input.GetKeyDown(KeyCode.Return))
            {
                timer -= Time.deltaTime;
                yield return null;
            }
        }
        //else display the message until the return button is pressed   否则显示消息，直到按下回车键
        else
        {
            while (!Input.GetKeyDown(KeyCode.Return))
            {
                yield return null;
            }
        }
        
        yield return null;
        
        //Clear text
        messageTextTMP.SetText("");
    }

    /// <summary>
    /// Get the player
    /// </summary>
    /// <returns></returns>
    public Player GetPlayer()
    {
        return player;
    }

    /// <summary>
    /// Get the player minimap icon
    /// </summary>
    /// <returns></returns>
    public Sprite GetPlayerMiniMapIcon()
    {
        return playerDetails.playerMiniMapIcon;
    }

    /// <summary>
    /// Get the current room to player is in 
    /// </summary>
    /// <returns></returns>
    public Room GetCurrentRoom()
    {
        return currentRoom;
    }

    /// <summary>
    /// Get the current dungeon level
    /// </summary>
    /// <returns></returns>
    public DungeonLevelSO GetCurrentDungeonLevel()
    {
        return dungeonLevelList[currentDungeonLevelListIndex];
    }

    #region Validation
    #if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this, nameof(pauseMenu), pauseMenu);
        HelperUtilities.ValidateCheckNullValue(this,nameof(messageTextTMP), messageTextTMP);
        HelperUtilities.ValidateCheckNullValue(this,nameof(canvasGroup), canvasGroup);
        HelperUtilities.ValidateCheckEnumerableValues(this, nameof(dungeonLevelList), dungeonLevelList);
    }
#endif
    #endregion
}
