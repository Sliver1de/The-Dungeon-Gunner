using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarTest : MonoBehaviour
{
    private InstantiatedRoom instantiatedRoom;
    private Grid grid;
    private Tilemap frontTilemap;
    private Tilemap pathTilemap;
    private Vector3Int startGridPosition;
    private Vector3Int endGridPosition;
    private TileBase startPathTile;
    private TileBase finishPathTile;

    private Vector3Int noValue = new Vector3Int(9999, 9999, 9999);
    private Stack<Vector3> pathStack;

    private void OnEnable()
    {
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;
    }

    private void OnDisable()
    {
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
    }

    private void Start()
    {
        startPathTile = GameResources.Instance.preferredEnemyPathTile;
        finishPathTile = GameResources.Instance.enemyUnwalkableCollisionTileArray[0];
    }

    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        pathStack = null;
        instantiatedRoom = roomChangedEventArgs.room.instantiatedRoom;
        frontTilemap = instantiatedRoom.transform.Find("Grid/Tilemap4_Front").GetComponent<Tilemap>();
        grid = instantiatedRoom.transform.GetComponentInChildren<Grid>();
        startGridPosition = noValue;
        endGridPosition = noValue;

        SetUpPathTilemap();
    }

    /// <summary>
    /// Use a clone of the front tilemap for the path tilemap. if not created then create one, else use the existing one.
    /// 使用前方 Tilemap 的克隆作为路径 Tilemap。如果尚未创建，则创建一个，否则使用现有的 Tilemap
    /// </summary>
    private void SetUpPathTilemap()
    {
        Transform tilemapCloneTransform=instantiatedRoom.transform.Find("Grid/Tilemap4_Front(Clone)");
        
        //if the front tilemap hasn't been cloned then clone it
        if (tilemapCloneTransform == null)
        {
            pathTilemap = Instantiate(frontTilemap, grid.transform);
            pathTilemap.GetComponent<TilemapRenderer>().sortingOrder = 2;
            pathTilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
            pathTilemap.gameObject.tag = "Untagged";
        }
        //else use it
        else
        {
            pathTilemap=instantiatedRoom.transform.Find("Grid/Tilemap4_Front(Clone)").GetComponent<Tilemap>();
            pathTilemap.ClearAllTiles();
        }
    }

    private void Update()
    {
        if (instantiatedRoom == null || startPathTile == null || finishPathTile == null || grid == null ||
            pathTilemap == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ClearPath();
            SetStartPosition();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ClearPath();
            SetEndPosition();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            DisplayPath();
        }
    }

    /// <summary>
    /// Set the start position and the start tile on the front tilemap  在前方 Tilemap 上设置起始位置和起始瓦片
    /// </summary>
    private void SetStartPosition()
    {
        if (startGridPosition == noValue)
        {
            startGridPosition = grid.WorldToCell(HelperUtilities.GetMouseWorldPosition());

            if (!IsPositionWithinBounds(startGridPosition))
            {
                startGridPosition = noValue;
                return;
            }

            pathTilemap.SetTile(startGridPosition, startPathTile);
        }
        else
        {
            pathTilemap.SetTile(startGridPosition, null);
            startGridPosition = noValue;
        }
    }

    /// <summary>
    /// Set the end position and the end tile on the front tilemap  在前方 Tilemap 上设置终点位置和起始瓦片
    /// </summary>
    private void SetEndPosition()
    {
        if (endGridPosition == noValue)
        {
            endGridPosition = grid.WorldToCell(HelperUtilities.GetMouseWorldPosition());

            if (!IsPositionWithinBounds(endGridPosition))
            {
                endGridPosition = noValue;
                return;
            }
            
            pathTilemap.SetTile(endGridPosition, finishPathTile);
        }
        else
        {
            pathTilemap.SetTile(endGridPosition,null);
            endGridPosition = noValue;
        }
    }

    /// <summary>
    /// Check if the position is within the lower and upper bounds of the room  检查位置是否在房间的下界和上界范围内
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsPositionWithinBounds(Vector3Int position)
    {
        //if position is beyond grid then return false
        if (position.x < instantiatedRoom.room.templateLowerBounds.x ||
            position.x > instantiatedRoom.room.templateUpperBounds.x ||
            position.y < instantiatedRoom.room.templateLowerBounds.y ||
            position.y > instantiatedRoom.room.templateUpperBounds.y)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Clear the path and reset the start and finish position  清除路径并重置起始点和终点位置
    /// </summary>
    private void ClearPath()
    {
        //Clear Path
        if(pathStack == null) return;

        foreach (Vector3 worldPosition in pathStack)
        {
            pathTilemap.SetTile(grid.WorldToCell(worldPosition), null);
        }

        pathStack = null;
        
        //Clear Start and Finish Squares    清除起始点和终点方块
        endGridPosition = noValue;
        startGridPosition = noValue;
    }

    /// <summary>
    /// Build and display the AStar path between the start and finish position  构建并显示起点和终点之间的A*路径
    /// </summary>
    private void DisplayPath()
    {
        if (startGridPosition == noValue || endGridPosition == noValue) return;

        pathStack = AStar.BuildPath(instantiatedRoom.room, startGridPosition, endGridPosition);
        
        if (pathStack == null) return;

        foreach (Vector3 worldPosition in pathStack)
        {
            pathTilemap.SetTile(grid.WorldToCell(worldPosition), startPathTile);
        }
    }
}
