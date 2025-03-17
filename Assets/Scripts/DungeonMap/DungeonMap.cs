using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class DungeonMap : SingletonMonobehaviour<DungeonMap>
{
    #region Header GameObject References

    [Space(10)]
    [Header("GameObject References")]

    #endregion

    #region Tooltip

    [Tooltip("Populate with the MinimapUI gameobject")]

    #endregion

    [SerializeField]
    private GameObject minimapUI;
    
    private Camera dungeonMapCamera;
    private Camera cameraMain;

    private void Start()
    {
        //Cache main camera     缓存主相机
        cameraMain = Camera.main;
        
        //Get player transform      获取玩家transform
        Transform playerTransform = GameManager.Instance.GetPlayer().transform;
        
        //Populate player as cinemachine camera target  将玩家设为 Cinemachine 摄像机的目标
        CinemachineVirtualCamera cinemachineVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        cinemachineVirtualCamera.Follow = playerTransform;
        
        //get dungeonmap camera     获取地下城地图摄像机
        dungeonMapCamera = GetComponentInChildren<Camera>();
        dungeonMapCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        //If mouse button pressed and gamestate is dungeon overview map then get the room clicked
        //如果鼠标按钮被按下且游戏状态为地牢总览地图，则获取被点击的房间
        if (Input.GetMouseButtonDown(0) && GameManager.Instance.gameState == GameState.dungeonOverviewMap)
        {
            GetRoomClicked();
        }
    }

    /// <summary>
    /// Get the room clicked on the map     获取地图上被点击的房间
    /// </summary>
    private void GetRoomClicked()
    {
        //Convert screen position to world position     将屏幕位置转换为世界位置
        Vector3 worldPosition = dungeonMapCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition = new Vector3(worldPosition.x, worldPosition.y, 0f);
        
        //Check for collisions at cursor position       检查光标位置是否有碰撞
        Collider2D[] collider2DArray = Physics2D.OverlapCircleAll(new Vector2(worldPosition.x, worldPosition.y), 1f);
        
        //Check if any of the colliders are a room      检查是否有任何碰撞体是一个房间
        foreach (Collider2D collider2D in collider2DArray)
        {
            if (collider2D.GetComponent<InstantiatedRoom>() != null)
            {
                InstantiatedRoom instantiatedRoom = collider2D.GetComponent<InstantiatedRoom>();
                
                //If clicked room is clear of enemies and previously visited then move player to the room
                //如果点击的房间已清除敌人且之前被访问过，则将玩家移动到该房间
                if (instantiatedRoom.room.isClearedOfEnemies && instantiatedRoom.room.isPreviouslyVisited)
                {
                    //Move player to room   将玩家移动到房间
                    StartCoroutine(MovePlayerToRoom(worldPosition, instantiatedRoom.room));
                }
            }
        }
    }

    /// <summary>
    /// Move the player to the selected room    将玩家移动到选定的房间
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    private IEnumerator MovePlayerToRoom(Vector3 worldPosition, Room room)
    {
        //Call room changed event   调用房间变更事件
        StaticEventHandler.CallRoomChangedEvent(room);

        //Fade out screen to black immediately  立即将屏幕渐变为黑色
        yield return StartCoroutine(GameManager.Instance.Fade(0f, 1f, 0f, Color.black));
        
        //clear dungeon overview    清除地牢概览
        ClearDungeonOverViewMap();
        
        //Disable player during the fade    在渐变期间禁用玩家
        GameManager.Instance.GetPlayer().playerControl.DisablePlayer();
        
        //Get nearest spawn point in room nearest to player     获取距离玩家最近的房间出生点
        Vector3 spawnPosition = HelperUtilities.GetSpawnPositionNearestToPlayer(worldPosition);
        
        //Move player to new location - spawning them at the closest spawn point    将玩家移动到新位置，并将其生成在离玩家最近的出生点
        GameManager.Instance.GetPlayer().transform.position = spawnPosition;
        
        //Fade the screen back in   淡入屏幕
        yield return StartCoroutine(GameManager.Instance.Fade(1f, 0f, 1f, Color.black));
        
        //Enable player     启用玩家
        GameManager.Instance.GetPlayer().playerControl.EnablePlayer();
    }

    /// <summary>
    /// Display dungeon overview map UI     显示地下城概览地图 UI
    /// </summary>
    public void DisplayDungeonOverViewMap()
    {
        //Set game state    设置游戏状态
        GameManager.Instance.previousGameState = GameManager.Instance.gameState;
        GameManager.Instance.gameState = GameState.dungeonOverviewMap;
        
        //Disable player    显示玩家
        GameManager.Instance.GetPlayer().playerControl.DisablePlayer();
        
        //Disable main camera and enable dungeon overview camera    禁用主摄像机并启用地牢概览摄像机
        cameraMain.gameObject.SetActive(false);
        dungeonMapCamera.gameObject.SetActive(true);
        
        //Ensure all rooms are active so they cam be displayed      确保所有房间都处于激活状态，以便显示
        ActivateRoomsForDisplay();
        
        //Disable Small Minimap UI      禁用小型迷你地图 UI
        minimapUI.SetActive(false);
    }

    /// <summary>
    /// Clear the dungeon overview map UI   清除地下城概览地图 UI
    /// </summary>
    public void ClearDungeonOverViewMap()
    {
        //Set game state
        GameManager.Instance.gameState = GameManager.Instance.previousGameState;
        GameManager.Instance.previousGameState = GameState.dungeonOverviewMap;
        
        //Enable player
        GameManager.Instance.GetPlayer().playerControl.EnablePlayer();
        
        //Enable main camera and disable dungeon overview camera    启用主摄像头并禁用地下城概览摄像头
        cameraMain.gameObject.SetActive(true);
        dungeonMapCamera.gameObject.SetActive(false);
        
        //Enable Small Minimap UI   启用小型迷你地图 UI
        minimapUI.SetActive(true);
    }

    /// <summary>
    /// Ensure all rooms are active so they can be displayed    确保所有房间都已激活，以便它们可以显示
    /// </summary>
    private void ActivateRoomsForDisplay()
    {
        //Iterate through dungeon rooms     迭代遍历地下城的所有房间
        foreach (KeyValuePair<string, Room> keyValuePair in DungeonBuilder.Instance.dungeonBuilderRoomDictionary)
        {
            Room room = keyValuePair.Value;
            
            room.instantiatedRoom.gameObject.SetActive(true);
        }
    }
}
