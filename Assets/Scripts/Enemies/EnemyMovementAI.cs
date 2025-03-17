using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class EnemyMovementAI : MonoBehaviour
{
    #region Tooltip
    //MovementDetailsSO 脚本化对象包含了诸如速度等的移动细节
    [Tooltip("MovementDetailsSO scriptable object containing movement details such as speed")]

    #endregion

    [SerializeField]
    private MovementDetailsSO movementDetails;

    private Enemy enemy;
    private Stack<Vector3> movementSteps = new Stack<Vector3>();
    private Vector3 playerReferencePosition;
    private Coroutine moveEnemyRoutine;
    private float currentEnemyPathRebuildCooldown;
    private WaitForFixedUpdate waitForFixedUpdate;
    [HideInInspector] public float moveSpeed;
    private bool chasePlayer = false;
    [HideInInspector] public int updateFrameNumber = 1; //default value. This is set by the enemy spawner   默认值,由敌人生成器设置
    private List<Vector2Int> surroundingPositionList = new List<Vector2Int>();

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        moveSpeed = movementDetails.GetMoveSpeed();
    }

    private void Start()
    {
        //Create waitforfixed update for use in coroutine   创建 WaitForFixedUpdate 用于协程中
        waitForFixedUpdate = new WaitForFixedUpdate();
        
        //Reset player reference position   重置玩家参考位置
        playerReferencePosition = GameManager.Instance.GetPlayer().GetPlayerPosition();
    }

    private void Update()
    {
        MoveEnemy();
    }

    /// <summary>
    /// Use AStar pathfinding to build a path to the player - and then move the enemy to each grid location on the path
    /// 使用AStar路径寻找来构建到玩家的路径，然后将敌人移动到路径上的每个网格位置
    /// </summary>
    private void MoveEnemy()
    {
        //Movement cooldown timer   移动冷却计时器
        currentEnemyPathRebuildCooldown -= Time.deltaTime;
        
        //Check distance to player to see if enemy should start chasing 检查与玩家的距离，判断敌人是否应该开始追击
        if (!chasePlayer && Vector3.Distance(transform.position, GameManager.Instance.GetPlayer().GetPlayerPosition()) <
            enemy.enemyDetails.chaseDistance)
        {
            chasePlayer = true;
        }
        
        //if not close enough to chase player then return   如果距离玩家不够近，则返回。
        if (!chasePlayer) return;
        
        //Only process A Star path rebuild on certain frames to spread the load between enemies
        //仅在特定帧处理 A* 路径重建，以在敌人之间分摊计算负载
        if (Time.frameCount % Settings.targetFrameRateToSpreadPathfindingOver != updateFrameNumber) return;
        
        //if the movement cooldown timer reached or player has moved more than required distance
        //then rebuild the enemy path and move the enemy
        //如果移动冷却计时器已达到，或者玩家已移动超过所需距离，则重新构建敌人的路径并移动敌人。
        if (currentEnemyPathRebuildCooldown <= 0 ||
            (Vector3.Distance(playerReferencePosition, GameManager.Instance.GetPlayer().GetPlayerPosition()) >
             Settings.playerMoveDistaanceToRebuildPath))
        {
            //Reset path rebuild cooldown timer 重置路径重建冷却计时器
            currentEnemyPathRebuildCooldown = Settings.enemyPathbuildCooldown;
            
            //Reset player reference position
            playerReferencePosition = GameManager.Instance.GetPlayer().GetPlayerPosition();
            
            //Move the enemy using AStar pathfinding - Trigger rebuild of path to player
            CreatePath();
            
            //if a path has been found move the enemy   如果找到路径，则移动敌人
            if (movementSteps != null)
            {
                if (moveEnemyRoutine != null)
                {
                    //Trigger idle event    触发空闲事件
                    enemy.idleEvent.CallIdleEvent();
                    StopCoroutine(moveEnemyRoutine);
                }
                
                //Move enemy along the path using a coroutine   通过协程沿路径移动敌人
                moveEnemyRoutine = StartCoroutine(MoveEnemyRoutine(movementSteps));
            }
        }
    }

    /// <summary>
    /// Coroutine to move the enemy to the next location on the path    协程将敌人移动到路径上的下一个位置
    /// </summary>
    /// <param name="movementSteps"></param>
    /// <returns></returns>
    private IEnumerator MoveEnemyRoutine(Stack<Vector3> movementSteps)
    {
        while (movementSteps.Count > 0)
        {
            Vector3 nextPosition = movementSteps.Pop();
            
            //while not very close continue to move - when close move onto the next step
            //当敌人未非常接近目标时，继续移动，接近目标时则进入下一个步骤。
            while (Vector3.Distance(nextPosition, transform.position) > 0.2f) 
            {
                //Trigger movement event    触发移动事件
                enemy.movementToPositionEvent.CallMovementToPosition(nextPosition, transform.position, moveSpeed,
                    (nextPosition - transform.position).normalized);
                
                //moving the enemy using 2D physics so wait until the next fixed update
                //使用2D物理来移动敌人，因此等待下一个FixedUpdate。
                yield return waitForFixedUpdate;
            }
            
            yield return waitForFixedUpdate;
        }
        
        //End of path steps - trigger the enemy idle event    路径步骤结束 - 触发敌人闲置事件。
        enemy.idleEvent.CallIdleEvent();
    }

    /// <summary>
    /// Set the frame number that the enemy path will be recalculated on - to avoid performance spikes
    /// 设置敌人路径重新计算的帧编号，以避免性能峰值
    /// </summary>
    /// <param name="updateFrameNumber"></param>
    public void SetUpdateFrameNumber(int updateFrameNumber)
    {
        this.updateFrameNumber = updateFrameNumber;
    }

    /// <summary>
    /// Use the AStar static class to create a path for the enemy   使用 AStar 静态类为敌人创建路径
    /// </summary>
    private void CreatePath()
    {
        Room currentRoom = GameManager.Instance.GetCurrentRoom();

        Grid grid = currentRoom.instantiatedRoom.grid;
        
        //Get players position on the grid  获取玩家在网格上的位置
        Vector3Int playerGridPosition = GetNearestNonObstaclePlayerPosition(currentRoom);
        
        //Get enemy position on the grid    获取敌人在网格上的位置
        Vector3Int enemyGridPosition = grid.WorldToCell(transform.position);
        
        //Build a path for the enemy to move on     为敌人构建一条移动路径
        movementSteps = AStar.BuildPath(currentRoom, enemyGridPosition, playerGridPosition);
        
        //Take off first step on path - this is the grid square the enemy is already on
        //移除路径中的第一步——这是敌人当前所在的网格格子
        if (movementSteps != null)
        {
            movementSteps.Pop();
        }
        else
        {
            //Trigger idle event - no path  当没有路径时，触发闲置事件
            enemy.idleEvent.CallIdleEvent();
        }
    }

    /// <summary>
    /// Get the nearest position to the player that isn't on an obstacle    获取离玩家最近且不在障碍物上的位置
    /// </summary>
    /// <param name="currentRoom"></param>
    /// <returns></returns>
    private Vector3Int GetNearestNonObstaclePlayerPosition(Room currentRoom)
    {
        Vector3 playerPosition = GameManager.Instance.GetPlayer().GetPlayerPosition();

        Vector3Int playerCellPosition = currentRoom.instantiatedRoom.grid.WorldToCell(playerPosition);

        Vector2Int adjustedPlayerCellPosition = new Vector2Int(playerCellPosition.x - currentRoom.templateLowerBounds.x,
            playerCellPosition.y - currentRoom.templateLowerBounds.y);

        int obstacle =
            Mathf.Min(currentRoom.instantiatedRoom.aStarMovementPenalty[adjustedPlayerCellPosition.x,
                    adjustedPlayerCellPosition.y],
                currentRoom.instantiatedRoom.aStarItemObstacles[adjustedPlayerCellPosition.x,
                    adjustedPlayerCellPosition.y]);
        
        //if the player isn't on a cell square marked as an obstacle then return that position
        //如果玩家不在标记为障碍物的格子上，则返回该位置
        if (obstacle != 0)
        {
            return playerCellPosition;
        }
        //find a surrounding cell that isn't an obstacle - required because with the 'half collision' tiles
        //and tables the player can be on a grid square that is marked as an obstacle
        //// 找到一个周围的格子，确保它不是障碍物 - 这是因为“半碰撞”瓷砖和桌子的存在，  玩家可能站在一个标记为障碍物的格子上
        else
        {
            //Empty surrounding position list   清空周围位置列表
            surroundingPositionList.Clear();
            
            //Populate surrounding position list - this will hold the 8 possible vector locations surround a (0,0) grid square
            //填充周围位置列表——该列表将包含围绕 (0,0) 网格方块的 8 个可能的向量位置
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (j == 0 && i == 0) continue;
                    
                    surroundingPositionList.Add(new Vector2Int(i, j));
                }
            }
            
            //loop through all positions    遍历所有位置
            for (int i = 0; i < 8; i++)
            {
                //Generate a random index for the list  生成列表的随机索引
                int index = Random.Range(0, surroundingPositionList.Count);
                
                //See if there is an obstacle in the selected surrounding position  查看选定的周围位置是否有障碍物
                try
                {
                    obstacle = Mathf.Min(
                        currentRoom.instantiatedRoom.aStarMovementPenalty[
                            adjustedPlayerCellPosition.x + surroundingPositionList[index].x,
                            adjustedPlayerCellPosition.y + surroundingPositionList[index].y],
                        currentRoom.instantiatedRoom.aStarItemObstacles[
                            adjustedPlayerCellPosition.x + surroundingPositionList[index].x,
                            adjustedPlayerCellPosition.y + surroundingPositionList[index].y]);
                    
                    //If no obstacle return the cell position to navigate to 如果没有障碍物，则返回可以导航到的单元格位置
                    if (obstacle != 0)
                    {
                        return new Vector3Int(playerCellPosition.x + surroundingPositionList[index].x,
                            playerCellPosition.y + surroundingPositionList[index].y, 0);
                    }
                }
                //Catch errors where the surrounding position is outside the grid   捕捉错误，当周围位置超出网格时
                catch
                {
                    
                }
                
                //Remove the surrounding position with the obstacle so we can try again 移除带有障碍物的周围位置，以便我们可以重新尝试
                surroundingPositionList.RemoveAt(index);
            }
            
            //If no non-obstacle cells found surround the player - send the enemy in the direction of an enemy spawn position
            //如果没有找到没有障碍物的单元格，围绕玩家的位置——则将敌人发送到敌人生成位置的方向
            return (Vector3Int)currentRoom.spawnPositionArray[Random.Range(0, currentRoom.spawnPositionArray.Length)];

            // for (int i = -1; i <= 1; i++)
            // {
            //     for (int j = -1; j <= 1; j++)
            //     {
            //         if (j == 0 && i == 0) continue;
            //
            //         try
            //         {
            //             obstacle = currentRoom.instantiatedRoom.aStarMovementPenalty[adjustedPlayerCellPosition.x + i,
            //                 adjustedPlayerCellPosition.y + j];
            //             if (obstacle != 0)
            //             {
            //                 return new Vector3Int(playerCellPosition.x + i, playerCellPosition.y + j, 0);
            //             }
            //         }
            //         catch
            //         {
            //             continue;
            //         }
            //     }
            // }
            
            //No non-obstacle cells surrounding the player so just return the player position
            //没有玩家周围的非障碍物格子，直接返回玩家的位置
            return playerCellPosition;
        }
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this,nameof(movementDetails), movementDetails);
    }
#endif

    #endregion
}
