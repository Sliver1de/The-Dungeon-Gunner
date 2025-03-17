using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class InstantiatedRoom : MonoBehaviour
{
    [HideInInspector] public Room room;
    [HideInInspector] public Grid grid;
    [HideInInspector] public Tilemap groundTilemap;
    [HideInInspector] public Tilemap decoration1Tilemap;
    [HideInInspector] public Tilemap decoration2Tilemap;
    [HideInInspector] public Tilemap frontTilemap;
    [HideInInspector] public Tilemap collisionTilemap;
    [HideInInspector] public Tilemap minimapTilemap;
    //use this 2d array to store movement penalties from the tilemaps to be used in AStar pathfinding
    //使用这个二维数组存储来自瓦片地图的移动惩罚，以用于 A* 寻路
    [HideInInspector] public int[,] aStarMovementPenalty;
    //use to store position of moveable items that are obstacle     使用存储可移动物品的先前位置的变量
    [HideInInspector] public int[,] aStarItemObstacles;
    [HideInInspector] public Bounds roomColliderBounds;
    [HideInInspector] public List<MoveItem> moveableItemsList = new List<MoveItem>();

    #region Header OBJECT REFERENCES

    [Space(10)]
    [Header("OBJECT REFERENCES")]

    #endregion

    #region Tooltip
    //填充环境子占位符游戏对象
    [Tooltip("Populate with the environment child placeholder gameobject")]

    #endregion

    [SerializeField]
    private GameObject environmentGameObject;
    
    private BoxCollider2D boxCollider2D;
    
    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>(); 
        
        //Save room collider bounds
        roomColliderBounds = boxCollider2D.bounds;
        
        // if (environmentGameObject != null)
        // {
        //     Debug.Log($"Environment Layer: {LayerMask.LayerToName(environmentGameObject.layer)}");
        // }
    }

    private void Start()
    {
        //Update moveable item obstacles array
        UpdateMoveableObstacles();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if the player trigger the collider 如果玩家触发了碰撞器
        if (collision.gameObject.tag == Settings.playerTag && room != GameManager.Instance.GetCurrentRoom())
        {
            //set room as visited 将房间设置为已访问
            this.room.isPreviouslyVisited = true;
            
            //Call room changed event 回调房间更改事件
            StaticEventHandler.CallRoomChangedEvent(room);
        }
    }


    /// <summary>
    /// Initialise the instantiated room    初始化实例化的房间
    /// </summary>
    /// <param name="roomGameobject"></param>
    public void Initialise(GameObject roomGameobject)
    {
        PopulateTileMapMemberVariables(roomGameobject);

        BlockOffUnusedDoorWays();

        AddObstaclesAndPreferredPaths();

        CreateItemObstaclesArray();

        AddDoorsToRooms();

        DisableCollisionTilemapRenderer();
    }

    /// <summary>
    /// Populate the tilemap and grid member variables  填充tilemap和grid成员变量
    /// </summary>
    /// <param name="roomGameobject"></param>
    private void PopulateTileMapMemberVariables(GameObject roomGameobject)
    {
        //Get the grid component    获取网格组件
        grid = roomGameobject.GetComponentInChildren<Grid>();
        
        //Get tilemaps in children
        Tilemap[] tilemaps = roomGameobject.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.CompareTag("groundTilemap"))
            {
                groundTilemap = tilemap;
            }
            else if (tilemap.CompareTag("decoration1Tilemap"))
            {
                decoration1Tilemap = tilemap;
            }
            else if (tilemap.CompareTag("decoration2Tilemap"))
            {
                decoration2Tilemap = tilemap;
            }
            else if (tilemap.CompareTag("frontTilemap"))
            {
                frontTilemap = tilemap;
            }
            else if (tilemap.CompareTag("collisionTilemap"))
            {
                collisionTilemap = tilemap;
            }
            else if (tilemap.CompareTag("minimapTilemap"))
            {
                minimapTilemap = tilemap;
            }
        }
    }

    /// <summary>
    /// Block Off Unused Doorways in the room   堵住房间内未使用的门口
    /// </summary>
    private void BlockOffUnusedDoorWays()
    {
        //Debug.Log($"Room: {room}");
        //Debug.Log($"Doorway list: {room?.doorwayList}");
        //Debug.Log($"CollisionTilemap: {collisionTilemap}");
        
        //Loop through all doorways
        foreach (Doorway doorway in room.doorwayList)
        {
            if (doorway.isConnected)
            {
                continue;
            }
            
            //Block unconnected doorways using tiles on tilemaps    使用瓷砖地图上的瓷砖阻挡未连接的门口
            if (collisionTilemap != null)
            {
                BlockADoorwayOnTilemapLayer(collisionTilemap, doorway);
            }

            if (minimapTilemap != null)
            {
                BlockADoorwayOnTilemapLayer(minimapTilemap, doorway);
            }

            if (groundTilemap != null)
            {
                BlockADoorwayOnTilemapLayer(groundTilemap, doorway);
            }

            if (decoration1Tilemap != null)
            {
                BlockADoorwayOnTilemapLayer(decoration1Tilemap, doorway);
            }

            if (decoration2Tilemap != null)
            {
                BlockADoorwayOnTilemapLayer(decoration2Tilemap, doorway);
            }

            if (frontTilemap != null)
            {
                BlockADoorwayOnTilemapLayer(frontTilemap, doorway);
            }
        }
    }

    /// <summary>
    /// Block a doorway on a tilemap layer  在图块地图图层上封锁门口
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockADoorwayOnTilemapLayer(Tilemap tilemap, Doorway doorway)
    {
        switch (doorway.orientation)
        {
            case Orientation.north:
            case Orientation.south:
                BlockDoorwayHorizontally(tilemap, doorway);
                break;
            case Orientation.east:
            case Orientation.west:
                BlockDoorwayVertically(tilemap, doorway);
                break;
            case Orientation.none:
                break;
        }
    }

    /// <summary>
    /// Block doorway horizontally - for North and South doorways   水平封锁门口 - 适用于北门和南门
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockDoorwayHorizontally(Tilemap tilemap, Doorway doorway)
    {
        Vector2Int startPosition = doorway.doorwayStartCopyPosition;
        
        //loop through all tiles to copy
        for (int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
        {
            for (int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
            {
                //Get rotation of tile being copied 获取正在复制的图块的旋转
                Matrix4x4 transformMatrix =
                    tilemap.GetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0));
                
                //Copy tile
                tilemap.SetTile(new Vector3Int(startPosition.x + 1 + xPos, startPosition.y - yPos, 0),
                    tilemap.GetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0)));
                
                //Set rotation of tile copied   设置复制的图块的旋转
                tilemap.SetTransformMatrix(new Vector3Int(startPosition.x + 1 + xPos, startPosition.y - yPos, 0),
                    transformMatrix);
            }
        }
    }

    /// <summary>
    /// Block doorway vertically - for East and West doorways   垂直阻挡门口 - 适用于东门和西门
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockDoorwayVertically(Tilemap tilemap, Doorway doorway)
    {
        Vector2Int startPosition = doorway.doorwayStartCopyPosition;
        
        //Loop through all tiles to copy
        for (int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
        {
            for (int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
            {
                //Get rotation of tile being copied
                Matrix4x4 transformMatrix =
                    tilemap.GetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0));
                
                //Copy tile
                tilemap.SetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - 1 - yPos, 0),
                    tilemap.GetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0)));
                
                //Set rotation of tile copied
                tilemap.SetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - 1 - yPos, 0),
                    transformMatrix);
            }
        }
    }

    /// <summary>
    /// Update obstacles used by AStar pathfinding  更新 A* 寻路所使用的障碍物数据
    /// </summary>
    private void AddObstaclesAndPreferredPaths()
    {
        //this array will be populated with wall obstacle   这个数组将被填充为墙壁障碍物
        aStarMovementPenalty = new int[room.templateUpperBounds.x - room.templateLowerBounds.x + 1,
            room.templateUpperBounds.y - room.templateLowerBounds.y + 1];
        
        //loop through all gird squares 遍历所有网格方块
        for (int x = 0; x < (room.templateUpperBounds.x - room.templateLowerBounds.x + 1); x++)
        {
            for (int y = 0; y < (room.templateUpperBounds.y - room.templateLowerBounds.y + 1); y++) 
            {
                //Set default movement penalty for grid squares 为网格方块设置默认移动惩罚
                aStarMovementPenalty[x, y] = Settings.defaultAStarMovementPenalty;
                
                //Add obstacles for collision tiles the enemy can't walk on 为敌人无法行走的碰撞瓦片添加障碍物
                TileBase tile = collisionTilemap.GetTile(new Vector3Int(x + room.templateLowerBounds.x,
                    y + room.templateLowerBounds.y, 0));

                foreach (TileBase collisionTile in GameResources.Instance.enemyUnwalkableCollisionTileArray)
                {
                    if (tile == collisionTile)
                    {
                        aStarMovementPenalty[x, y] = 0;
                        break;
                    }
                }
                
                //Add preferred path for enemies (1 is the preferred path value,
                //default value for a grid location is specified in the settings)
                //为敌人添加优先路径（1 表示优先路径值，网格位置的默认值在设置中指定）
                if (tile == GameResources.Instance.preferredEnemyPathTile)
                {
                    aStarMovementPenalty[x, y] = Settings.preferredPathAStarMovementPenalty;
                }
            }
        }
    }

    /// <summary>
    /// Add opening doors if this is not a corridor room    如果这不是一个走廊房间，则添加开门
    /// </summary>
    private void AddDoorsToRooms()
    {
        //if the room is a corridor return 如果是走廊则返回
        if (room.roomNodeType.isCorridorEW || room.roomNodeType.isCorridorNS) return;
        
        //Instantiate door prefabs at doorway position 在门口位置实例化门预设体
        foreach (Doorway doorway in room.doorwayList)
        {
            //if the doorway prefab isn't null and the doorway is connected 若门预设体不为空且门是连接着的
            if (doorway.doorPrefab != null && doorway.isConnected)
            {
                float tileDistance = Settings.tileSizePixels / Settings.pixelPerUnit;

                GameObject door = null;

                if (doorway.orientation == Orientation.north)
                {
                    //create door with parent as the room 创建门, 并将房间作为父物体
                    door = Instantiate(doorway.doorPrefab, gameObject.transform);
                    door.transform.localPosition = new Vector3(doorway.position.x + tileDistance / 2f,
                        doorway.position.y + tileDistance, 0f);
                }
                else if (doorway.orientation == Orientation.south)
                {
                    //create door with parent as the room 创建门, 并将房间作为父物体
                    door = Instantiate(doorway.doorPrefab, gameObject.transform);
                    door.transform.localPosition = new Vector3(doorway.position.x + tileDistance / 2f,
                        doorway.position.y, 0f);
                }
                else if (doorway.orientation == Orientation.east)
                {
                    //create door with parent as the room 创建门, 并将房间作为父物体
                    door = Instantiate(doorway.doorPrefab, gameObject.transform);
                    door.transform.localPosition = new Vector3(doorway.position.x + tileDistance,
                        doorway.position.y + tileDistance * 1.25f, 0f);
                }
                else if (doorway.orientation == Orientation.west)
                {
                    //create door with parent as the room 创建门, 并将房间作为父物体
                    door = Instantiate(doorway.doorPrefab, gameObject.transform);
                    door.transform.localPosition =
                        new Vector3(doorway.position.x, doorway.position.y + tileDistance * 1.25f, 0f);
                }
                
                //get door component
                Door doorComponent = door.GetComponent<Door>();
                
                //set if door is part of a boss room 如果是boss房间则设置门
                if (room.roomNodeType.isBossRoom)
                {
                    doorComponent.isBossRoomDoor = true;
                    
                    //lock the door to prevent access to the room 锁住门以防止进入房间
                    doorComponent.LockDoor();
                    
                    //Instantiate skull icon for minimap by door    在门旁实例化骷髅图标用于小地图
                    GameObject skullIcon = Instantiate(GameResources.Instance.minimapSkullPrefab, gameObject.transform);
                    skullIcon.transform.localPosition = door.transform.localPosition;
                }
            }
        }
    }

    private void DisableCollisionTilemapRenderer()
    {
        //Disable collision tilemap renderer    禁用碰撞图块地图渲染器
        collisionTilemap.gameObject.GetComponent<TilemapRenderer>().enabled = false;
    }

    /// <summary>
    /// Disable the room trigger collider that is used to trigger when the player enters a room
    /// 禁用房间触发器碰撞器，该碰撞器用于触发玩家进入房间时的事件
    /// </summary>
    public void DisableRoomCollider()
    {
        boxCollider2D.enabled = false;
    }

    /// <summary>
    /// Enable the room trigger collider that is used to trigger when the player enters a room
    /// 启用房间触发器碰撞器，该碰撞器用于触发玩家进入房间时的事件
    /// </summary>
    public void EnableRoomCollider()
    {
        boxCollider2D.enabled = true;
    }

    public void ActivateEnvironmentGameObjects()
    {
        if (environmentGameObject != null)
        {
            environmentGameObject.SetActive(true);
        }
    }

    public void DeactivateEnvironmentGameObjects()
    {
        if (environmentGameObject != null)
        {
            environmentGameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Lock the room doors
    /// </summary>
    public void LockDoors()
    {
        Door[] doorArray = GetComponentsInChildren<Door>();

        foreach (Door door in doorArray)
        {
            door.LockDoor();
        }
        
        //Disable room trigger collider
        DisableRoomCollider();
    }

    /// <summary>
    /// Unlock the room doors
    /// </summary>
    public void UnlockDoors(float doorUnlockDelay)
    {
        StartCoroutine(UnlockDoorsRoutine(doorUnlockDelay));
    }

    /// <summary>
    /// unlock the room doors coroutine     解锁房间门的协程
    /// </summary>
    /// <param name="doorUnlockDelay"></param>
    /// <returns></returns>
    private IEnumerator UnlockDoorsRoutine(float doorUnlockDelay)
    {
        if (doorUnlockDelay > 0f)
        {
            yield return new WaitForSeconds(doorUnlockDelay);
        }
        
        Door[] doorArray = GetComponentsInChildren<Door>();
        
        //Trigger opne doors
        foreach (Door door in doorArray)
        {
            door.UnlockDoor();
        }
        
        //Enable room trigger collider  启用房间触发碰撞体
        EnableRoomCollider();
    }

    /// <summary>
    /// Create Item Obstacles Array     创建物品障碍数组
    /// </summary>
    private void CreateItemObstaclesArray()
    {
        //this array will be populated during gameplay with any moveable obstacles
        //这个数组将在游戏过程中填充所有可移动的障碍物
        aStarItemObstacles = new int[room.templateUpperBounds.x - room.templateLowerBounds.x + 1,
            room.templateUpperBounds.y - room.templateLowerBounds.y + 1];
    }

    /// <summary>
    /// Initialize Item Obstacles Array with Default AStar Movement Penalty Values
    /// 初始化物品障碍物数组，使用默认的AStar移动惩罚值
    /// </summary>
    private void InitializeItemObstaclesArray()
    {
        for (int x = 0; x < (room.templateUpperBounds.x - room.templateLowerBounds.x + 1); x++)
        {
            for (int y = 0; y < (room.templateUpperBounds.y - room.templateLowerBounds.y + 1); y++) 
            {
                //Set default movement penalty for grid square  设置网格格子的默认移动惩罚值
                aStarItemObstacles[x, y] = Settings.defaultAStarMovementPenalty;
            }
        }
    }

    /// <summary>
    /// This is used for debugging - shows the position of the table obstacles  这是用于调试的——显示桌面障碍物的位置
    /// (Must be commented out updating room prefabs)   （必须注释掉更新房间预制件的代码）
    /// </summary>
    // private void OnDrawGizmos()
    // {
    //     for (int i = 0; i < (room.templateUpperBounds.x - room.templateLowerBounds.x + 1); i++)
    //     {
    //         for (int j = 0; j < (room.templateUpperBounds.y - room.templateLowerBounds.y + 1); j++) 
    //         {
    //             if (aStarItemObstacles[i, j] == 0)
    //             {
    //                 Vector3 worldCellPos = grid.CellToWorld(new Vector3Int(i + room.templateLowerBounds.x,
    //                     j + room.templateLowerBounds.y, 0));
    //
    //                 Gizmos.DrawWireCube(new Vector3(worldCellPos.x + 0.5f, worldCellPos.y + 0.5f, 0), Vector3.one);
    //             }
    //         }
    //     }
    // }

    /// <summary>
    /// Update the array of moveable obstacles
    /// </summary>
    public void UpdateMoveableObstacles()
    {
        InitializeItemObstaclesArray();

        foreach (MoveItem moveItem in moveableItemsList)
        {
            Vector3Int colliderBoundsMin = grid.WorldToCell(moveItem.boxCollider2D.bounds.min);
            Vector3Int colliderBoundsMax = grid.WorldToCell(moveItem.boxCollider2D.bounds.max);
            
            //Loop through and add moveable item bounds to obstacle array
            for (int i = colliderBoundsMin.x; i <= colliderBoundsMax.x; i++)
            {
                for (int j = colliderBoundsMin.y; j <= colliderBoundsMax.y; j++)
                {
                    aStarItemObstacles[i - room.templateLowerBounds.x, j - room.templateLowerBounds.y] = 0;
                }
            }
        }
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this, nameof(environmentGameObject), environmentGameObject);
    }
#endif

    #endregion
}
