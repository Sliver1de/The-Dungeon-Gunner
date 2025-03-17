using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Editor Code
#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;


    /// <summary>
    /// Initialite node
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="nodeGraph"></param>
    /// <param name="roomNodeType"></param>
    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;

        //Load room node type list  加载房间节点类型列表
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    /// <summary>
    /// Draw node with the nodestyle    使用节点样式绘制节点
    /// </summary>
    /// <param name="nodeStyle"></param>
    public void Draw(GUIStyle nodeStyle)
    {
        //Draw Node Box Using Begin Area    使用开始区域绘制节点框
        GUILayout.BeginArea(rect, nodeStyle);

        //start Region to detect popup selection changes    启动区域以检测弹出选择的变化
        EditorGUI.BeginChangeCheck();
        
        //if the room node has a parent or is of type entrance then display a label else display a popup
        //如果房间节点有父节点或属于入口类型，则显示标签，否则显示弹出窗口
        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            //dispaly a label that can't be changed     显示一个无法更改的标签
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            //display a popup using the RoomNOdeType name values that can be selected from (default to the currently set roomNodeType)
            //在弹出窗口中显示可供选择的 RoomNOdeType 名称值（默认为当前设置的 roomNodeType）
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];
            
            //if the room type selection has changed making child connections potentially invalid
            //如果房间类型选择发生变化，导致子连接可能无效
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor ||
                !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor ||
                !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        //Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);
                        
                        //if the child room node is not null
                        if (childRoomNode != null)
                        {
                            //Remove childID from parent room node
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                            
                            //Remove parentID from child room node
                            childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                        }
                    }
                }
            }
        }
        

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(this);
        }
        GUILayout.EndArea();
    }

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }

        return roomArray;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            //process mouse down event      处理鼠标按下事件
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;
            //处理鼠标抬起事件
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            //处理鼠标拖拽事件
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }
    }

    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }

    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;

        //Toggle node selection     切换节点选择
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        //按下鼠标右键，画出连线
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }

    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// add childid to the node (returns true if the node has been added, false otherwise)
    /// 为节点添加 childid（如果节点已添加，则返回 true，否则返回 false）
    /// </summary>
    /// <param name="childID"></param>
    public bool AddChildRoomNodeIDRoomNode(string childID)
    {
        //Check child node can be added validly to parent
        if (ISChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check the child node can be validly added to the parent node - return true if it can otherwise return false
    /// 检查子节点是否可以有效地添加到父节点 - 如果可以，则返回 true，否则返回 false
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public bool ISChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        //Check if there already a connected boss room in the node graph    检查节点图中是否已经存在相连的boss房间
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
            {
                isConnectedBossNodeAlready = true;
            }
        }
        
        //if the achild node has a type of boss room and there is already a connected boss room node then return false
        //如果子节点的类型是boss房间，并且已经有一个相连的boss房间节点，则返回 false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
            return false;
        
        //if the child room has a type of none then return false    如果子房间的类型为 "无"，则返回 false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;
        
        //if the node already has a child with this child id return false   如果该节点已经有一个具有该子节点 ID 的子节点，则返回 false
        if (childRoomNodeIDList.Contains(childID))
            return false;
        
        //if this node id and the child id are the same return false    如果此节点 ID 和子节点 ID 相同，则返回 false
        if (id == childID)
            return false;
        
        //if this child id is already in the parentID list return false     如果该子 ID 已在 parentID 列表中，则返回 false
        if (parentRoomNodeIDList.Contains(childID)) 
            return false;
        
        //if the child node already has a parent return false   如果子节点已有父节点，则返回 false
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0) 
            return false;
        
        //if child is a corridor and this node is a corridor return false   如果子节点是走廊，而此节点是走廊，则返回 false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor) 
            return false;
        
        //if child is not a corridor and this is not a corridor return false    如果子节点不是走廊，且此节点不是走廊，则返回 false
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor) 
            return false;
        
        //if adding a corridor check that this node has < the maximum permitted child corridors
        //如果添加走廊，则检查该节点是否有 < 允许的最大子走廊数
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor &&
            childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;
        
        //if the child room is an entrance return false - the entrance must always be the top level parent node
        //如果子房间是入口，则返回 false - 入口必须始终是父节点的顶层节点
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
            return false;
        
        //if adding a room to a corridor check that this corridor node doesn't already have a room added
        //如果要为房间添加走廊节点，请检查该走廊节点是否已经添加了房间
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0) 
            return false;

        return true;
    }

    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    /// <summary>
    /// Remove childID from the node (return true if the node has been removed, false otherwise)
    /// 从节点中移除 childID（如果节点已被移除，则返回 true，否则返回 false）
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        //if the node contains the child id then remove it  如果节点包含子节点 ID，则将其删除
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove parentID from the node (return true if the node has been removed, false otherwise)
    /// 从节点中移除 parentID（如果节点已被移除，则返回 true，否则返回 false）
    /// </summary>
    /// <param name="parentID"></param>
    /// <returns></returns>
    public bool RemoveParentRoomNodeIDFromRoomNode(string parentID)
    {
        //if the node contains the parent id then remove it 如果节点包含父节点 ID，则将其删除
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }
        return false;
    }

#endif
    #endregion


}
