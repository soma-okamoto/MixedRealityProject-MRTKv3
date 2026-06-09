# MASK: Mixed reality Adaptive Spatial and Kinematic support

**Field**: Mixed Reality (MR) × Robotics × Human–Robot Interaction  
**Objective**: To develop an MR-based teleoperation system that assists robot control and base relocation by inferring the user’s manipulation intent.  
**Technical Stack**: Unity (MRTK3), Meta Quest 3, ROS (Noetic), Python  ,Docker

## Author
 **Soma Okamoto**  
Meijo University, Sekiyama Laboratory  
- Research: Mixed Reality Human–Robot Interaction  
- Skills: Unity (C#), ROS, Python, Docker, Git, Meta Quest SDK  
<!-- - [LinkedIn](#) | [Portfolio Site](#) -->


## 🎥 Demo Video
▶️ [Watch on Google Drive](https://drive.google.com/file/d/1aXLi1SnfnAiOzHu7YugYYONfN54CpGNZ/view?usp=sharing)


---
## Graphical Abstract
An overview of **MASK (Mixed reality Adaptive Spatial and Kinematic support)**,  
which integrates **BaseMove**, **Aim Assist**, and **Spatial Visualization** modules into a unified MR–ROS framework.

![Visual Abstract](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/GA.png?raw=true)


---

## BaseMove System
A module for **base position suggestion and relocation** utilizing the **Inverse Reachability Map (IRM)**.  
It visualizes feasible robot base positions in the MR space, enabling users to intuitively guide and optimize robot placement for task execution.

![BaseMove](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像4.png?raw=true)

## Aim Assist with ManipulationSystem and UserTarget Detection
Implements **aim-assist control** that dynamically aligns the robot end-effector with the user’s intended target in real time.  
The **User Target Detection System** estimates the grasp target based on **hand posture** and **gaze direction**, providing a seamless interaction experience between the user and the robot.

![AimAssist Teleoperation](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像6.png?raw=true)


---

## SpacialVisualizationSystem
A real-time environment visualization module combining **YOLOv5**, **DeepSORT**, and **Point Cloud rendering**.

This system detects and tracks dynamic objects using YOLOv5 + DeepSORT,  
while simultaneously reconstructing the surrounding environment as a **3D point cloud** within the MR space.  
By combining **semantic tracking** and **geometric mapping**, it enables intuitive visualization of both **terrain structures** and **object motion** for enhanced situational awareness.

![SpatialVisualization](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/画像7.png?raw=true)




## ⚡ Quick Start


```bash
# 1) Clone
git clone https://github.com/soma-okamoto/MixedRealityProject-MRTKv3.git
cd MixedRealityProject-MRTKv3

# 2) Open Unity project
#   Unity 2022.3.xx / MRTK3 / Meta XR SDK / OpenXR
#   Open:  My projectg/  (then open the scene used for demo)

```
![Unity](https://github.com/soma-okamoto/MetaSDKv74-MRTKv3/blob/NotFX/Images/unity.png?raw=true)

## Related Repositories

https://github.com/soma-okamoto/Docker_Youbot_project_gradient.git

https://github.com/soma-okamoto/Docker_ReachabilityMap.git

https://github.com/soma-okamoto/Docker_youbot_sim.git