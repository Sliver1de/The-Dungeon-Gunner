using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperUtilities
{
    public static Camera mainCamera;
    
    /// <summary>
    /// Get the mouse world position    获取鼠标世界位置
    /// </summary>
    /// <returns></returns>
    public static Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        
        //Clamp mouse position to screen size将鼠标位置固定到屏幕尺寸
        mouseScreenPosition.x = Mathf.Clamp(mouseScreenPosition.x, 0f, Screen.width);
        mouseScreenPosition.y = Mathf.Clamp(mouseScreenPosition.y, 0f, Screen.height);

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        
        worldPosition.z = 0f;

        return worldPosition;
    }

    /// <summary>
    /// Get the camera viewport lower and upper bounds
    /// </summary>
    /// <param name="cameraWorldPositionLowerBounds"></param>
    /// <param name="cameraWorldPositionUpperBounds"></param>
    /// <param name="camera"></param>
    public static void CameraWorldPositionBounds(out Vector2Int cameraWorldPositionLowerBounds,
        out Vector2Int cameraWorldPositionUpperBounds, Camera camera)
    {
        Vector3 worldPositionViewPortBottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 worldPositionViewPortTopRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        cameraWorldPositionLowerBounds = new Vector2Int((int)worldPositionViewPortBottomLeft.x,
            (int)worldPositionViewPortBottomLeft.y);
        cameraWorldPositionUpperBounds =
            new Vector2Int((int)worldPositionViewPortTopRight.x, (int)worldPositionViewPortTopRight.y);
    }

    /// <summary>
    /// Get the angle in degrees from a direction vector 从方向向量获取角度（以度为单位）
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static float GetAngleFromVector(Vector3 vector)
    {
        float radians = Mathf.Atan2(vector.y, vector.x);

        float degrees = radians * Mathf.Rad2Deg;
        
        return degrees;
    }

    /// <summary>
    /// Get the direction vector from an angle degrees  从角度（度）获取方向向量
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector3 GetDirectionVectorFromAngle(float angle)
    {
        Vector3 directionVector = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle), 0f);
        return directionVector;
    }

    /// <summary>
    /// Get AimDirection enum value from the pased in angleDegrees  从传入的角度（angleDegrees）获取 AimDirection 枚举值
    /// </summary>
    /// <param name="angleDegrees"></param>
    /// <returns></returns>
    public static AimDirection GetAimDirection(float angleDegrees)
    {
        AimDirection aimDirection;
        
        //Set player direction
        if (angleDegrees >= 22f && angleDegrees <= 67f)
        {
            aimDirection = AimDirection.UpRight;
        }
        else if (angleDegrees > 67f && angleDegrees <= 112f)
        {
            aimDirection = AimDirection.Up;
        }
        else if (angleDegrees > 112f && angleDegrees <= 158f)
        {
            aimDirection = AimDirection.UpLeft;
        }
        else if ((angleDegrees > 158f && angleDegrees <= 180f) || (angleDegrees > -180f && angleDegrees <= -135f))
        {
            aimDirection = AimDirection.Left;
        }
        else if (angleDegrees > -135f && angleDegrees <= -45f)
        {
            aimDirection = AimDirection.Down;
        }
        else if ((angleDegrees > -45f && angleDegrees <= 0f) || (angleDegrees > 0 && angleDegrees < 22f))
        {
            aimDirection = AimDirection.Right;
        }
        else
        {
            aimDirection = AimDirection.Right;
        }
        return aimDirection;
    }

    /// <summary>
    /// Convert the linear volume scale to decibels 将线性音量尺度转换为分贝（dB）
    /// </summary>
    /// <param name="linear"></param>
    /// <returns></returns>
    public static float LinearToDecibels(int linear)
    {
        float linearScaleRange = 20f;
        
        //formula to convert from the linear scale to the logarithmic decibel scale 将线性音量尺度转换为对数分贝（dB）尺度
        //dB=20×log.10(linear_value)
        return Mathf.Log10((float)linear / linearScaleRange) * 20;
    }

    /// <summary>
    /// Empty string debug check    空字符串调试检查
    /// 空字符串调试检查
    /// </summary>
    /// <param name="thisOnject"></param>
    /// <param name="fileName"></param>
    /// <param name="stringToCheck"></param>
    /// <returns></returns>
    public static bool ValidateCheckEmptyString(Object thisObject, string fileName, string stringToCheck)
    {
        if (stringToCheck == "")
        {
            Debug.Log(fileName + " is empty and must contain a value in object " + thisObject.name.ToString());
            return true;
        }
        return false;
    }

    /// <summary>
    /// null value debug check  空值调试检查
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fileName"></param>
    /// <param name="objectToCheck"></param>
    /// <returns></returns>
    public static bool ValidateCheckNullValue(Object thisObject, string fileName, Object objectToCheck)
    {
        if (objectToCheck == null)
        {
            Debug.Log(fileName + " is null and must contain a value in object" + thisObject.name.ToString());
            return true;
        }

        return false;
    }

    /// <summary>
    /// list empty or contains null value check - returns true if there is an error
    /// 列表为空或包含空值检查 - 如果出现错误，返回 true
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fileName"></param>
    /// <param name="enumerableObjectToCheck"></param>
    /// <returns></returns>
    public static bool ValidateCheckEnumerableValues(Object thisObject, string fileName, IEnumerable enumerableObjectToCheck)
    {
        bool error = false;
        int count = 0;

        if (enumerableObjectToCheck == null)
        {
            Debug.Log(fileName + " is null in object " + thisObject.name.ToString());
            return true;
        }

        foreach (var item in enumerableObjectToCheck)
        {
            if (item == null)
            {
                Debug.Log(fileName + " has null values in object" + thisObject.name.ToString());
                error = true;
            }
            else
            {
                count++;
            }
        }

        if (count == 0)
        {
            Debug.Log(fileName + " has no values in object" + thisObject.name.ToString());
            error = true;
        }
        return error;
    }

    /// <summary>
    /// positive value debug check if zero is allowed set isZeroAllowed to true. Returns true if there is an error
    /// 正值调试检查，如果允许零值，则将 isZeroAllowed 设置为 true。如果存在错误，则返回 true。”
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fileName"></param>
    /// <param name="valueToCheck"></param>
    /// <param name="isZeroAllowed"></param>
    /// <returns></returns>
    public static bool ValidateCheckPositiveValue(Object thisObject, string fileName, int valueToCheck,
        bool isZeroAllowed)
    {
        bool error = false;

        if (isZeroAllowed)
        {
            if (valueToCheck < 0)
            {
                Debug.Log(fileName + " must contain a positive value or zero in object " + thisObject.name.ToString());
                error = true;
            }
        }
        else
        {
            if (valueToCheck <= 0)
            {
                Debug.Log(fileName + " must contain a positive value in object " + thisObject.name.ToString());
                error = true;
            }
        }
        return error;
    }

    public static bool ValidateCheckPositiveValue(Object thisObject, string fileName, float valueToCheck,
        bool isZeroAllowed)
    {
        bool error = false;

        if (isZeroAllowed)
        {
            if (valueToCheck < 0)
            {
                Debug.Log(fileName + " must contain a positive value or zero in object " + thisObject.name.ToString());
                error = true;
            }
        }
        else
        {
            if (valueToCheck <= 0)
            {
                Debug.Log(fileName + " must contain a positive value in object " + thisObject.name.ToString());
                error = true;
            }
        }
        return error;
    }

    /// <summary>
    /// positive range debug check - set isZeroAllow to true if the min and max range values can both be zero.
    /// returns true if there is an error
    /// 正范围调试检查 —— 如果最小值和最大值范围都可以为零，则将 isZeroAllowed 设置为 true。如果存在错误，则返回 true。
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fieldNameMinimum"></param>
    /// <param name="valueToCheckMinimum"></param>
    /// <param name="fieldNameMaximum"></param>
    /// <param name="valueToCheckMaximum"></param>
    /// <param name="isZeroAllowed"></param>
    /// <returns></returns>
    public static bool ValidateCheckPositiveRange(Object thisObject, string fieldNameMinimum, float valueToCheckMinimum,
        string fieldNameMaximum, float valueToCheckMaximum, bool isZeroAllowed)
    {
        bool error = false;
        if (valueToCheckMinimum > valueToCheckMaximum)
        {
            Debug.Log(fieldNameMinimum + " must be less than or equal to " + fieldNameMaximum + " in object " +
                      thisObject.name.ToString());
            error = true;
        }

        if (ValidateCheckPositiveValue(thisObject, fieldNameMinimum, valueToCheckMinimum, isZeroAllowed))
        {
            error = true;
        }

        if (ValidateCheckPositiveValue(thisObject, fieldNameMaximum, valueToCheckMaximum, isZeroAllowed))
        {
            error = true;
        }

        return error;
    }
    
    /// <summary>
    /// positive range debug check - set isZeroAllow to true if the min and max range values can both be zero.
    /// returns true if there is an error
    /// 正范围调试检查 —— 如果最小值和最大值范围都可以为零，则将 isZeroAllowed 设置为 true。如果存在错误，则返回 true。
    /// </summary>
    public static bool ValidateCheckPositiveRange(Object thisObject, string fieldNameMinimum, int valueToCheckMinimum,
        string fieldNameMaximum, float valueToCheckMaximum, bool isZeroAllowed)
    {
        bool error = false;
        if (valueToCheckMinimum > valueToCheckMaximum)
        {
            Debug.Log(fieldNameMinimum + " must be less than or equal to " + fieldNameMaximum + " in object " +
                      thisObject.name.ToString());
            error = true;
        }

        if (ValidateCheckPositiveValue(thisObject, fieldNameMinimum, valueToCheckMinimum, isZeroAllowed))
        {
            error = true;
        }

        if (ValidateCheckPositiveValue(thisObject, fieldNameMaximum, valueToCheckMaximum, isZeroAllowed))
        {
            error = true;
        }

        return error;
    }

    /// <summary>
    /// Get the nearest spawn position to the player
    /// </summary>
    /// <param name="playerPosition"></param>
    /// <returns></returns>
    public static Vector3 GetSpawnPositionNearestToPlayer(Vector3 playerPosition)
    {
        Room currentRoom = GameManager.Instance.GetCurrentRoom();

        Grid grid = currentRoom.instantiatedRoom.grid;

        Vector3 nearestSpawnPosition = new Vector3(10000f, 10000f, 0f);
        
        //Loop through room spawn position
        foreach (Vector2Int spawnPositionGrid in currentRoom.spawnPositionArray)
        {
            //convert the spawn grid position to world position 将生成网格位置转换为世界位置
            Vector3 spawnPositionWorld = grid.CellToWorld((Vector3Int)spawnPositionGrid);

            if (Vector3.Distance(spawnPositionWorld, playerPosition) <
                Vector3.Distance(nearestSpawnPosition, playerPosition))
            {
                nearestSpawnPosition = spawnPositionWorld;
            }
        }
        return nearestSpawnPosition;
    }
}