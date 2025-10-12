# HRI-CAS: Mixed Reality Cooperative Manipulation System

**Field**: Mixed Reality (MR) × Robotics × Human–Robot Interaction  
**Objective**: To develop an MR-based teleoperation system that assists robot control and base relocation by inferring the user’s manipulation intent.  
**Technical Stack**: Unity (MRTK3), Meta Quest 3, ROS (Noetic), Python  

## Author
 **Soma Okamoto**  
Meijo University, Sekiyama Laboratory  
- Research: Mixed Reality Human–Robot Interaction  
- Skills: Unity (C#), ROS, Python, Docker, Git, Meta Quest SDK  
<!-- - [LinkedIn](#) | [Portfolio Site](#) -->


## BaseMove System
A module for **base position suggestion and relocation** using Inverse Reachability Map (IRM).  
It visualizes feasible robot base positions in MR space, allowing users to intuitively guide the robot’s movement.  

![BaseMove](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像4.png?raw=true)

## Aim Assist with ManipulationSystem and UserTarget DetectionSystem
Implements **aim-assist control** that aligns the robot end-effector with the user’s intended target in real time.  
The **UserTarget Detection System** infers the grasp target based on hand posture and gaze direction.

![AimAssist Teleoperation](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像6.png?raw=true)


## SpacialVisualizationSystem
A real-time environment visualization module combining **YOLOv5**, **DeepSORT**, and **Point Cloud rendering**.

This system detects and tracks dynamic objects in the scene using YOLOv5 + DeepSORT,  
and simultaneously reconstructs the surrounding geometry as a **3D point cloud** within the MR environment.  
By integrating object tracking with spatial mapping, the user can visualize both **terrain structure** and **object motion** intuitively through MR.


![SpatialVisualization](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像7.png?raw=true)




