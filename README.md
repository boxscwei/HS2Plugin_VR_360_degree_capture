# HS2Plugin_VR_360_degree_capture

This plugins is useful when you try to capture 360-degree panoramic pictures even without a VR device.
Start the Studio normally before copy the release files into BepInEx\Plugins. Press F1 key to show the configration UI of this plugin.

By the VideoExport function provided by ![HSPlugins](https://github.com/IllusionMods/HSPlugins), It's possible to capture 360-degree panoramic pictures continually. However slightly modification of the source code of HSPlugins-r2.4\VideoExport.Core\VideoExport.cs is necessary. The modification version of the source-code of VideoExport.cs is attach in this repo.

This repo is focused on 360-degree panoramic pictures capturing.


If you only want to capture 360-degree panoramic pictures, you only need to use VR360VideoFramesCreator.dll. 
If you need to automatically capture video frames, you'll need to deploy VideoExport.dll as well. 
Please use the rewritte code version to rebuild the "VideoExport" project provided by HSPlugins.

![snapshot1](https://github.com/boxscwei/HS2Plugin_VR_360_degree_capture/blob/main/Using_1.png)

![snapshot2](https://github.com/boxscwei/HS2Plugin_VR_360_degree_capture/blob/main/Using_2.png)



