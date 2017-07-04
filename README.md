Q's VRChat MoCap animation
============

A unity project which you can use to record your own motion capture recordings directly out of VRChat.


![alt text](Media/Example_Recording.gif)

## How to install
Click "Clone or download" button on the right side and select Download ZIP.
Unzip onto your harddrive and then open project in Unity 5.3.4p1

## How to use
There's an example scene and example avatar set up in the project already that you can test your first recordings with.
The animations you record in VRChat will only work with the character you recorded them with so if you don't have source model for
the avatar it won't work as of now.

To test out with example avatar first go to "Scenes/UnityChan Avatar" scene and upload avatar to your account.
Then go into "Scenes/UnityChan Example Scene" scene and use the VRCchat SDK Build Control Panel to launch VRChat in "Test New Build" mode.

Once VRChat starts change Avatar to the UnityChan avatar you uploaded.
Then use the "Start recording" button to start your recording. Recording will start 2 seconds after button is pressed so you have time to prepare your start stance.
When you're done with the animation use the "Save recording" button to save your recording. You can record as many times as you want, no need to restart VRChat to record again.

Once you're done recording exit out of VRChat and locate your "Docucuments/My Games/VRChat/MoCap Recordings".
This folder should now have a .txt file for each of your recorded animations. Copy these into your AnimationFiles folder in the Unity project.

While you're in the "UnityChan Example Scene" select the recording files you brought in and then using the Menu bar at top go to "Avatar Recording/Record Selected Animations".
Your animations should now exist in the "Motion Capture Animations" folder in your project.

**Remember these animations will only work with the Avatar they where recorded with!!**

## How to setup an Avatar for mapping your recordings onto
Create a duplication of the "Scenes/Base Scene" scene. Rename it to have your avatar name in it so you can locate it easier later.

Bring in your Avatar into the project so you have the model to use for mapping your VRC animation file onto.
Drag your avatar into the scene, select the avatar and then using the Menu bar at top go to "Avatar Recording/Setup Selected Avatar".
The system will now setup this scene to use the avatar you dragged in and the avatar will be disabled so it's not in the way when recording in VRC.

Now just use same steps as when you brought in test animations with UnityChan.

**Remember these animations will only work with the Avatar they where recorded with!!**

## Notes
Project contains the following assets:  
* <a href="http://unity-chan.com/">SD UnityChan</a>