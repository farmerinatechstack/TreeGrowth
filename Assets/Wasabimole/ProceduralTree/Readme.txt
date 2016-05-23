
---------------------------------------------------------------------------------------------------------------------------
Procedural Tree - Simple tree mesh generation - © 2015 Wasabimole http://wasabimole.com
---------------------------------------------------------------------------------------------------------------------------

BASIC USER GUIDE

- Choose GameObject > Create Procedural > Procedural Tree from the Unity menu
- Select the object to adjust the tree's properties
- Click on Rand Seed to get a new tree of the same type
- Click on Rand Tree to change the tree type

---------------------------------------------------------------------------------------------------------------------------

ADVANCED USER GUIDE

- Drag the object to a project folder to create a Prefab (to keep a static snapshot of the tree)
- To add a collision mesh to the object, choose Add Component > Physics > Mesh Collider
- To add or remove detail, change the number of sides
- You can change the default diffuse bark materials for more complex ones (with bump-map, specular, etc.)
- Add or replace default materials by adding them to the SampleMaterials\ folder
- You can also change the tree generation parameters in REAL-TIME from your scripts (*)
- Use Unity's undo to roll back any unwanted changes

---------------------------------------------------------------------------------------------------------------------------

ADDITIONAL NOTES

The generated mesh will remain on your scene, and will only be re-computed if/when you change any tree parameters.

Branch(...) is the main tree generation function (called recursively), you can inspect/change the code to add new 
tree features. If you add any new generation parameters, remember to add them to the checksum in the Update() function 
(so the mesh gets re-computed when they change). If you add any cool new features, please share!!! ;-)

To generate a new tree at runtime, just follow the example in Editor\ProceduralTreeEditor.cs:CreateProceduralTree()

Additional scripts under ProceduralTree\Editor are optional, used to better integrate the trees into Unity.

(*) To change the tree parameters in real-time, just get/keep a reference to the ProceduralTree component of the 
tree GameObject, and change any of the public properties of the class.

---------------------------------------------------------------------------------------------------------------------------

>>> Please visit http://wasabimole.com/procedural-tree for more information, and to read a "How To .." tutorial

---------------------------------------------------------------------------------------------------------------------------

VERSION HISTORY

1.02 Error fixes update
- Fixed bug when generating the mesh on a rotated GameObject
- Fix error when building the project

1.00 First public release

---------------------------------------------------------------------------------------------------------------------------

Thank you for choosing Procedural Tree, we sincerely hope you like it!

Please send your feedback and suggestions to mailto://contact@wasabimole.com
