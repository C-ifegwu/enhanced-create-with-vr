# Enhanced Create with VR

A Unity VR project for the Meta Quest. It has the Create with VR course room (Chibueze_Room) and a second scene, the VR Room, with a physics door, puzzle sockets, a wall clock and animated hands. You can move between the two scenes without restarting.

Unity 6000.4.6f1, Universal Render Pipeline, XR Interaction Toolkit 3.4.1, OpenXR, XR Hands 1.7.3.

## Scenes
- Assets/Scenes/Chibueze_Room.unity: the course room with lessons 1.1 to 3.4, the two challenges, a welcome panel, a menu and a fireplace.
- Assets/Scenes/VR_Room.unity: the VR Room with grab objects, three sockets, a hinge door, an analog and digital clock, a control panel and teleport locomotion.

## Controls (Quest Touch controllers)
- Grip: grab an object.
- Trigger: press a button or select a pointed object.
- Course room: push the thumbstick forward and release to teleport.
- VR Room: hold A / X or touch the thumbstick, aim at the floor and squeeze the grip to teleport.
- Left Y button: show or hide the control panel in the VR Room.

## Scripts (Assets/Scripts)
- AnalogClock.cs: analog and digital clock driven by the system time.
- DoorHandleDrive.cs, DoorCollisionSetup.cs: grabbing and swinging the hinge door.
- HandPoseAnimator.cs: finger curl from the trigger and grip values.
- PuzzleTracker.cs: objectives board for the sockets and the door.
- HoverHighlight.cs: tint on hover and grab.
- ProjectorToggle.cs, FireToggle.cs: projector and fireplace toggles.
- SceneNavigator.cs: fade and async load between the two scenes.
- VRRoomMenu.cs: control panel, reset and teleport buttons.
- RoomSpawn.cs: places the player at a spawn anchor and handles recentering.

Assets/Editor holds the setup scripts that were used to build each lesson and the VR Room, plus a few play mode tests.

## Running
1. Open the project in Unity 6000.4.6f1 and let it import.
2. Open a scene in Assets/Scenes and press Play to test with the XR Interaction Simulator, or
3. Switch to Android, enable OpenXR with the Meta Quest feature group, make sure Chibueze_Room is scene 0 and VR_Room is scene 1, connect the headset and use Build And Run.

Submission details are in Docs/SUBMISSION.md.

## Author
Chibueze Ifegwu - https://github.com/C-ifegwu
