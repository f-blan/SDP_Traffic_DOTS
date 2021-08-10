using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//each node represent an intersection in our map
public class GraphNode
{
    private int index;
    private int x;
    private int y;

    //the directions our intersection allows to take (index: 0 = up, 1 = right, 2 = down, 3 = left)
    private bool[] goesTo;

    //might be needed
    private bool isWalkable;

    public GraphNode(int x, int y) {
        this.x = x;
        this.y = y;

        isWalkable = true;
        goesTo=null;
    }

    public GraphNode(int index, int x, int y) {
        this.x = x;
        this.y = y;
        this.index = index;

        isWalkable = true;
        goesTo=null;
    }

    public bool IsWalkable() {
        return isWalkable;
    }

    public void SetIsWalkable(bool isWalkable) {
        this.isWalkable = isWalkable;
    }

    public void SetGoesTo(bool[] v){
        
        if(v.Length!=4){
            return;
        }
        
        goesTo = new bool[v.Length];
        for(int t=0; t<v.Length; ++t){
            goesTo[t] = v[t];
        }
    }

    public bool[] GetGoesTo(){
        return goesTo;
    }
    
}
