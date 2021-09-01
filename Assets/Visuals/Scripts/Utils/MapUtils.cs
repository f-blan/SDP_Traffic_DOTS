using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class MapUtils 
{
    public static void InitializeMap(Map<MapTile> CityMap, PathFindGraph CityGraph, int index, int n_districts_x, int n_districts_y, out List<GraphNode> busStopNodes,
            out List<Tuple<bool,MapTile>> trafficLightTiles, out List<MapTile> parkSpotTiles)
    {
        int[,] i_to_n;
        int[,] bs_to_n;
        int[] refBusTile;
        int[,] tlTiles;
        
        
        MapTile.TileType[,] districtImage = MapUtils.GetDistrictImage(index, out i_to_n, out bs_to_n, out refBusTile, out tlTiles);
        GraphNode[,] graphDistrictImage = MapUtils.GetGraphDistrictImage(index);
        
        busStopNodes = new List<GraphNode>();
        
        trafficLightTiles = new List<Tuple<bool,MapTile>>();
        parkSpotTiles = new List<MapTile>();

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
                        MapTile tile = CityMap.GetMapObject(m_x, m_y);

                        
                        

                        //cut at the borders of the map
                        if(m_x==0 || m_y==0 || m_x== CityMap.GetWidth()-1 || m_y== CityMap.GetHeight()-1){
                            tile.SetTileType(MapTile.TileType.Obstacle);    
                        }else{
                            tile.SetTileType(districtImage[d_y,d_x]);
                        }

                        if(tile.GetTileType() == MapTile.TileType.ParkSpot){
                            parkSpotTiles.Add(tile);
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
                        int[] goesTo = new int[4];
                        int[] temp1 = new int[]{g_y, g_x, g_y, g_x};
                        int [] temp2 = new int[]{CityGraph.GetHeight()-1, CityGraph.GetWidth()-1, 0, 0};
                       
                       //cut at the borders of the
                       for(int i =0 ; i< 4; ++i){
                           if(temp1[i] == temp2[i]){
                               goesTo[i] = -1;
                           }else{
                               goesTo[i] = graphDistrictImage[d_y, d_x].GetGoesTo()[i];
                           }
                       }


                        CityGraph.GetGraphNode(g_x, g_y).SetGoesTo( goesTo);
                        
                        g_x++;
                    }
                    
                }
                
                g_y++;
            }
        }
        
        //link intersections to graphnodes (so that given your position you can know what node you're in)
        CityGraph.SetBusStopRelativeCoords(bs_to_n[0,0], bs_to_n[0,1]);
        CityGraph.SetBusStopRelativePosition(refBusTile[0], refBusTile[1]);
        for(int y=0; y<n_districts_y; ++y){
            for(int x =0 ; x<n_districts_x; ++x){

                for(int t=0; t< i_to_n.GetLength(0); ++t){
                    GraphNode g = CityGraph.GetGraphNode(x,y,i_to_n[t, 2], i_to_n[t, 3]);
                    CityMap.GetMapObject(x,y, i_to_n[t, 0], i_to_n[t, 1]).SetGraphNode(g);
                    CityMap.GetMapObject(x,y, i_to_n[t, 0]+1, i_to_n[t, 1]).SetGraphNode(g);
                    CityMap.GetMapObject(x,y, i_to_n[t, 0], i_to_n[t, 1]+1).SetGraphNode(g);
                    CityMap.GetMapObject(x,y, i_to_n[t, 0]+1, i_to_n[t, 1]+1).SetGraphNode(g);
                    //Debug.Log("linked "+i_to_n[t,0]+ "-"+i_to_n[t, 1] + "to node " + g.GetX() + "-"+g.GetY());
                }

                //same for bus stops
                for(int t=0; t<bs_to_n.GetLength(0); ++t){
                   
                    //also graphNodes have to be aware that they are "bus stop nodes"
                    CityGraph.GetGraphNode(x,y, bs_to_n[t,0], bs_to_n[t,1]).SetIsBusStop(true);
                    busStopNodes.Add(CityGraph.GetGraphNode(x,y,bs_to_n[t,0], bs_to_n[t,1]));
                }
                //initialize trafficLightTiles
                for(int t=0; t<tlTiles.GetLength(0); ++t){
                    int r_x = tlTiles[t,0];
                    int r_y = tlTiles[t,1];
                    trafficLightTiles.Add(new Tuple<bool, MapTile>(true, CityMap.GetMapObject(x,y,r_x,r_y)));
                    trafficLightTiles.Add(new Tuple<bool, MapTile>(true, CityMap.GetMapObject(x,y, r_x-1, r_y + 3)));
                    trafficLightTiles.Add(new Tuple<bool, MapTile>(false, CityMap.GetMapObject(x,y, r_x+1, r_y + 2)));
                    trafficLightTiles.Add(new Tuple<bool, MapTile>(false, CityMap.GetMapObject(x,y, r_x-2, r_y + 1))); 
                }

            }
        }

    }

    public static MapTile.TileType[,] GetDistrictImage(int index, out int[,] intersectionTiles, out int[,] busStopTiles, out int[] busStopReferencePosition,
            out int[,] tlTiles){
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
                busStopReferencePosition = new int[2];

                tlTiles = new int[,]{
                    {6,3}
                };

                
                
                break;
            case 1:
                image = new int[,]{
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 2, 0, 3, 1, 0, 2, 2, 2, 2, 0, 0, 0, 0, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 4, 4, 1, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 4, 4, 1, 1 },
                    { 2, 0, 1, 3, 0, 2, 2, 2, 2, 0, 1, 1, 0, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 0, 2, 2, 2, 2, 0, 1, 1, 0, 2, 2, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 0, 2, 2, 0, 1, 1, 0, 0, 2, 2, 2, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 2, 2, 2, 0, 0, 0, 0, 2, 2, 2, 0, 3, 1, 0, 5, 6, 0, 0, 0, 0, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 5, 0, 1, 3, 0, 0, 2, 2, 2, 2, 2, 0, 1, 3, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 6, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                };
                
                tlTiles = new int[,]{
                    {17,2},
                    {27, 2},
                    {27, 9},
                    {3, 15}
                };

                intersectionTiles= new int[,]{
                    {2, 3, 0, 0},
                    {10, 3, 1, 0},
                    {16, 3, 2, 0},
                    {26, 3, 3, 0},

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
                    {2, 0},
                    
                };
                busStopReferencePosition = new int[]{17,3};
                
                break;
            case 2:
                image = new int[,]{
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 2, 0, 3, 1, 0, 0, 2, 2, 2, 0, 0, 0, 0, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 4, 4, 1, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 2, 4, 4, 1, 1 },
                    { 2, 0, 1, 3, 0, 0, 2, 2, 2, 0, 1, 1, 0, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 6, 1, 1, 2, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 5, 1, 1, 2, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 2, 2, 0, 3, 1, 0, 5, 6, 0, 0, 2, 2, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 4, 4, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 4, 4, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 2, 1, 1, 2, 6, 5, 0, 1, 3, 0, 0, 2, 2, 2, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 1, 1, 5, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 2, 1, 1, 6, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 0 },
                    { 0, 0, 3, 1, 0, 2, 2, 2, 0, 0, 1, 1, 0, 0, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 1, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 1, 1 },
                    { 0, 0, 1, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                };

                tlTiles = new int[,]{
                    {3,2},
                    //{17,2},
                    //{27, 2},
                    {3,15},
                    {27, 9},
                    {17, 9}
                };

                intersectionTiles= new int[,]{
                    {2, 3, 0, 0},
                    {10, 3, 1, 0},
                    {16, 3, 2, 0},
                    {26, 3, 3, 0},

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
                    {2, 1},
                    
                };
                busStopReferencePosition = new int[]{17,10};
            break;
            case 3:
                image = new int[,]{
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 2, 0, 1, 1, 0, 0, 2, 2, 2, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 2, 2, 2, 2, 2, 0, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 2, 0, 0, 0, 0, 0, 2, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 2, 2, 2, 0, 3, 1, 0, 5, 6, 0, 0, 0, 0, 2, 2, 2, 0, 2, 2, 0, 1, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1 },
                    { 0, 0, 1, 1, 0, 0, 0, 6, 5, 0, 1, 3, 0, 2, 2, 0, 1, 1, 0, 0, 2, 2, 0, 2, 0, 0, 0, 0, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 5, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 6, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 3, 1, 0, 2, 2, 2, 0, 0, 1, 1, 0, 2, 2, 0, 3, 1, 0, 0, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1 },
                    { 0, 0, 1, 3, 0, 0, 2, 2, 0, 0, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 2, 2, 2, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                };

                tlTiles = new int[,]{
                    {3,2},
                    {11,9},
                    {27, 15},
                    
                    {17, 2}
                };

                intersectionTiles= new int[,]{
                    {2, 3, 0, 0},
                    {10, 3, 1, 0},
                    {16, 3, 2, 0},
                    {26, 3, 3, 0},

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
                    {1, 1},
                    
                };
                busStopReferencePosition = new int[]{11,10};
            break;
            case 4:
                image = new int[,]{
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 2, 0, 1, 1, 0, 0, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 2, 2, 2, 2, 2, 0, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 2, 0, 0, 0, 0, 0, 2, 2, 2, 0, 1, 1, 0, 2, 2, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 2, 2, 2, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 0, 2, 2, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 1, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 0, 0, 0, 0, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 2, 2, 0, 2, 0, 0, 1, 3, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 0, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 6, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 2, 1, 1, 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 0, 5, 1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 3, 1, 0, 2, 2, 2, 0, 0, 1, 1, 0, 2, 2, 0, 3, 1, 0, 5, 6, 0, 2, 2, 2, 0, 3, 1, 0, 0 },
                    { 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 1, 4, 4, 3, 1, 1, 1, 1, 1, 1, 1, 4, 4, 3, 1 },
                    { 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 4, 4, 1, 1, 1, 3, 4, 4, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 1, 1 },
                    { 0, 0, 1, 3, 0, 0, 2, 2, 0, 0, 0, 0, 0, 6, 5, 0, 1, 3, 0, 0, 2, 2, 2, 0, 0, 0, 1, 3, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                    { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 6, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0 },
                };

                tlTiles = new int[,]{
                    {3,2},
                    
                    {27, 15},
                    {27, 2},
                    {27, 9},
                    {17, 2}
                };

                intersectionTiles= new int[,]{
                    {2, 3, 0, 0},
                    {10, 3, 1, 0},
                    {16, 3, 2, 0},
                    {26, 3, 3, 0},

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
                    {2, 0},
                    
                };
                busStopReferencePosition = new int[]{17,3};
            break;
            default:
                image = null;
                intersectionTiles = null;
                busStopTiles=null;
                busStopReferencePosition = null;
                tlTiles=null;
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
                    case 6:
                        ret[r_y, x] = MapTile.TileType.Other;
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
                
                ret[0,0].SetGoesTo(new int[]{9,11,9,11});
            break;
            case 1:
                ret = new GraphNode[3,4];

                ret[0,0]=new GraphNode(0,0);
                ret[0,0].SetGoesTo(new int[]{-1,7,6,5});
                ret[0,1]=new GraphNode(1,0);
                ret[0,1].SetGoesTo(new int[]{-1,5,-1,7});
                ret[0,2]=new GraphNode(2,0);
                ret[0,2].SetGoesTo(new int[]{6,9,6,5});
                ret[0,3]=new GraphNode(3,0);
                ret[0,3].SetGoesTo(new int[]{6,5,6,9});

                ret[1,0]=new GraphNode(0,1);
                ret[1,0].SetGoesTo(new int[]{5,7,-1,5});
                ret[1,1]=new GraphNode(1,1);
                ret[1,1].SetGoesTo(new int[]{5,5,-1,7});
                ret[1,2]=new GraphNode(2,1);
                ret[1,2].SetGoesTo(new int[]{-1,9,6,5});
                ret[1,3]=new GraphNode(0,1);
                ret[1,3].SetGoesTo(new int[]{5,5,6,9});

                ret[2,0]=new GraphNode(0,2);
                ret[2,0].SetGoesTo(new int[]{6,7,5,5});
                ret[2,1]=new GraphNode(1,2);
                ret[2,1].SetGoesTo(new int[]{-1,5,5,7});
                ret[2,2]=new GraphNode(2,2);
                ret[2,2].SetGoesTo(new int[]{6,-1,-1,5});
                ret[2,3]=new GraphNode(3,2);
                ret[2,3].SetGoesTo(new int[]{6,5,5,-1});
                

            break;
            case 2:
                ret = new GraphNode[3,4];

                ret[0,0]=new GraphNode(0,0);
                ret[0,0].SetGoesTo(new int[]{6,7,6,5});
                ret[0,1]=new GraphNode(1,0);
                ret[0,1].SetGoesTo(new int[]{6,5,-1,7});
                ret[0,2]=new GraphNode(2,0);
                ret[0,2].SetGoesTo(new int[]{6,9,6,5});
                ret[0,3]=new GraphNode(3,0);
                ret[0,3].SetGoesTo(new int[]{6,5,6,-1});

                ret[1,0]=new GraphNode(0,1);
                ret[1,0].SetGoesTo(new int[]{5,-1,6,5});
                ret[1,1]=new GraphNode(1,1);
                ret[1,1].SetGoesTo(new int[]{5,5,6,-1});
                ret[1,2]=new GraphNode(2,1);
                ret[1,2].SetGoesTo(new int[]{5,9,6,5});
                ret[1,3]=new GraphNode(0,1);
                ret[1,3].SetGoesTo(new int[]{5,5,6,9});

                ret[2,0]=new GraphNode(0,2);
                ret[2,0].SetGoesTo(new int[]{6,-1,5,5});
                ret[2,1]=new GraphNode(1,2);
                ret[2,1].SetGoesTo(new int[]{-1,5,5,-1});
                ret[2,2]=new GraphNode(2,2);
                ret[2,2].SetGoesTo(new int[]{6,-1,5,5});
                ret[2,3]=new GraphNode(3,2);
                ret[2,3].SetGoesTo(new int[]{6,5,5,-1});
                

            break;
            case 3:
                ret = new GraphNode[3,4];

                ret[0,0]=new GraphNode(0,0);
                ret[0,0].SetGoesTo(new int[]{6,7,6,5});
                ret[0,1]=new GraphNode(1,0);
                ret[0,1].SetGoesTo(new int[]{6,5,-1,7});
                ret[0,2]=new GraphNode(2,0);
                ret[0,2].SetGoesTo(new int[]{6,9,6,5});
                ret[0,3]=new GraphNode(3,0);
                ret[0,3].SetGoesTo(new int[]{-1,5,6,9});

                ret[1,0]=new GraphNode(0,1);
                ret[1,0].SetGoesTo(new int[]{-1,7,6,5});
                ret[1,1]=new GraphNode(1,1);
                ret[1,1].SetGoesTo(new int[]{5,5,6,7});
                ret[1,2]=new GraphNode(2,1);
                ret[1,2].SetGoesTo(new int[]{-1,9,6,5});
                ret[1,3]=new GraphNode(0,1);
                ret[1,3].SetGoesTo(new int[]{5,5,-1,9});

                ret[2,0]=new GraphNode(0,2);
                ret[2,0].SetGoesTo(new int[]{6,7,-1,5});
                ret[2,1]=new GraphNode(1,2);
                ret[2,1].SetGoesTo(new int[]{-1,-1,5,7});
                ret[2,2]=new GraphNode(2,2);
                ret[2,2].SetGoesTo(new int[]{6,9,-1,-1});
                ret[2,3]=new GraphNode(3,2);
                ret[2,3].SetGoesTo(new int[]{6,5,5,9});
                

            break;
            case 4:
                ret = new GraphNode[3,4];

                ret[0,0]=new GraphNode(0,0);
                ret[0,0].SetGoesTo(new int[]{6,7,6,5});
                ret[0,1]=new GraphNode(1,0);
                ret[0,1].SetGoesTo(new int[]{6,5,-1,7});
                ret[0,2]=new GraphNode(2,0);
                ret[0,2].SetGoesTo(new int[]{6,9,6,5});
                ret[0,3]=new GraphNode(3,0);
                ret[0,3].SetGoesTo(new int[]{6,5,6,9});

                ret[1,0]=new GraphNode(0,1);
                ret[1,0].SetGoesTo(new int[]{-1,7,6,5});
                ret[1,1]=new GraphNode(1,1);
                ret[1,1].SetGoesTo(new int[]{5,-1,6,7});
                ret[1,2]=new GraphNode(2,1);
                ret[1,2].SetGoesTo(new int[]{-1,9,6,-1});
                ret[1,3]=new GraphNode(0,1);
                ret[1,3].SetGoesTo(new int[]{5,5,6,9});

                ret[2,0]=new GraphNode(0,2);
                ret[2,0].SetGoesTo(new int[]{6,7,-1,5});
                ret[2,1]=new GraphNode(1,2);
                ret[2,1].SetGoesTo(new int[]{-1,5,5,7});
                ret[2,2]=new GraphNode(2,2);
                ret[2,2].SetGoesTo(new int[]{6,9,-1,5});
                ret[2,3]=new GraphNode(3,2);
                ret[2,3].SetGoesTo(new int[]{6,5,5,9});
                

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
                ret = new int[]{12,10,1,1,10,20};
            break;
            case 1:
                ret = new int[]{30, 20, 4,3,128};
                break;
            case 2:
                ret = new int[]{30, 20, 4,3,120};
                break;
            case 3:
                ret = new int[]{30, 20, 4,3,128};
                break;
            case 4:
                ret = new int[]{30, 20, 4,3,128};
                break;
            default:
                ret= null;
            break;
        }

        return ret;
    }

    
}
