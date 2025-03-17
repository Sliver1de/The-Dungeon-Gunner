using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : SingletonMonobehaviour<EnemySpawner>
{
    private int enemiesToSpawn;
    private int currentEnemyCount;
    private int enemiesSpawnedSoFar;
    private int enemyMaxConcurrentSpawnNumber;
    private Room currentRoom;
    private RoomEnemySpawnParameters roomEnemySpawnParameters;

    private void OnEnable()
    {
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;
    }

    private void OnDisable()
    {
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
    }

    /// <summary>
    /// Process a change in room    处理房间变化
    /// </summary>
    /// <param name="roomChangedEventArgs"></param>
    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        enemiesSpawnedSoFar = 0;
        currentEnemyCount = 0;
        
        currentRoom = roomChangedEventArgs.room;
        
        //Update music for room     更新房间音乐
        MusicManager.Instance.PlayMusic(currentRoom.ambientMusic, 0.2f, 2f);
        
        //if the room is a corridor or the entrance then return 如果房间是走廊或入口，则返回
        if (currentRoom.roomNodeType.isCorridorEW || currentRoom.roomNodeType.isCorridorNS ||
            currentRoom.roomNodeType.isEntrance)
        {
            return;
        }
        
        //if the room has already been defeated then return 如果房间（敌人）已经被打败，则返回
        if (currentRoom.isClearedOfEnemies) return;
        
        //Get random number of enemies to spawn 获取生成的敌人数量
        enemiesToSpawn = currentRoom.GetNumberOfEnemiesToSpawn(GameManager.Instance.GetCurrentDungeonLevel());
        
        //Get room enemy spawn parameters   获取房间敌人生成参数
        roomEnemySpawnParameters =
            currentRoom.GetRoomEnemySpawnParameters(GameManager.Instance.GetCurrentDungeonLevel());
        
        //if no enemies to spawn return 如果没有敌人生成，返回
        if (enemiesToSpawn == 0)
        {
            //Mark the room as cleared  将房间标记为已清除
            currentRoom.isClearedOfEnemies = true;
            return;
        }
        
        //Get concurrent number of enemies to spawn 获取要生成的敌人并发数量
        enemyMaxConcurrentSpawnNumber = GetConcurrentEnemies();
        
        //Update music for room     更新房间音乐
        MusicManager.Instance.PlayMusic(currentRoom.battleMusic, 0.2f, 0.5f);
        
        //Lock doors
        currentRoom.instantiatedRoom.LockDoors();
        
        //Spawn enemies
        SpawnEnemies();
    }

    /// <summary>
    /// Spawn the enemies   生成敌人
    /// </summary>
    private void SpawnEnemies()
    {
        //Set gamestate engaging boss
        if (GameManager.Instance.gameState == GameState.bossStage)
        {
            GameManager.Instance.previousGameState = GameState.bossStage;
            GameManager.Instance.gameState = GameState.engagingBoss;
        }

        //Set gamestate engaging enemies
        else if (GameManager.Instance.gameState == GameState.playingLevel)
        {
            GameManager.Instance.previousGameState = GameState.playingLevel;
            GameManager.Instance.gameState = GameState.engagingEnemies;
        }

        StartCoroutine(SpawnEnemiesRoutine());
    }

    /// <summary>
    /// Spawn the enemies coroutine 生成敌人的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnEnemiesRoutine()
    {
        Grid grid = currentRoom.instantiatedRoom.grid;
        
        //Create an instance of the helper class used to select a random enemy  创建一个实例化的助手类，用于选择一个随机敌人
        RandomSpawnableObject<EnemyDetailsSO> randomEnemyHelperClass =
            new RandomSpawnableObject<EnemyDetailsSO>(currentRoom.enemiesByLevelList);
        
        //Check we have somewhere to spawn the enemies  检查我们是否有地方生成敌人
        if (currentRoom.spawnPositionArray.Length > 0)
        {
            //loop through to create all the enemies    循环遍历以创建所有敌人
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                //wait until current enemy count is less than max concurrent enemies    等待直到当前敌人数量少于最大并发敌人数量
                while (currentEnemyCount >= enemyMaxConcurrentSpawnNumber)
                {
                    yield return null;
                }

                Vector3Int cellPosition =
                    (Vector3Int)currentRoom.spawnPositionArray[Random.Range(0, currentRoom.spawnPositionArray.Length)];
                
                //Create Enemy - Get next enemy type to spawn   创建敌人 - 获取下一个要生成的敌人类型
                CreateEnemy(randomEnemyHelperClass.GetItem(), grid.CellToWorld(cellPosition));
                
                yield return new WaitForSeconds(GetEnemySpawnInterval());
            }
        }
    }

    /// <summary>
    /// Get a random spawn interval between the minimum and maximum values  获取最小值和最大值之间的随机生成间隔
    /// </summary>
    /// <returns></returns>
    private float GetEnemySpawnInterval()
    {
        return (Random.Range(roomEnemySpawnParameters.minSpawnInterval, roomEnemySpawnParameters.maxSpawnInterval));
    }

    /// <summary>
    /// Get a random number of concurrent enemies between the minimum and maximum values
    /// 获取最小值和最大值之间的随机生成并发敌人数
    /// </summary>
    /// <returns></returns>
    private int GetConcurrentEnemies()
    {
        return (Random.Range(roomEnemySpawnParameters.minConcurrentEnemies,
            roomEnemySpawnParameters.maxConcurrentEnemies));
    }

    /// <summary>
    /// Create an enemy in the specified position   在指定位置创建一个敌人
    /// </summary>
    /// <param name="enemyDetails"></param>
    /// <param name="position"></param>
    private void CreateEnemy(EnemyDetailsSO enemyDetails, Vector3 position)
    {
        //keep track of the number of enemies spawned os far    跟踪到目前为止已生成的敌人数量
        enemiesSpawnedSoFar++;
        
        //Add one to the current enemy count - this is reduced when an enemy is destroyed
        //将当前敌人数量加一——当敌人被销毁时，这个数量会减少。
        currentEnemyCount++;
        
        //Get current dungeon level 获取当前地下城等级
        DungeonLevelSO dungeonLevel = GameManager.Instance.GetCurrentDungeonLevel();
        
        //Instantiate enemy 实例化敌人
        GameObject enemy = Instantiate(enemyDetails.enemyPrefab, position, Quaternion.identity, transform);
        
        //Initialize Enemy  初始化敌人
        enemy.GetComponent<Enemy>().EnemyInitialization(enemyDetails, enemiesSpawnedSoFar, dungeonLevel);
        
        //subscribe to enemy destroyed event    订阅敌人销毁事件
        enemy.GetComponent<DestroyedEvent>().OnDestroyed += Enemy_OnDestroyed;
    }

    /// <summary>
    /// Process enemy destroyed 处理敌人销毁
    /// </summary>
    /// <param name="destroyedEvent"></param>
    private void Enemy_OnDestroyed(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
    {
        //Unsubscribe from event
        destroyedEvent.OnDestroyed -= Enemy_OnDestroyed;
        
        //reduce current enemy count    减少当前敌人数量
        currentEnemyCount--;
        
        //Score points - call points scored event   得分 - 调用得分事件
        StaticEventHandler.CallPointsScoredEvent(destroyedEventArgs.points);

        if (currentEnemyCount <= 0 && enemiesSpawnedSoFar == enemiesToSpawn)
        {
            currentRoom.isClearedOfEnemies = true;
            
            //Set game state
            if (GameManager.Instance.gameState == GameState.engagingEnemies)
            {
                GameManager.Instance.gameState = GameState.playingLevel;
                GameManager.Instance.previousGameState = GameState.engagingEnemies;
            }
            else if (GameManager.Instance.gameState == GameState.engagingBoss)
            {
                GameManager.Instance.gameState = GameState.bossStage;
                GameManager.Instance.previousGameState = GameState.engagingBoss;
            }
            
            //unlock doors
            currentRoom.instantiatedRoom.UnlockDoors(Settings.doorUnlockDelay);
            
            //Update music for room
            MusicManager.Instance.PlayMusic(currentRoom.ambientMusic, 0.2f, 2f);
            
            //Trigger room enemies defeated event   触发敌人击败事件
            StaticEventHandler.CallRoomEnemiesDefeatedEvent(currentRoom);
        }
    }
}
