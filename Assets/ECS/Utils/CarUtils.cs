using Unity.Mathematics;
using UnityEngine;
public static class CarUtils{
    
    public enum DirectionEnum{
        Up,
        Right,
        Down,
        Left
    }

    public static float2 ComputeVelocity(float maxSpeed, int direction){

        switch(direction){
            case (int)DirectionEnum.Up:
                return new float2(0f, maxSpeed);
            case (int)DirectionEnum.Down:
                return new float2(0f, -maxSpeed);
            case (int)DirectionEnum.Right:
                return new float2(maxSpeed, 0f);
            default:
                return new float2(-maxSpeed, 0f);
        }

    }

    public static float ComputeRotation(int direction){
        return -((direction + 2) % 4)*90f; //Changed after Francesco's suggestion
    }

    public static float2 ComputeOffset(float offset, int direction, int prevDirection, int nextDirection, out float2 intersectionOffset){
        float2 offsetFloat2;

        //If the vehicle has to perform a left turn, then, an extra tile must be moved.
        int prevOffset = ((math.abs(direction - prevDirection) == 1 && math.sign(direction - prevDirection) == -1.0f) || (prevDirection == (int)DirectionEnum.Up && direction == (int)DirectionEnum.Left)) ? 1 : 0;
        int nextOffset = (nextDirection - direction) == 0 || (math.abs(nextDirection - direction) == 1 && math.sign(nextDirection - direction) == -1.0f) || (direction == (int)DirectionEnum.Up && nextDirection == (int)DirectionEnum.Left) ? 1 : 0;

        // Debug 
        // Debug.Log("Prev direction: " + prevDirection + " Current direction: " + direction + " Next direction: " + nextDirection);
        // Debug.Log("Current cost: " + offset);
        // Debug.Log("Next offset: " + nextOffset);
        // Debug.Log("Prev offset: " + prevOffset);
        // End Debug

        switch(direction){
            case (int)DirectionEnum.Up:
                offsetFloat2 = new float2(0f, offset + nextOffset + prevOffset);
                intersectionOffset = new float2(0, offset + prevOffset -1);
                break;
            case (int)DirectionEnum.Down:
                offsetFloat2 = new float2(0f, -offset - nextOffset - prevOffset);
                intersectionOffset = new float2(0,-offset - prevOffset +1);
                break;
            case (int)DirectionEnum.Right:
                offsetFloat2 = new float2(offset + nextOffset + prevOffset, 0f);
                intersectionOffset = new float2(offset + prevOffset -1,0);
                break;
            default:
                offsetFloat2 = new float2(-offset - nextOffset - prevOffset, 0f);
                intersectionOffset =new float2( -offset -prevOffset +1,0);
                break;
        }

        return offsetFloat2;
    }

    public static float2 ComputeUTurn(int curDirection, int nextDirection){
        float2 uTurnFloat2 = new float2(0,0);

        if(curDirection != nextDirection && curDirection % 2 == 0 && nextDirection % 2 == 0){
            if(curDirection == (int)CarUtils.DirectionEnum.Up){
                uTurnFloat2.x = -1;
            }
            else{
                uTurnFloat2.x = 1;
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

    public static int GetLeftoverCost(int direction, float2 initialPosition, float3 currentPosition, int targetDirection, float2 offset){
        int offsetAdjustment;
        if(targetDirection == (direction +2)%4 || targetDirection == (direction+1)%4){
            
            offsetAdjustment=0;
        }else{
            offsetAdjustment=1;
            
        }

        switch(direction){
            case ((int)CarUtils.DirectionEnum.Up):
                return (int) math.floor(math.abs(offset.y) - math.abs(initialPosition.y - currentPosition.y)) - offsetAdjustment;
            case ((int)CarUtils.DirectionEnum.Right):
                return (int) math.floor(math.abs(offset.x) - math.abs(initialPosition.x - currentPosition.x)) - offsetAdjustment;
            case ((int)CarUtils.DirectionEnum.Down):
                return (int) math.floor(math.abs(offset.y) - math.abs(initialPosition.y - currentPosition.y)) - offsetAdjustment;
            default:
                return (int) math.floor(math.abs(offset.x) - math.abs(initialPosition.x - currentPosition.x)) - offsetAdjustment;
        }
    }
    public static int ComputeTurnState(int curDirection, int nextDirection){
        return ((nextDirection - curDirection)+4)%4;
    }

}


