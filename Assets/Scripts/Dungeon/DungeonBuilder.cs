using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class DungeonBuilder : SingletonMonobehaviour<DungeonBuilder>
{
    public Dictionary<string, Room> dungeonBuilderRoomDictionary = new Dictionary<string, Room>();
    private Dictionary<string, RoomTemplateSO> roomTemplateDictionary = new Dictionary<string, RoomTemplateSO>();
    private List<RoomTemplateSO> roomTemplateList = null;
    private RoomNodeTypeListSO roomNodeTypeList;
    private bool dungeonBuilderSuccessful;

    private void OnEnable()
    {
        //Set dimmed material to off    将暗淡材质设置为关闭
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 0f);
    }

    private void OnDisable()
    {
        //Set dimmed material to fully visible  将暗淡材质设置为完全可见
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }

    protected override void Awake()
    {
        base.Awake();
        
        //Load the room node type list  加载房间节点类型列表
        LoadRoomNodeTypeList();
        
        //Set dimmed material to fully visible  将变暗的材质设置为完全可见
        //GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }

    /// <summary>
    /// Load the room node type list    加载房间节点类型列表
    /// </summary>
    private void LoadRoomNodeTypeList()
    {
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public bool GenerateDungeon(DungeonLevelSO currentDungeonLevel)
    {
        roomTemplateList = currentDungeonLevel.roomTemplateList;
        
        //Load the scriptable object room templates into the dictionary 将可编写脚本的对象房间模板加载到字典中
        LoadRoomTemplatesIntoDictionary();

        dungeonBuilderSuccessful = false;
        int dungeonBuildAttempts = 0;

        while (!dungeonBuilderSuccessful && dungeonBuildAttempts < Settings.maxDungeonBuildAttempts) 
        {
            dungeonBuildAttempts++;
            
            //Select a random room node graph from the list     从列表中选择一个随机房间节点图
            RoomNodeGraphSO roomNodeGraph = SelectRandomRoomNodeGraph(currentDungeonLevel.roomNodeGraphList);

            int dungeonRebuildAttemptsForNodeGraph = 0;
            dungeonBuilderSuccessful = false;
            
            //Loop until dungeon successfully build or more than max attempts for node graph
            //循环直到地下城成功构建或超过节点图的最大尝试次数
            while (!dungeonBuilderSuccessful &&
                   dungeonRebuildAttemptsForNodeGraph <= Settings.maxDungeonRebulidAttemptsForRoomGraph) 
            {
                //Clear dungeon room gameobjects and dungeon room dictionary    清除地牢室游戏对象和地牢室字典
                ClearDungeon();

                dungeonRebuildAttemptsForNodeGraph++;
                
                //Attempt To Build a random Dungeon for the selected room node graph    尝试为选定的房间节点图构建随机地下城
                dungeonBuilderSuccessful = AttemptToBuildRandomDungeon(roomNodeGraph);
            }
            if (dungeonBuilderSuccessful)
            {
                //Instantiate Room Gameobjects  实例化房间游戏对象
                InstantiateRoomGameobjects();
            }
        }
        return dungeonBuilderSuccessful;
    }

    /// <summary>
    /// Load the room templates into the dictionary
    /// </summary>
    private void LoadRoomTemplatesIntoDictionary()
    {
        //Clear room template dictionary    清除房间模板字典
        roomTemplateDictionary.Clear();
        
        //Load room template list into dictionary   将房间模板列表加载到字典中
        foreach (RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if (!roomTemplateDictionary.ContainsKey(roomTemplate.guid))
            {
                roomTemplateDictionary.Add(roomTemplate.guid,roomTemplate);
            }
            else
            {
                Debug.Log("Duplicate Room Template Key In " + roomTemplateList);
            }
        }
    }

    /// <summary>
    /// Attempt to randomly build the dungeon for the specified room nodeGraph.
    /// Returns true if a succssful random layout was generated,else returns false
    /// if a problem was encoutered and another attempt is required.
    /// 尝试为指定房间节点图随机构建地牢。
    /// 如果随机布局生成成功则返回 true，
    /// 如果遇到问题并且需要再次尝试则返回 false
    /// </summary>
    /// <param name="roomNodeGraph"></param>
    /// <returns></returns>
    private bool AttemptToBuildRandomDungeon(RoomNodeGraphSO roomNodeGraph)
    {
        //Create Open Room Node Queue   创建开放房间节点队列
        Queue<RoomNodeSO> openRoomNodeQueue = new Queue<RoomNodeSO>();
        
        //Add Entrance Node To Room Node Queue From Room Node Graph 从房间节点图将入口节点添加到房间节点队列
        RoomNodeSO entranceNode = roomNodeGraph.GetRoomNode(roomNodeTypeList.list.Find(x => x.isEntrance));

        if (entranceNode != null)
        {
            openRoomNodeQueue.Enqueue(entranceNode);
        }
        else
        {
            Debug.Log("No Entrance Node");
            return false;   //Dungeon Not Build
        }
        
        //Start with no room overlaps   从没有房间重叠的地方开始
        bool noRoomOverlaps = true;
        
        //Process open room nodes queue
        noRoomOverlaps = ProcessRoomsInOpenRoomNodeQueue(roomNodeGraph, openRoomNodeQueue, noRoomOverlaps);
        
        //if all the room nodes have been processed and there hasn't been a room overlap then return true
        if (openRoomNodeQueue.Count == 0 && noRoomOverlaps)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Process rooms in the open room node queue,returing true if there are no room overlaps
    /// 处理开放房间节点队列中的房间，如果没有房间重叠则返回true
    /// </summary>
    /// <param name="roomNodeGraph"></param>
    /// <param name="openroomNodeQueue"></param>
    /// <param name="noRoomOverlaps"></param>
    /// <returns></returns>
    private bool ProcessRoomsInOpenRoomNodeQueue(RoomNodeGraphSO roomNodeGraph, Queue<RoomNodeSO> openroomNodeQueue,
        bool noRoomOverlaps)
    {
        //while room nodes in open room node queue & no room overlaps detected
        //当开放房间节点队列中的房间节点且未检测到房间重叠
        while (openroomNodeQueue.Count > 0 && noRoomOverlaps == true) 
        {
            //Get next room node from open room node queue  从开放房间节点队列中获取下一个房间节点
            RoomNodeSO roomNode = openroomNodeQueue.Dequeue();
            
            //Add child Nodes to queue from room node graph (with links to this parent Room)
            //将子节点添加到房间节点图中的队列（带有指向此父房间的链接）
            foreach (RoomNodeSO childRoomNode in roomNodeGraph.GetChildRoomNodes(roomNode))
            {
                openroomNodeQueue.Enqueue(childRoomNode);
            }
            
            //if the room is the entrance mark as positioned and add to room dictionary 如果房间是定位的入口标记并添加到房间字典中
            if (roomNode.roomNodeType.isEntrance)
            {
                RoomTemplateSO roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);

                Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

                room.isPositioned = true;
                
                //Add room to room dictionary   将房间添加到房间字典
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }
            //else if the room type isn't an entrance  如果房间类型不是入口
            else
            {
                //else get parent room for node 获取节点的父房间
                Room parentRoom = dungeonBuilderRoomDictionary[roomNode.parentRoomNodeIDList[0]];
                
                //See if room can be placed without overlaps    看看房间是否可以放置得不重叠
                noRoomOverlaps = CanPlaceRoomWithNoOverlaps(roomNode, parentRoom);
            }
        }

        return noRoomOverlaps;
    }

    /// <summary>
    /// Attempt to place the room node in the dungeon - if room can be placed return the room, else return null
    /// 尝试将房间节点放置在地牢中 - 如果可以放置房间则返回房间，否则返回 null
    /// </summary>
    /// <param name="room"></param>
    /// <param name="parentRoom"></param>
    /// <returns></returns>
    private bool CanPlaceRoomWithNoOverlaps(RoomNodeSO roomNode, Room parentRoom)
    {
        //Initialise and assume overlap until proven otherwise 初始假设有重叠，直到证明没有重叠为止。
        bool roomOverlaps = true;
        
        //Do while Room Overlaps - try to place against all available doorways of the parent 
        //until the room is successfully placed without overlap
        // 当房间重叠时执行循环 —— 尝试将房间放置在父房间所有可用的门口上，直到房间成功放置且没有重叠为止。
        while (roomOverlaps) 
        {
            //Select random unconnected available doorway for Parent
            List<Doorway> unconnectedAvailableParentDoorways =
                GetUnconnectedAvailableDoorways(parentRoom.doorwayList).ToList();

            if (unconnectedAvailableParentDoorways.Count == 0)
            {
                //if no more doorways to try then overlap failure   如果没有更多的门道可供尝试，则重叠失败
                return false;   //room overlaps
            }

            Doorway doorwayParent =
                unconnectedAvailableParentDoorways[
                    UnityEngine.Random.Range(0, unconnectedAvailableParentDoorways.Count)];
            
            //get a random room template for room node that is consistent with parent door orientation
            RoomTemplateSO roomTemplate = GetRandomTemplateForRoomConsistentWithParent(roomNode, doorwayParent);
            
            //Create a room
            Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);
            
            //Place the room - returns true if the room doesn't overlap 放置房间 - 如果房间不重叠则返回 true
            if (PlaceTheRoom(parentRoom, doorwayParent, room))
            {
                //If room doesn't overlap then set to false to exit while loop  如果房间不重叠则设置为 false 退出 while 循环
                roomOverlaps = false;
                
                //mark room as positioned   将房间标记为已定位
                room.isPositioned = true;
                
                //Add room to dictionary
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }
            else
            {
                roomOverlaps = true;
            }
        }

        return true;    //no room overlaps  没有房间重叠
    }

    /// <summary>
    /// Get random template for room node taking into account the parent doorway orientation
    /// 考虑父门口方向，获取房间节点的随机模板
    /// </summary>
    /// <param name="roomNode"></param>
    /// <param name="doorwayParent"></param>
    /// <returns></returns>
    private RoomTemplateSO GetRandomTemplateForRoomConsistentWithParent(RoomNodeSO roomNode, Doorway doorwayParent)
    {
        RoomTemplateSO roomTemplate = null;
        
        //if room node is a corridor then select random corrent Corridor room template based on parent doorway orientation
        //如果房间节点是走廊，则根据父门口方向随机选择当前走廊房间模板
        if (roomNode.roomNodeType.isCorridor)
        {
            switch (doorwayParent.orientation)
            {
                case Orientation.north:
                case Orientation.south:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorNS));
                    break;
                case Orientation.east:
                case Orientation.west:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorEW));
                    break;
                case Orientation.none:
                    break;
                default:
                    break;
            }
        }
        //else select random room tempalte
        else
        {
            roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
        }

        return roomTemplate;
    }

    /// <summary>
    /// Place the room - returns true if the room doesn't overlap, false otherwise
    /// </summary>
    /// <param name="parentRoom"></param>
    /// <param name="doorwayParent"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    private bool PlaceTheRoom(Room parentRoom, Doorway doorwayParent, Room room)
    {
        //get current room doorway position 获取当前房间门口位置
        Doorway doorway = GetOppositeDoorway(doorwayParent, room.doorwayList);
        
        //Return if no doorway in room opposite to parent doorway  如果父门口对面的房间没有门口，则返回
        if (doorway == null)
        {
            //Just mark the parent doorway as unavailable so we don't try and connect it again
            //只需将父门口标记为不可用，这样我们就不会尝试再次连接它
            doorwayParent.isUnavailable = true;
            return false;
        }
        
        //Calculate 'world' grid parent doorway position    计算“世界”网格父门口位置
        Vector2Int parentDoorWayPosition =
            parentRoom.lowerBounds + doorwayParent.position - parentRoom.templateLowerBounds;

        Vector2Int adjustment = Vector2Int.zero;
        
        //Calculate adjustment position offset based on room doorway position that we are trying to connect
        //(e.g. if this doorway is west then we need to add(1,0) to the east parent doorway)
        //根据我们尝试连接的房间门口位置计算调整位置偏移（例如，如果这个门口在西边，那么我们需要将（1,0）添加到东边的父门口）
        switch (doorway.orientation)
        {
            case Orientation.north:
                adjustment = new Vector2Int(0, -1);
                break;
            case Orientation.east:
                adjustment = new Vector2Int(-1, 0);
                break;
            case Orientation.south:
                adjustment = new Vector2Int(0, 1);
                break;
            case Orientation.west:
                adjustment = new Vector2Int(1, 0);
                break;
            case Orientation.none:
                break;
            default:
                break;
        }
        
        //Calculate room lower bounds and upper bounds based on positioning to align with parent doorway
        //根据与父门口对齐的定位计算房间下限和上限
        room.lowerBounds = parentDoorWayPosition + adjustment + room.templateLowerBounds - doorway.position;
        room.upperBounds = room.lowerBounds + room.templateUpperBounds - room.templateLowerBounds;

        Room overlappingRoom = CheckForRoomOverlap(room);

        if (overlappingRoom == null)
        {
            //mark doorways as connected & unavailable  将门口标记为已连接和不可用
            doorwayParent.isConnected = true;
            doorwayParent.isUnavailable = true;

            doorway.isConnected = true;
            doorway.isUnavailable = true;
            
            //return true to show rooms have been connected with no overlap 返回 true 表示房间已连接且没有重叠
            return true;
        }
        else
        {
            //Just mark the parent doorway as unavailable so we don't try and connect it again
            //只需将父门口标记为不可用，这样我们就不会尝试再次连接它
            doorwayParent.isUnavailable = true;
            
            return false;
        }
    }
    
    

    /// <summary>
    /// Get the doorway from the doorway list that has the opposite orientation to doorway
    /// 从门口列表中获取与门口方向相反的门口
    /// </summary>
    /// <param name="parentDoorway"></param>
    /// <param name="doorwayList"></param>
    /// <returns></returns>
    private Doorway GetOppositeDoorway(Doorway parentDoorway, List<Doorway> doorwayList)
    {
        foreach (Doorway doorwayToCheck in doorwayList)
        {
            if (parentDoorway.orientation == Orientation.east && doorwayToCheck.orientation == Orientation.west)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientation.west && doorwayToCheck.orientation == Orientation.east)
            {
                return doorwayToCheck;
            }
            else if(parentDoorway.orientation == Orientation.north && doorwayToCheck.orientation == Orientation.south)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientation.south && doorwayToCheck.orientation == Orientation.north)
            {
                return doorwayToCheck;
            }
        }
        return null;
    }

    /// <summary>
    /// Check for rooms that overlap the upper and lower bounds parameters,
    /// and if there are overlapping rooms then return room else return null
    /// 检查是否有与上下限参数重叠的房间，如果有重叠房间则返回房间，否则返回null
    /// </summary>
    /// <param name="roomToTest"></param>
    /// <returns></returns>
    private Room CheckForRoomOverlap(Room roomToTest)
    {
        //Iterate through all rooms
        foreach (KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary)
        {
            Room room = keyValuePair.Value;
            
            //skip if same room as room to test or rooom hasn't been positioned
            //如果与要测试的房间相同的房间或房间尚未定位，则跳过
            if (room.id == roomToTest.id || !room.isPositioned)
            {
                continue;
            }

            //If room overlaps  如果房间重叠
            if (IsOverLappingRoom(roomToTest, room))
            {
                return room;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if 2 rooms overlap each other - return true if they overlap or false if they don't overlap
    /// 检查 2 个房间是否相互重叠 - 如果重叠则返回 true，如果不重叠则返回 false
    /// </summary>
    /// <param name="room1"></param>
    /// <param name="room2"></param>
    /// <returns></returns>
    private bool IsOverLappingRoom(Room room1, Room room2)
    {
        bool isOverlappingX = IsOverLappingInterval(room1.lowerBounds.x, room1.upperBounds.x, room2.lowerBounds.x,
            room2.upperBounds.x);
        bool isOverlappingY = IsOverLappingInterval(room1.lowerBounds.y, room1.upperBounds.y, room2.lowerBounds.y,
            room2.upperBounds.y);

        if (isOverlappingX && isOverlappingY)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Check if interval 1 overlaps interval 2 - this method is used by the IsOverlappingRoom method
    /// 检查间隔 1 是否与间隔 2 重叠 - IsOverlappingRoom 方法使用此方法
    /// </summary>
    /// <param name="imin1"></param>
    /// <param name="imax1"></param>
    /// <param name="imin2"></param>
    /// <param name="imax2"></param>
    /// <returns></returns>
    private bool IsOverLappingInterval(int imin1, int imax1, int imin2, int imax2)
    {
        if (Mathf.Max(imin1, imin2) <= Mathf.Min(imax1, imax2))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Get a random room template from the roomtemplatelist that matches the roomType and return it
    /// (return null if no matching room template found)
    /// 从房间模板列表中获取与房间类型匹配的随机房间模板并返回（如果没有找到匹配的房间模板，则返回 null）
    /// </summary>
    /// <param name="roomNodeType"></param>
    /// <returns></returns>
    private RoomTemplateSO GetRandomRoomTemplate(RoomNodeTypeSO roomNodeType)
    {
        List<RoomTemplateSO> matchingRoomTemplateList = new List<RoomTemplateSO>();

        //Loop through room template list
        foreach (RoomTemplateSO roomTemplate in roomTemplateList)
        {
            //Add matching room templates
            if (roomTemplate.roomNodeType == roomNodeType)
            {
                matchingRoomTemplateList.Add(roomTemplate);
            }
        }
        
        //Return null if list is zero
        if (matchingRoomTemplateList.Count == 0)
        {
            return null;
        }
        //Select random room tempalte from list and return 从列表中选择随机房间模板并返回
        return matchingRoomTemplateList[UnityEngine.Random.Range(0, matchingRoomTemplateList.Count)];
    }

    /// <summary>
    /// Get unconnected doorways
    /// </summary>
    /// <param name="roomDoorwayList"></param>
    /// <returns></returns>
    private IEnumerable<Doorway> GetUnconnectedAvailableDoorways(List<Doorway> roomDoorwayList)
    {
        //loop through doorway list
        foreach (Doorway doorway in roomDoorwayList)
        {
            if (!doorway.isConnected && !doorway.isUnavailable)
            {
                yield return doorway;
            }
        }
    }

    //Create room based on roomTemplate and layoutNode, and return the created room
    //根据房间模板和布局节点创建房间，并返回创建的房间
    private Room CreateRoomFromRoomTemplate(RoomTemplateSO roomTemplate, RoomNodeSO roomNode)
    {
        //Initialise room from template
        Room room = new Room();

        room.templateID = roomTemplate.guid;
        room.id = roomNode.id;
        room.prefab = roomTemplate.prefab;
        room.battleMusic = roomTemplate.battleMusic;
        room.ambientMusic = roomTemplate.ambientMusic;
        room.roomNodeType = roomTemplate.roomNodeType;
        room.lowerBounds = roomTemplate.lowerBounds;
        room.upperBounds = roomTemplate.upperBounds;
        room.spawnPositionArray = roomTemplate.spawnPositionArray;
        room.enemiesByLevelList = roomTemplate.enemiesByLevelList;
        room.roomLevelEnemySpawnParametersList = roomTemplate.roomEnemySpawnParametersList;
        room.templateLowerBounds = roomTemplate.lowerBounds;
        room.templateUpperBounds = roomTemplate.upperBounds;

        room.childRoomIDList = CopyStringList(roomNode.childRoomNodeIDList);
        room.doorwayList = CopyDoorWayList(roomTemplate.doorwayList);
        
        //Set parent ID for room
        if (roomNode.parentRoomNodeIDList.Count == 0)
        {
            room.parentRoomID = "";
            room.isPreviouslyVisited = true;
            
            //Set entrance in game manager  在游戏管理器中设置入口
            GameManager.Instance.SetCurrentRoom(room);
        }
        else
        {
            room.parentRoomID = roomNode.parentRoomNodeIDList[0];
        }
        
        //if there are no enemies to spawn then default the room to be clear of enemies
        if (room.GetNumberOfEnemiesToSpawn(GameManager.Instance.GetCurrentDungeonLevel()) == 0)
        {
            room.isClearedOfEnemies = true;
        }

        return room;
    }


    /// <summary>
    /// Select a random room node graph from the list of node graphs
    /// </summary>
    /// <param name="roomNodeGraphList"></param>
    /// <returns></returns>
    private RoomNodeGraphSO SelectRandomRoomNodeGraph(List<RoomNodeGraphSO> roomNodeGraphList)
    {
        if (roomNodeGraphList.Count > 0)
        {
            return roomNodeGraphList[Random.Range(0, roomNodeGraphList.Count)];
        }
        else
        {
            Debug.Log("No room node graphs in list");
            return null;
        }
    }

    /// <summary>
    /// Create deep copy of doorway list    创建门口列表的深层副本
    /// </summary>
    /// <param name="oldDoorwayList"></param>
    /// <returns></returns>
    private List<Doorway> CopyDoorWayList(List<Doorway> oldDoorwayList)
    {
        List<Doorway> newDoorwayList = new List<Doorway>();

        foreach (Doorway doorway in oldDoorwayList)
        {
            Doorway newDoorway = new Doorway();

            newDoorway.position = doorway.position;
            newDoorway.orientation = doorway.orientation;
            newDoorway.doorPrefab = doorway.doorPrefab;
            newDoorway.isConnected = doorway.isConnected;
            newDoorway.isUnavailable = doorway.isUnavailable;
            newDoorway.doorwayStartCopyPosition = doorway.doorwayStartCopyPosition;
            newDoorway.doorwayCopyTileWidth = doorway.doorwayCopyTileWidth;
            newDoorway.doorwayCopyTileHeight = doorway.doorwayCopyTileHeight;
            
            newDoorwayList.Add(newDoorway);
        }
        
        return newDoorwayList;
    }

    /// <summary>
    /// Create deep copy of string list 创建字符串列表的深层副本
    /// </summary>
    /// <param name="oldStringList"></param>
    /// <returns></returns>
    private List<string> CopyStringList(List<string> oldStringList)
    {
        List<string> newStringList = new List<string>();
        foreach (string stringValue in oldStringList)
        {
            newStringList.Add(stringValue);
        }
        return newStringList;
    }

    /// <summary>
    /// Instantiate the dungeon room gameobjects from the prefabs   从预制件中实例化地牢房间游戏对象
    /// </summary>
    private void InstantiateRoomGameobjects()
    {
        //Iterate through all dungeon rooms
        foreach (KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary)
        {
            Room room = keyValuePair.Value;
            
            //Calculate room position (remember the room instantiation position to be adjusted by the room template lower bounds)
            //计算房间位置（记住要通过房间模板下界调整的房间实例化位置）
            Vector3 roomPosition = new Vector3(room.lowerBounds.x - room.templateLowerBounds.x,
                room.lowerBounds.y - room.templateLowerBounds.y, 0f);
            
            //Instantiate room
            GameObject roomGameObject = Instantiate(room.prefab, roomPosition, Quaternion.identity, transform);
            
            //Get instantiated room component from instantiated prefab  从实例化的预制件中获取实例化的房间组件
            InstantiatedRoom instantiatedRoom = roomGameObject.GetComponentInChildren<InstantiatedRoom>();
            
            instantiatedRoom.room = room;       //别忘了
            
            //Initialise the instantiated room  初始化实例化的房间
            instantiatedRoom.Initialise(roomGameObject);
            
            //Save gameobject reference 保存游戏对象引用
            room.instantiatedRoom = instantiatedRoom;
            
            //Demo code to set rooms as cleared - except for boss
            // if (!room.roomNodeType.isBossRoom)
            // {
            //     room.isClearedOfEnemies = true;
            // }
        }
    }

    /// <summary>
    /// Get a room template by room template ID, returns null if ID doesn't exist
    /// 通过房间模板ID获取房间模板，如果ID不存在则返回null
    /// </summary>
    /// <param name="roomTemplateID"></param>
    /// <returns></returns>
    public RoomTemplateSO GetRoomTemplate(string roomTemplateID)
    {
        if (roomTemplateDictionary.TryGetValue(roomTemplateID, out RoomTemplateSO roomTemplate))
        {
            return roomTemplate;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Get room by roomID, if no room exists with that ID return null
    /// 通过roomID获取房间，如果不存在该ID的房间则返回null
    /// </summary>
    /// <param name="roomID"></param>
    /// <returns></returns>
    public Room GetRoomByRoomID(string roomID)
    {
        if (dungeonBuilderRoomDictionary.TryGetValue(roomID, out Room room))
        {
            return room;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Clear dungeon room gameobjects and dungeon room dictionary  清除地牢室游戏对象和地牢室字典
    /// </summary>
    private void ClearDungeon()
    {
        //Destroy instantiated dungeon gameobjects and clear dungeon manager room dictionary
        //销毁实例化的地下城游戏对象并清除地下城经理室字典
        if (dungeonBuilderRoomDictionary.Count > 0)
        {
            foreach (KeyValuePair<string, Room> keyvaluepair in dungeonBuilderRoomDictionary)
            {
                Room room = keyvaluepair.Value;

                if (room.instantiatedRoom != null)
                {
                    Destroy(room.instantiatedRoom.gameObject);
                }
            }
            dungeonBuilderRoomDictionary.Clear();
        }
    }
}
