# Traffic Simulation Report

## 1. Project description
<br>
This section serves as a general description of the project and its features, for more details about the implementation visit section number 2
<br><br>

### 1.1 Authors
<br>Both of the authors are students from Politecnico di Torino and are enrolled in the System and Device Programming course (a.y. 2020/2021)

|Surname | Name | Student number |
|--------|------|----------------|
|Abreu | Samuel Oreste | s281568|
|Blangiardi| Francesco  |s288265|
<br>

### 1.2 Objectives

<br>The goal of the project is to explore the potential of the Unity DOTS technology and get familiar with the ECS programming pattern by developing a simulation that takes full advantage of their related optimizations as well as using the Unity Jobs System to create highly optimized multithreaded C# code. More information about the DOTS technology can be found [in this page](https://unity.com/dots).
<br>For this purpose, the authors have created a simulated environment of a city using Unity, as suggested by the project tutors.
<br>The simulation is meant to be run from the Unity editor and after having imported the packages listed [in this page](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/install_setup.html) under the "Recommended Packages" section.

### 1.3 Features

<br>The city features streets and intersections, where different type of vehicles (implemented as entities) are allowed to move.
<br>Vehicles are divided into cars and buses, with each category featuring a different behavioral pattern, while their motion is regulated and limited by:
- Streets
- Intersections
- Traffic lights
- Parking spots/Bus stops
- Other nearby cars

<br>The entities are meant to move in the city by mimicking as much as possible vehicles moving in real cities, and while the focus of the project was not to give a detailed graphical representation of an urban environment (through animations etc.), the team has developed a simulation able to run a fairly high amount of vehicles with reasonable performances.

## 2. Implementation Details

<br>Here are listed the most important implementation details of the simulation, as well as an overview of file organization
<br><br>
### 2.1 File Organization
<br>All the code developed by the team is contained in the Assets folder. The most important folders are:
- ECS. This folder contains two subfolders ("DataComponents" and "Systems") that contain all the code related to ECS. Also a "Utils" folder is present where some more C# files are stored that contain functions used by the application's systems
- Scenes. This is where the only scene developed by the team is stored. The scene is composed of three GameObjects that handle the generation and visualization of the map (Map_Setup and Map_Visual) while setting up the most important systems, as well as a Main Camera and a UI object that displays on screen some information about the simulation at run time.
- Visuals. The scripts used by the scene's GameObjects are all inside its "Scripts" subfolder, which additionally contains also the classes used to represent the map internally. This folder also contains the "Textures" folders where all textures and materials used in the simulation are stored.
<br>All parameters of the application (see section 2.2) can be set from the "config.xml" file in the "Assets/Configuration" folder.

### 2.2 The parameters
<br> The parameters of the simulation can be set both by the config.xml file (Assets/Configuration) or by the Unity editor (enable "Override Reading Config File" in Map_Setup GameObject). This section presents the table of parameters along with their description.

|Parameter name|Related GameObject|Description|
|--------------|------------------|-----------|
|map_n_district_x|Map_Setup|defines the map width (in terms of districts)|
|map_n_district_y|Map_Setup|defines the map height (in terms of districts)|
|n_entities|Map_Setup|the number of cars that will run in the simulation. Warning: a map of a given size can spawn up to a maximum amount of cars; if n_entities exceeds such number, the maximum number of cars will be spawned instead.
|n_bus_lines|Map_Setup|the number of bus lines that will be spawned in the simulation. Warning: spawning bus lines requires a map with at least 2 districts both in the x and y directions; the map doesn't allow to spawn more than one bus line per district; bus lines paths are computed in the first fram for all bus lines, setting this value too high with big maps may delay the start of the simulation.
|frequency_district_X|Map_Setup|This is a group of parameters (X goes from 0 to 3). They define the frequency the corresponding district type will be chosen during map creation (choosing a higher frequency with respect to other will generate a map comprised of mostly the respective district type). To disable a given district type set its frequency parameter to 0.|
|maxCarSpeed|Map_Spawner|sets the maximum speed of cars|
|maxBusSpeed|Map_Spawner|sets the maximum speed of buses|
|differentTypeOfVehicles|Map_Visual|the number of different colors spawned cars may have|

