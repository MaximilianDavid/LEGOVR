# VRBrickBuilder
This system was used to conduct a user study as part of a master's thesis.




## Instructions
1. Install Steam VR and connect a compatible VR device to the PC
2. Download the code or clone the repository using git
3. Open the downloaded repository with a Unity Editor with Version 2021.x
4. Open MainScene located in Assets > Scenes
5. Run the Project






## Brick is stuck to hand bug (DEPRECATED)
It is possible that bricks get stuck to the model of the controller. If this happens, the bricks prefab within the scene needs to be duplicated and the old copy needs to be deleted.

![Bricks](https://user-images.githubusercontent.com/72796522/187219613-3445213d-666a-498f-9a27-41c37fc3d6e4.JPG)

Afterward, all bricks contained within the "Bricks" list of the BasePlate's "Grid Building System VR" script need to be deleted.

![BasePlateBefore](https://user-images.githubusercontent.com/72796522/187220087-572f1d2a-6d71-4789-b15b-ad2f0a93af4a.JPG)

Finally, all bricks inside the "Bricks" prefab need to be inserted into the "Bricks" list of the BasePlate's "Grid Building System VR" script.

![BricksPlacedInto](https://user-images.githubusercontent.com/72796522/187219930-4dc5bcb7-2d37-48a3-96c2-7998b49e3951.jpg)


Now the bricks should no longer be stuck to the hand model. If they still are, please get in touch with me.
