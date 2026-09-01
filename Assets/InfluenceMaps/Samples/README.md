# Incluence Maps - Sample Scenes & Setup Guide

This folder contains 4 demonstration scenes that showcase the core functionality of the Influence Maps system. Below you will find a breakdown of each scene and a step-by-step guide on how to build your own influence-driven scene from scratch.

## 1. Sample Scenes Breakdown

### Scene UnitWall

- **Description:** Demonstrates how a single AI unit interacts with an influence map within a static environment containing an obstacle.
- **Key Concept:** Grid positioning, obstacle detection, and influence occlusion/masking.
- **Hierarchy Setup:** Main Camera, Ground, Obstacle, Influence Map Manager, Influence Map, Visualizer, and 1 AI Unit.

### Scene Units

- **Description:** Showcases a single influence map handling opposing forces simultaneously. It contains 3 units on the same map: 2 units generate a value of `-1`, while the 3rd unit generates a value of `+1`.
- **Key Concept:** Additive and subtractive influence blending on a single layer to represent opposing factions/threats.
- **Hierarchy Setup:** Main Camera, Ground, Influence Map Manager, 1 Influence Map, Visualizer, and 3 Units (no obstacles).

### Scene Maps

- **Description:** Features 3 independent influence sources, each assigned and writing to a completely different influence map.
- **Key Concept:** Multi-layer influence separation. This is crucial for tracking different data types independently (e.g., separating "Allies", "Enemies", and "Resources").
- **Hierarchy Setup:** Main Camera, Ground, Influence Map Manager, 3 separate Influence Maps, Visualizer, and 3 Independent Sources.

### Scene AI

- **Description:** A dynamic gameplay loop featuring 2 active AI units. One unit is configured as a `Chaser` (pursuer) and the other as an `Evader` (fleeing). The Evader dynamically calculates escape paths based on threat, momentum, and map edges.
- **Key Concept:** Real-time path evaluation, tactical decision-making, momentum weights, and border safety margins using the `AI_Unit` system.
- **Hierarchy Setup:** Main Camera, Ground, 1 Central Obstacle, Influence Map Manager, 2 Influence Maps (Threat Map & Target Map), Visualizer, and 2 AI Units.

---

## 2. Step-by-Step Setup Guide

Follow these steps to configure the Influence Maps system in a brand-new scene:

### Step 1: Environment Setup

1. Create a 3D object, such as a **3D Plane**, to act as your walkable floor (Ground).
2. For moving AI agents, ensure your ground and obstacles are marked as **Navigation Static**, then bake the navigation mesh (**NavMesh**) via _Window > AI > Navigation_.

### Step 2: Manager, Map, and Configuration

1. Create an empty GameObject in the hierarchy and name it `InfluenceMapManager`. Attach the `InfluenceMapManager` script to it.
2. Create your global configuration, propagation, and decay assets by right-clicking in the Project window: **Create > InfluenceMaps > [Select Asset Type]**.
3. Assign the newly created **Global Configuration** asset to the `InfluenceMapManager` component. Within that configuration asset, assign your specific **Propagation** and **Decay** curves.
4. Create another empty GameObject and name it `ThreatMap` (or any layer name you prefer). Attach the `InfluenceMap` script to it.
5. **Positioning & Grid Alignment:** In the `InfluenceMap` component, drag your ground object into the positioning slot. Then, in the Global Configuration settings, set the **Origin Mode** to **Anchor Object**.
6. Customize the grid bounds, cell resolution, and update frequency settings to fit your project's performance and gameplay needs.

### Step 3: Influence Sources & Obstacles

- **Influence Sources:** Attach the `InfluenceSource` to any GameObject (e.g., a tower, item, or enemy). Set its base influence value (positive for allies/rewards, negative for enemies/hazards) and define its **Maximum Influence Range**.
- **Influence Obstacles:** Attach the `InfluenceObstacle` to any blocking geometry. Within the script inspector, specify which map layers this obstacle should affect. Then, fine-tune the **Blocking Strength** and the **Dead Zone** (an area behind the obstacle where influence is forced to 0).

Multiple corresponding scripts with different parameters can be attached to a single source or obstacle depending on the specific map layers they are assigned to.

### Step 4: Configuring the Visualizer

1. Create an empty GameObject named `InfluenceVisualizer` and attach your visualization script.
2. In the **Layers** list inside the inspector, add the specific map layers you want to render.
   _Note:_ Cell value displays and grid lines rendering can be toggled on/off globally inside your **Global Configuration** asset.

---

## 3. Advanced Customization & Architecture

The system is highly modular and allows for deep customization depending on your project constraints:

1. **Local Overrides:** Each individual `InfluenceMap` can override the **Global Configuration** settings for specific sections to allow unique behavior per layer.
2. **Per-Map Pipelines:** Every map layer can utilize different propagation/decay functions and be configured with its own independent update interval.
3. **Custom Interfaces:** \* To create custom behavior for a source, implement the `IInfluenceSource` interface in your script.
   - To create custom obstacle behavior, implement the `IInfluenceObstacle` interface.
4. **Custom Update Pipelines:** You can alter the execution loop algorithm by changing the pipeline setting from **Wave** to **Custom**, and then passing a script that implements the `IInfluenceMapPipeline` interface.
5. **High-Performance Job System:** For massive performance gains, the architecture supports **Unity Jobs + Burst Compiler**. To utilize this, implement the `IJobInfluenceMapPipeline` interface to run calculations off the main thread.