### 2.3 The map
<br>The map is generated entirely through script and is divided in several "districts" (the number of districts the map is composed of can be set through parameters). Each district is chosen randomly (according to the "frequency" parameters) from a set of 4 different district types:
<br>all of them are defined by a unique structure and contain several intersections (12, of which one features bus stops) but all have (for simplicity's sake) the same dimension and are compatible with each other (e.g stacking any number of district in any combination of district types and in any direction gives shape to a street network where any road can be accessed from any position). Additionally every district is divided into tiles, where a tile is equivalent to the space occupied by a single car, which can be of several different types (road, building, traffic light, etc.).
<br>The map is stored internally in two different classes, both of which are contained in the Visuals/Scripts subfolder; they follow a singleton architectural pattern, and thus, are instantiated only once in Map_Setup:
- Map (instantiated as CityMap). It contains information related to each tile as well as some general information on the map (number of districts, dimension of each district etc.). It is used (in a read-only representation) in CarSpawnerSystem(see section 2.5).
- PathFindGraph (instantiated as CityGraph). As the name suggests this class is used for computing the path each vehicle has to take. As such, it is a much more compact data structure compared to CityMap since it only needs to store information on intersections (their coordinates, where they allow to go and with which cost, defined as number of tiles) together with a matrix (usually small) that stores the type of each district present in the map. Intersections can only allow to go to up to 4 directions (up, right, down and left), and is treated by the algorithms as a node of the graph, with each node being identified by an x and y coordinate (the graph is organized as a matrix).

### 2.4 The Entities

<br>As any Unity project based on DOTS and ECS, our simulation features several different types of entities, each one being defined by their Components (section 2.5) and being processed by a set of Systems (section 2.6). All the entity types developed contain the Translation component and possibly some other default components (e.g. Rotation, RenderMesh, NonUniformScale etc.)

#### 2.4.1 Districts
<br> These type of entities are in charge of two tasks: spawning car entities and render the texture of the corresponding district type. The former is carried out by the CarSpawnerSystem and is done only once per district, while the latter is done in Map_Visual (first frame) together with the spawning of the district entity itself.<br>

#### 2.4.2 ParkSpots

<br> ParkSpot entities are only identified by the ParkSpotTag (a Tag is a Component without data associated to it), they are not tasked with any rendering (parking spot visuals is dealt with by the district entities, whose textures feature several light blue tiles) and are only used in the QuadrantSystem to notify cars looking for a parking spot of their presence. They are spawned by Map_Setup using a dedicated function in Map_Spawner<br>

#### 2.4.3 BusStops
<br> These type of entities are very similar to ParkSpot entities, the only difference being that there is a fixed amount of them (4 per district) and that they are reserved for buses when they reach the corresponding Bus Stop Intersection. Likewise the entities themselves do not render any visual component but their position; it is represented in the district texture by the orange areas.<br>

#### 2.4.4 TrafficLights
<br> TrafficLight entities are identified by the TrafficLightComponent and are processed by the TrafficLightSystem and the QuadrantSystem. They are divided in two types (vertical and horizontal) and are rendered by their own RenderMesh component, which renders a different material depending on the traffic light type. Just like ParkSpots and BusStops they are spawned in the first frame by Map_Setup.<br>

#### 2.4.5 Cars
<br> Cars are the most important entity of the simulation and can contain different types of components at different points in time. As mentioned in 2.4.1 they are spawned by the CarSpawnerSystem, which initializes most of their components. Their movement is regulated by the QuadrantSystem, which makes them avoid colliding with other cars along with some other functionalities.
<br> Their behavioral cycle is the following:

- PathFinding: the cars have a CarPathParams component and are therefore processed by the CarPathSystem. In this phase a random intersection in the whole map is selected and a path is computed within one frame, at the end of which the CarPathParams component is removed and the path is stored into a DynamicBuffer<CarPathBuffer> attached to the entity.
- PathFollowing: all cars that contain the VehicleMovementData (initialized in CarSpawnerSystem) are processed by the VehicleMovementSystem, which in this phase moves the cars according to the information contained in the dynamic buffer.
- Parking: after reaching the last node in the path the cars start moving in random directions (still through the VehicleMovementSystem) looking for a free parking spot (mimicking human behavior) until the QuadrantSystem notifies them that there's one available in their immediate right. After that the car parks into the parking spots and waits a fixed amount of time, after which the VehicleMovementSystem will attach the CarPathParams component to it in order to start a new cycle.


#### 2.4.6 BusLines
<br> These entities are identified by the BusPathParams component and are processed only once and only in the first frame by the BusPathSystem. Each bus line has a starting district (and therefore a starting Bus Stop Intersection), while the computed path goes through the starting district and two more random districts in the map. After path computation the path is stored into a blobArray (a read-only data structure that can be shared between entities) and the BusLine entities are destroyed.<br>

#### 2.4.7 Buses
<br> Buses are very similar to cars (they also have a VehicleMovementData component) but their motion is handled by the BusMovementSystem. They are spawned after the path computation in BusPathSystem, while their behavioral cycle consists only in following the path of their respective bus line (in two possible verses) and in stopping at bus stops for a fixed amount of time.<br>

### 2.5 Components

<br> In this subsection some general information about the custom component used in the simulation are listed
<br><br>

| name | related entities | related systems | description |
|------|------------------|-----------------|-------------|
| BusPathComponent | Buses | BusMovementSystem | contains a reference to the related bus line's path, as well as some other information |
| BusPathParams | BusLines | BusPathSystem | contains some setup information to compute the path that will be used by bus entities |
| BusStopTag | BusStops | QuadrantSystem | a tag that identifies bus stop entities|
| CarPathParams | Cars | CarPathSystem | setup information to compute the path for cars |
| CarSpawnerComponent | Districts | CarSpawnerSystem | data related to the district and the number of cars to spawn |
| ParkSpotTag | ParkSpots | QuadrantSystem | a tag that identifies bus stop entities |
| TrafficLightComponent | TrafficLights | TrafficLightSystem, QuadrantSystem | stores the state of the traffic light (can be traversed or not)|
| VehicleMovementData | Buses, Cars | BusMovementSystem, CarMovementSystem, QuadrantSystem | contains information on the movement of the bus/car and on its state (moving, turning, parking etc.)|

### 2.6 Systems

<br> Here are described the most important details of the main custom systems used by the application. All the most critical operations are designed to be executed in parallel on multiple cores through the usage of worker threads.

#### 2.6.1 TrafficLightSystem
<br> This simple system processes TrafficLight entities: at fixed time intervals the system changes the state and the color of all traffic light entities depending on their type (vertical or horizontal). Changing the color is achieved by modifying the material all the entities of the given type are using for rendering.

#### 2.6.2 CarSpawnerSystem
<br> This system processes District entities containing the CarSpawnerComponent, and generates a number of car entities contained in the component inside of the related district. It also initializes the CarPathComponent needed by cars to compute the path they're going to follow. This means that all spawned cars will be processed by the CarPathSystem in the next frame: this is an expensive operation and requires allocating an amount of memory that scales with the size of the graph for each car, so each CarSpawnerComponent also contains a "delay" field that allows the System to deal with each districts at different points in time (thus preventing the simulation from crashing when run with a high number of entities in a big map) 

#### 2.6.3 CarPathSystem and BusPathSystem
<br> Both of these systems have the purpose of computing a path. They work on a representation of CityGraph that can be used in read-only mode by parallel Jobs (a native array of structs). The computation is done using the [A\* algorithm](https://en.wikipedia.org/wiki/A*_search_algorithm) which guarantees optimality with reasonable performances; the downside is that each Job needs to work on its own copy of the graph, which was limited to only the relevant nodes in the graph in order to allocate (and therefore free) as least memory as possible for each computation. The differences between the two systems is that BusPathSystem follows a more complex procedure due to the fact that its path has to be circular: it receives through BusPathParams the coordinates of 3 districts and computes a path for each combination of two of those districts on a graph representing only the districts of the map; then for each edge of this path the system computes the path on the actual graph going from the bus stop Intersection of the starting district to the one of the destination district (with some additional constraints to adjust the overall bus path); finally, after joining all these smaller paths, it stores the complete circular path into a blobArray for the bus entities to read. Additionally BusPathSystem takes care of spawning the bus entities.<br>

#### 2.6.4 BusMovementSystem and CarMovementSystem
<br> These systems handle the path following of their respective entities together with their other possible states: CarMovementSystem deals with the parking phase of cars, while BusMovementSystem regulates the stopping procedure buses have to do whenever they reach a bus stop intersection. Both of these system work together with the QuadrantSystem to prevent vehicles from colliding, from taking illegal turns at intersections and to allow them to detect traffic lights and free ParkSpots/BusStops.<br>

#### 2.6.5 QuadrantSystem
<br> As stated in section 2.6.4, this System basically works as a "Nearby entity detection" System: it computes whether a given vehicle can move according to its pathing decisions or if it has to stop for any reason (e.g. there are other vehicles in front of it, it can't cross an intersection because of a red semaphore or if it has to give precedence to other cars) and it detects nearby free ParkSpots/BusStops. The system works as follows:
- A few NativeMultiHashMap are allocated as private variables, each one representing one or more type of entities and all of them using as key an integer computed by hashing a Translation value. 
- Each key represents a "Quadrant" (a small area of the map, set to be a 5x5 tile square), the idea being that every tile of the CityMap is identified by only one Quadrant (and therefore a key in the hashMap). 
- The hashMaps representing immovable, unchanging entities (ParkSpots and BusStops) are filled with the respective entities only once by hashing their Translation component, while the rest (TrafficLights and vehicles) are filled once every frame since they may change the quadrant they are in due to movement (vehicles) or they need to refresh their related information inside the hashmap (traffic lights).
- Vehicles can detect if entities of a given type are present in a position they are interested in (e.g. right in front of them or to their side) by cycling through all the entities located within the Quadrant of the aforementioned position. They can do so by hashing the position into an integer and using the HashMap of the given type(s).
- Depending on the state of the vehicle, one or more positions will be probed: when the vehicle is inside a road the system will probe only the position right in front of the vehicle (the position on the right side may also be probed if the vehicle has to park/stop); if instead the vehicle is at an intersection the system will probe several different positions depending on where the vehicle has to turn (generally the vehicles are meant to stop if the Italian Road System requires them to)

Since these operations require populating hashMaps and cycling through entities, and since (differently from pathFinding) this is done once every frame and for every vehicle, the QuadrantSystem is the most critical part of the simulation when it comes to performances. Its implementation has been made considering a tradeoff between performances, traffic jam avoidance and correctness.

## 3. Results

<br>In this section several runs of the simulation with varying parameters along with some additional information are reported

### 3.1 Simulations
<br>DISCLAIMERS: 
<br>Average fps is the value measured after the spawning phase has ended. Lowest fps is usually reached during the final stages of car spawning.
<br>The following simulations were run by setting all the frequency parameters for districts to 1
<br>The following simulations were run on a System with the following specifics:
- Processor: Intel(R) Core(TM) i7-7700HQ CPU @ 2.80GHz   2.81 GHz
- RAM installed = 16,0 GB
- Graphics Card = NVIDIA GeForce GTX 1060

|Id|Number of running entities|Number of bus lines|Total number of entities|Map Size (in districts)|Graph size (in nodes)|Peak fps|Lowest fps|Average fps| 
|--|--------------------------|-----------|------------------------|-----------------------|---------------------|--------|----------|-----------|
| 0 | 15 | 0 |94 | 1 | 12 | 450 | 390 | 400 |
| 1 | 275| 0 |615| 4 | 48 | 415 | 370 | 390 |
| 2 | 18 | 9 | 763 | 762 | 9 | 108 | 450 | 390 | 400|
| 3 | 35200| 100 | 138308| 1225 | 14700 | 130 | 55 | 120 |
| 4 | 100600| 300 | 541254 | 4900 | 58800 | 55 | 25 | 45 |
| 5 | 150000| 0 | 878000 | 8100 | 97200 | 40 | 15 | 30 |
| 6 | 250000| 0 | 930000 | 8100 | 97200 | 30 | 7 | 22 |

### 3.2 Comments and Observations
- 0. A simple simulation to test the behavior of cars. The number of cars is very low as well as the map size, so performance are very good. No bus lines were spawned since the map is too small. Note: path randomicity for each car is actually pseudo randomic based on car position, time delay between frames etc. so the cars that spawn close to each other may have similar paths during their first cycles. Pseudo randomicity could not be avoided since the class Unity.Random was used in order to generate random numbers inside Jobs.
- 1. A simulation of an overcrowded small city. Performances are still not an issue, however the city is overcrowded and since (for the sake of performances) cars don't make dynamic pathing decisions based on traffic it may happen that traffic jams are formed (e.g. an intersection is too crowded and doesn't allow any car to move or loops of cars spanning more than one intersection are formed). To avoid these situations as much as possible cars are allowed to overlap briefly in some occasions, but in order to guarantee fair traffic rules the team decided to reach a compromise between collision avoidance, probability of traffic jams and performances. As a general it is suggested not to spawn more than 25 cars per district.
- 2. Bus-only simulation in a small city. Performances are very good since there are not many running entities, the purpose of this simulation is to show bus behavior and the bus drawing feature.
- 3. A simulation of a medium sized city with both cars and bus lines.
- 4. A very big city with > 100k running entities. Since there were 300 bus lines with such a big map the simulation takes a bit more time than usual to start, however once the spawning of the entities is finished the simulation holds at 45 fps.
- 5. A test to see how many cars can be handled by the application considering a map big enough to not become overcrowded. Buses were not spawned because, considering the amount that can be spawned with such a big map without taking too long at the first frame, they don't weigh on performance nearly the same way as cars after being spawned. The spawning + pathfinding of cars takes the framerate as low as 15 fps in the last stages of the simulation, however once this phase is finished the framerate stabilizes at around 30 fps.
- 6. Another stress test, similar to n.5 but with more cars. The spawning phase weighs a lot on performance and the city is a bit overcrowded, but the final and stable framerate stays at around 22 fps.

## 4 Additional Features and Usage
<br>The simulation is meant to be run on the Unity editor after importing all the related packages. 
<br>All parameters can be set on an external file named "config.xml" in the "Assets/Configuration" folder and are described in section 2.2. 
<br>Also the simulation contains the following additional features that can be accessed from the game scene:
- Camera functionalities. The main camera of the simulation allows to zoom in and out (by using the mouse wheel) and to move its position by dragging the mouse. Also it features a vehicle follow mode: by left clicking on a vehicle the camera will position so that the selected car is at its center and will keep following its movements (right click anywhere to exit vehicle follow mode),
- UI. There are some text fields in the top left and bottom left corners of the main camera. They display the time elapsed since the start of the application and the number of running entities currently running in the simulation (the number may differ from the real one by a few units) 
- Path drawing. Left clicking on a bus entity will (other than entering vehicle mode) draw a line in the screen that joins all the intersection traversed by the related bus line (right click to clear the screen)
 
