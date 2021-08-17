using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;

public static class CarUtils{

    public enum DirectionEnum{
        Up,
        Right,
        Down,
        Left
    }

    public static float2 ComputeVelocity(float maxSpeed, int direction){
        float2 velocity = new float2();

        if(direction == ((int)DirectionEnum.Up)){
            velocity = new float2(0f, maxSpeed);
        }
        else if(direction == ((int)DirectionEnum.Down)){
            velocity = new float2(0f, -maxSpeed);
        }
        else if(direction == ((int)DirectionEnum.Right)){
            velocity = new float2(maxSpeed, 0f);
        }
        else{
            velocity = new float2(-maxSpeed, 0f);
        }
        return velocity;
    }

    public static float ComputeRotation(int direction){
        if(direction == (int)DirectionEnum.Up){
            return 2*90f;
        }
        else if(direction == (int)DirectionEnum.Down){
            return 0;
        }
        else{
            return direction*90f;
        }
    }

    public static float2 ComputeOffset(float offset, int direction, int prevDirection, int nextDirection){
        float2 offsetFloat2;

        //If the vehicle has to perform a left turn, then, an extra tile must be moved.
        int prevOffset = ((math.abs(direction - prevDirection) == 1 && math.sign(direction - prevDirection) == -1.0f) || (prevDirection == (int)DirectionEnum.Up && direction == (int)DirectionEnum.Left)) ? 1 : 0;
        int nextOffset = (nextDirection - direction) == 0 || (math.abs(nextDirection - direction) == 1 && math.sign(nextDirection - direction) == -1.0f) || (direction == (int)DirectionEnum.Up && nextDirection == (int)DirectionEnum.Left) ? 1 : 0;

        // Debug 
        // Debug.Log("Prev direction: " + prevDirection + " Current direction: " + direction + " Next direction: " + nextDirection);
        // Debug.Log("Current cost: " + offset);
        // Debug.Log("Next offset: " + nextOffset);
        // End Debug

        switch(direction){
            case (int)DirectionEnum.Up:
                offsetFloat2 = new float2(0f, offset + nextOffset + prevOffset);
                break;
            case (int)DirectionEnum.Down:
                offsetFloat2 = new float2(0f, -offset - nextOffset - prevOffset);
                break;
            case (int)DirectionEnum.Right:
                offsetFloat2 = new float2(offset + nextOffset + prevOffset, 0f);
                break;
            default:
                offsetFloat2 = new float2(-offset - nextOffset - prevOffset, 0f);
                break;
        }

        return offsetFloat2;
    }

    public static float2 ComputeUTurn(int curDirection, int nextDirection){
        float2 uTurnFloat2 = new float2(0,0);

        if(curDirection != nextDirection && curDirection % 2 == 0 && nextDirection % 2 == 0){
            if(curDirection == (int)CarUtils.DirectionEnum.Up){
                uTurnFloat2.x = 1;
            }
            else{
                uTurnFloat2.x = -1;
            }
        }
        else if(curDirection != nextDirection && curDirection % 2 == 1 && nextDirection % 2 == 1){
            if(curDirection == (int)CarUtils.DirectionEnum.Right){
                uTurnFloat2.y = 1;
            }
            else{
                uTurnFloat2.y = -1;
            }

        }

        return uTurnFloat2;
    }

    public static bool ComputeReachedDestination(int direction, float2 initialPosition, float2 offset, float3 currentPosition){
        switch(direction){
                case ((int)CarUtils.DirectionEnum.Up):
                    return initialPosition.y + offset.y <= currentPosition.y;
                case ((int)CarUtils.DirectionEnum.Right):
                    return initialPosition.x + offset.x <= currentPosition.x;
                case ((int)CarUtils.DirectionEnum.Down):
                    return initialPosition.y + offset.y >= currentPosition.y;
                default:
                    return initialPosition.x + offset.x >= currentPosition.x;
            }
    }

}


