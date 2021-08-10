using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapUtils 
{
    public static void InitializeMap(Map<MapTile> CityMap, PathFindGraph CityGraph, int index, int n_districts_x, int n_districts_y){
        int[,] i_to_n;
        int[,] bs_to_n;
        MapTile.TileType[,] districtImage = MapUtils.GetDistrictImage(index, out i_to_n, out bs_to_n);
        GraphNode[,] graphDistrictImage = MapUtils.GetGraphDistrictImage(index);

        int m_x = 0;
        int m_y = 0;

        int g_x = 0;
        int g_y = 0;

        
        //initialize CityMap
        for(int t = 0; t<n_districts_y; ++t){

            for(int d_y=0; d_y<districtImage.GetLength(0); ++d_y){
                m_x=0;
                for( int s = 0; s<n_districts_x; ++s){
                    
                    for( int d_x=0; d_x<districtImage.GetLength(1); ++d_x){
                       
                        if(m_x==0 || m_y==0 || m_x== CityMap.GetWidth()-1 || m_y== CityMap.GetHeight()-1){
                            CityMap.GetMapObject(m_x,m_y).SetTileType(MapTile.TileType.Obstacle);    
                        }else{
                            CityMap.GetMapObject(m_x,m_y).SetTileType(districtImage[d_y,d_x]);
                        }
                        m_x++;

                    }
                }
                m_y++;
            }
        }

        //initialize CityGraph
        for(int t=0; t<n_districts_y; ++t){

            for(int d_y = 0; d_y<graphDistrictImage.GetLength(0); ++d_y){
                g_x=0;

                for(int s =0; s<n_districts_x; ++s){

                    for(int d_x=0; d_x<graphDistrictImage.GetLength(1); ++d_x){
                        bool[] goesTo = graphDistrictImage[d_y, d_x].GetGoesTo();
                        CityGraph.GetGraphNode(g_x, g_y).SetGoesTo( goesTo);

                    }
                    g_x++;
                }
                g_y++;
            }
        }

        //link intersections to graphnodes (so that given your position you can know what node you're in)
        for(int y=0; y<n_districts_y; ++y){
            for(int x =0 ; x<n_districts_x; ++x){

                for(int t=0; t< i_to_n.GetLength(0); ++t){
                    CityMap.GetMapObject(x,y, i_to_n[t, 0], i_to_n[t, 1]).setGraphNode(CityGraph.GetGraphNode(x,y,i_to_n[t, 2], i_to_n[t, 3]));
                    CityMap.GetMapObject(x,y, i_to_n[t, 0]+1, i_to_n[t, 1]).setGraphNode(CityGraph.GetGraphNode(x,y,i_to_n[t, 2], i_to_n[t, 3]));
                    CityMap.GetMapObject(x,y, i_to_n[t, 0], i_to_n[t, 1]+1).setGraphNode(CityGraph.GetGraphNode(x,y,i_to_n[t, 2], i_to_n[t, 3]));
                    CityMap.GetMapObject(x,y, i_to_n[t, 0]+1, i_to_n[t, 1]+1).setGraphNode(CityGraph.GetGraphNode(x,y,i_to_n[t, 2], i_to_n[t, 3]));
                }

                //same for bus stops
                for(int t=0; t<bs_to_n.GetLength(0); ++t){
                    CityMap.GetMapObject(x,y, bs_to_n[t, 0], bs_to_n[t, 1]).setGraphNode(CityGraph.GetGraphNode(x,y,bs_to_n[t, 2], bs_to_n[t, 3]));
                    CityMap.GetMapObject(x,y, bs_to_n[t, 0]+1, bs_to_n[t, 1]).setGraphNode(CityGraph.GetGraphNode(x,y,bs_to_n[t, 2], bs_to_n[t, 3]));
                }

            }
        }


    }

    public static MapTile.TileType[,] GetDistrictImage(int index, out int[,] intersectionTiles, out int[,] busStopTiles){
        //define here the map of a district


        //for brevity write it as int[,] with 0 = obstacle, 1 = road, 2 = parkSpot, 3 = traffic light, 4 = intersection, 5 = busStop
        int[,] image;
        switch (index){
            case 0:
                image = new int[,]{
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                    {2, 2, 2, 2, 0, 3, 1, 0, 2, 2, 2, 2,},
                    {1, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1,},
                    {1, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1,},
                    {2, 2, 2, 2, 0, 1, 3, 0, 2, 2, 2, 2,},
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                    {0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0,},
                };

                //this will be used in Map_Setup (initializeMap) to link the intersection tile to the graphNode (i think we need a way to get the node from the tile coords)
                //since it's easier if each intersection is composed of 4 tiles, just specify (for each intersection) the bottom left tile coordinates
                //and the coordinates of the graph node you want them to be linked to
                intersectionTiles = new int[,]{
                    { 5, 4, 0, 0 },
                };
                busStopTiles = new int[0,0];
                break;
            case 1:
                image = new int[,]{
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 2, 0, 3, 1, 0, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 1, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 1, 1 },
                    { 2, 0, 1, 3, 0, 2, 2, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 0, 2, 2, 2, 2, 0, 1, 1, 0, 2, 2, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 0, 2, 2, 0, 1, 1, 0, 0, 2, 2, 2, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 5, 5, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1, 0, 0, 2, 2, 2, 2, 2, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 0, 0, 1, 3, 0, 0, 2, 2, 2, 2, 2, 0, 1, 3, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                };

                intersectionTiles= new int[,]{
                    {2, 2, 0, 0},
                    {10, 2, 1, 0},
                    {16, 2, 2, 0},
                    {26, 2, 3, 0},

                    {2, 10, 0, 1},
                    {10, 10, 1, 1},
                    {16, 10, 2, 1},
                    {26, 10, 3, 1},

                    {2, 16, 0, 2},
                    {10, 16, 1, 2},
                    {16, 16, 2, 2},
                    {26, 16, 3, 2}
                };

                busStopTiles = new int[,]{
                    {6, 4, 0, 0},
                    {12, 1, 1, 0}
                };

                break;
            default:
                image = null;
                intersectionTiles = null;
                busStopTiles=null;
                break;
        }


       
        MapTile.TileType[,] ret = new MapTile.TileType[image.GetLength(0), image.GetLength(1)];

        int r_y=0;
        //translate into TileType[,] and mirror (so that it looks coherent to how you draw it)
        for(int x = 0; x<image.GetLength(1); ++x){
            r_y = 0;
            for(int y = image.GetLength(0)-1; y>=0; --y){
                switch(image[y,x]){
                    case 0:
                        ret[r_y,x] = MapTile.TileType.Obstacle;
                        break;
                    case 1:
                        ret[r_y,x] = MapTile.TileType.Road;
                        break;
                    case 2:
                        ret[r_y,x] = MapTile.TileType.ParkSpot;
                        break;
                    case 3:
                        ret[r_y,x] = MapTile.TileType.TrafficLight;
                        break;
                    case 4:
                        ret[r_y,x] = MapTile.TileType.Intersection;
                        break;
                    case 5:
                        ret[r_y, x] = MapTile.TileType.BusStop;
                        break;
                    default:
                        break;
                }
                r_y++;
            }
        }
        return ret;
    }

    public static GraphNode[,] GetGraphDistrictImage(int index){
        
        GraphNode[,] ret;

        //Must be coherent to the DistrictImage of the same index
        switch(index){
            case 0:
                ret = new GraphNode[1,1];
                ret[0,0] = new GraphNode(0,0);
                
                ret[0,0].SetGoesTo(new bool[]{true,true,true,true});
            break;
            case 1:
                ret = new GraphNode[3,4];

                ret[0,0]=new GraphNode(0,0);
                ret[0,0].SetGoesTo(new bool[]{false,true,true,true});
                ret[0,1]=new GraphNode(1,0);
                ret[0,1].SetGoesTo(new bool[]{false,true,false,true});
                ret[0,2]=new GraphNode(2,0);
                ret[0,2].SetGoesTo(new bool[]{true,true,true,true});
                ret[0,3]=new GraphNode(3,0);
                ret[0,3].SetGoesTo(new bool[]{true,true,true,true});

                ret[1,0]=new GraphNode(0,1);
                ret[1,0].SetGoesTo(new bool[]{true,true,false,true});
                ret[1,1]=new GraphNode(1,1);
                ret[1,1].SetGoesTo(new bool[]{true,true,false,true});
                ret[1,2]=new GraphNode(2,1);
                ret[1,2].SetGoesTo(new bool[]{false,true,true,true});
                ret[1,3]=new GraphNode(0,1);
                ret[1,3].SetGoesTo(new bool[]{true,true,true,true});

                ret[2,0]=new GraphNode(0,2);
                ret[2,0].SetGoesTo(new bool[]{true,true,true,true});
                ret[2,1]=new GraphNode(1,2);
                ret[2,1].SetGoesTo(new bool[]{false,true,true,true});
                ret[2,2]=new GraphNode(2,2);
                ret[2,2].SetGoesTo(new bool[]{true,false,false,true});
                ret[2,3]=new GraphNode(3,2);
                ret[2,3].SetGoesTo(new bool[]{true,true,true,false});
                

                break;
            default:
                ret= null;
            break;
        }

        return ret;

    }

    //width and height of the map, width and height of the graph
    public static int[] GetDistrictParams(int index){
        int[] ret;

        switch(index){
            case 0:
                ret = new int[]{12,10,1,1};
            break;
            case 1:
                ret = new int[]{30, 20, 4,3};
                break;
            default:
                ret= null;
            break;
        }

        return ret;
    }

    
}
