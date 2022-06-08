using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Valve.VR;

public class GridBuildingSystemVR : MonoBehaviour
{
    public static GridBuildingSystemVR Instance { get; private set; }

    public SteamVR_Action_Single squeezeAction;
    public SteamVR_Action_Boolean triggerAction;
    public SteamVR_Action_Boolean leftDirectionAction;
    public SteamVR_Action_Boolean rightDirectionAction;


    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOVGhosts;
    [SerializeField] private Material deleteGhostMaterial;
    [SerializeField] private Transform parentTransform;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    private PlacedObjectTypeSO placedObjectTypeSO;


    private Material lastHoveredMaterial;
    private Collider lastHoveredObject = null;


    private int gridWidth = 24;
    private int gridLength = 24;
    private int gridHeight = 24;
    private float cellSize = 10f;
    private float brickOffset = 4.8f;
    private float brickHeight = 9.6f;
    private float basePlateHeight = .65f;
    private float scale;

    private List<GridXZ<GridObject>> grids;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;
    private int currentPlacedObjectTypeSOIndex = 0;


    public event EventHandler OnSelectedBrickChanged;

    private void Awake()
    {
        Instance = this;

        this.scale = transform.localScale.x;
        this.gridWidth = 24;
        this.gridLength = 24;
        this.gridHeight = 24;
        this.brickHeight = 9.6f * scale;
        this.cellSize = 10 * scale;
        this.brickOffset = 4.8f * scale;
        this.basePlateHeight = .65f * scale * 2;

        grids = new List<GridXZ<GridObject>>();


        bool showDebug = true;

        for (int i = 0; i < gridHeight; i++)
        {
            grids.Add(new GridXZ<GridObject>(gridWidth, gridLength, cellSize, parentTransform.position + new Vector3(0, basePlateHeight + i * brickOffset, 0), (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z)));
            grids[i].debugLineColor = Color.magenta;
            grids[i].drawGridLines();
        }

        placedObjectTypeSO = placedObjectTypeSOList[0];

        Debug.Log("0/0 Pos: " + grids[0].GetWorldPosition(0, 0));
        Debug.Log("0/24 Pos: " + grids[0].GetWorldPosition(0, 24));
        Debug.Log("24/0 Pos: " + grids[0].GetWorldPosition(24,0));
        Debug.Log("Origin: " + grids[0].GetOriginPosition());

        RefreshSelectedObjectType();
    }




    private void LateUpdate()
    {
        LayerMask mask = LayerMask.GetMask("Brick");
        if (Physics.Raycast(leftControllerTransform.position, leftControllerTransform.forward, out RaycastHit raycastHit, 99f, mask))
        {
            Debug.DrawRay(leftControllerTransform.position, leftControllerTransform.forward * raycastHit.distance, Color.cyan);
            //Debug.Log("RayCastHit: " + raycastHit.point);

            if (lastHoveredObject != null)
                lastHoveredObject.gameObject.GetComponent<Renderer>().material = lastHoveredMaterial;

            
            lastHoveredObject = raycastHit.collider;
            Renderer hoverObjectRenderer = lastHoveredObject.gameObject.GetComponent<Renderer>();
            lastHoveredMaterial = hoverObjectRenderer.material;
            hoverObjectRenderer.material = deleteGhostMaterial;
        }
        else if(lastHoveredObject != null)
        {
            lastHoveredObject.gameObject.GetComponent<Renderer>().material = lastHoveredMaterial;
            lastHoveredObject = null;
            lastHoveredMaterial = null;
        }
    }


    private void Update()
    {
        //float triggerValue = squeezeAction.GetAxis(SteamVR_Input_Sources.RightHand);


        if(triggerAction.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            Debug.Log("Squoze!");

            try
            {
                Vector3 rayHit = GetControllerIntersectionPoint(rightControllerTransform.position, rightControllerTransform.forward);



                int gridNumber = GetGridNumber(rayHit);
                grids[gridNumber].GetXZ(rayHit, out int x, out int z);
                Debug.Log("X: " + x + " Y: " + z);

                Debug.Log("Grid number: " + gridNumber);


                List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);
                int gridNumberForBuild = GetBuildableGridNumber(gridPositionList, gridNumber);

                if (gridNumberForBuild >= 0)
                {
                    Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                    Vector3 placedObjectWorldPosition = grids[gridNumberForBuild].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumberForBuild].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);

                    PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(x, z), dir, placedObjectTypeSO, scale);


                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        grids[gridNumberForBuild].GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

                    }
                }

                /*
                // Test can build
                bool canBuild = true;
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    if (!grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                    {
                        // Cannot build at level 
                        canBuild = false;
                        break;
                    }
                }
                if (canBuild)
                {
                    Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                    Vector3 placedObjectWorldPosition = grids[gridNumber].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumber].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);

                    PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(x, z), dir, placedObjectTypeSO);


                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

                    }
                }
                else
                {
                    bool canBuildHigher = true;
                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        if (!grids[gridNumber + 1].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                        {
                            // Cannot build at level 
                            canBuildHigher = false;
                            break;
                        }
                    }
                    if (canBuildHigher)
                    {
                        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                        Vector3 placedObjectWorldPosition = grids[gridNumber + 1].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumber + 1].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);

                        PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(x, z), dir, placedObjectTypeSO);


                        foreach (Vector2Int gridPosition in gridPositionList)
                        {
                            grids[gridNumber + 1].GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

                        }
                    }
                    else
                        UtilsClass.CreateWorldTextPopup("Cannot build here!", MyUtilities.MyUtils.GetMouseWorldPosition_Instance());
                }
                */
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }


        }

       

        if(triggerAction.GetStateDown(SteamVR_Input_Sources.LeftHand))
        {

            // TODO: Check for deleteabilty


            // Delete selected object
            LayerMask mask = LayerMask.GetMask("Brick");
            if (Physics.Raycast(leftControllerTransform.position, leftControllerTransform.forward, out RaycastHit raycastHit, 99f, mask))
            {
                Debug.DrawRay(leftControllerTransform.position, leftControllerTransform.forward * raycastHit.distance, Color.cyan);
                //Debug.Log("RayCastHit: " + raycastHit.point);

                Vector3 rayHit = raycastHit.transform.position;
                rayHit += new Vector3(0, brickHeight * 0.001f, 0);
                Debug.Log("We hit: " + raycastHit.transform.gameObject.ToString());
                int gridNumber = GetGridNumber(rayHit);
                Debug.Log("Grid number: " + gridNumber);

                GridObject gridObject = grids[gridNumber].GetGridObject(rayHit);

                PlacedObject placedObject = gridObject.GetPlacedObject();
                if (placedObject != null)
                {
                    placedObject.DestroySelf();

                    List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();

                    foreach (Vector2Int gridPosition in gridPositionList)
                    {
                        grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                    }
                }
                else
                {
                    Debug.Log("No Object to Delete!");
                }
            }
        }



        




        if(rightDirectionAction.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            dir = PlacedObjectTypeSO.GetNextDir(dir);
            //UtilsClass.CreateWorldTextPopup("" + dir.ToString(), Camera.main.transform.forward * 2);
        }

        if(leftDirectionAction.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            dir = PlacedObjectTypeSO.GetPreviousDir(dir);
            //UtilsClass.CreateWorldTextPopup("" + dir.ToString(), Camera.main.transform.forward * 2);
        }


        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            placedObjectTypeSO = placedObjectTypeSOList[0];
            RefreshSelectedObjectType();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            placedObjectTypeSO = placedObjectTypeSOList[1];
            RefreshSelectedObjectType();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            placedObjectTypeSO = placedObjectTypeSOList[2];
            RefreshSelectedObjectType();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            placedObjectTypeSO = placedObjectTypeSOList[3];
            RefreshSelectedObjectType();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            placedObjectTypeSO = placedObjectTypeSOList[4];
            RefreshSelectedObjectType();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            placedObjectTypeSO = placedObjectTypeSOList[5];
            RefreshSelectedObjectType();
        }
    }


    public void RefreshSelectedObjectType()
    {
        OnSelectedBrickChanged?.Invoke(this, EventArgs.Empty);
    }









    public Vector3 GetControllerIntersectionPoint(Vector3 controllerPositioon, Vector3 direction)
    {
        LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");
        if (Physics.Raycast(controllerPositioon, direction, out RaycastHit raycastHit, 99f, mask))
        {
            Debug.DrawRay(controllerPositioon, direction * raycastHit.distance, Color.cyan);
            //Debug.Log("RayCastHit: " + raycastHit.point);
            return raycastHit.point;
        }
        else
        {
            Debug.DrawRay(controllerPositioon, direction * 1000, Color.white);
            //Debug.Log("Did not hit!");
            throw new NoIntersectionException("No intersection");
        }
    }








    public Vector3 GetSnapPoint()
    {
        Vector3 mousePosition = GetControllerIntersectionPoint(rightControllerTransform.position, rightControllerTransform.forward);
        int gridNumber = GetGridNumber(mousePosition);
        grids[gridNumber].GetXZ(mousePosition, out int x, out int z);

        if (placedObjectTypeSO != null)
        {
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);
            int gridNumberForBuild = GetBuildableGridNumber(gridPositionList, gridNumber);

            if(gridNumberForBuild >= 0)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grids[gridNumberForBuild].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumberForBuild].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);
                return placedObjectWorldPosition;
            }
            /*
            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                if (!grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    // Cannot build at level 
                    canBuild = false;
                    break;
                }
            }
            if (canBuild)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grids[gridNumber].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumber].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);
                return placedObjectWorldPosition;
            }
            bool canBuildHigher = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                if (!grids[gridNumber + 1].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    // Cannot build at level 
                    canBuildHigher = false;
                    break;
                }
            }
            if (canBuildHigher)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grids[gridNumber + 1].GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize + new Vector3(0, grids[gridNumber + 1].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);
                return placedObjectWorldPosition;
            }
            */
            else
            {
                throw new CannotBuildHereException();
            }
        }
        else
        {
            throw new CannotBuildHereException();
        }
        
    }



    public Quaternion GetPlacedObjectRotation()
    {
        if(placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }



    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }



    public PlacedObjectTypeSO GetCurrentGhosttPlacedObjectTypeSO()
    {
        return placedObjectTypeSOVGhosts[currentPlacedObjectTypeSOIndex];
    }






    public int GetBuildableGridNumber(List<Vector2Int> gridPositions, int startingGridNumber = 0)
    {
        for(int currentGrid = startingGridNumber; currentGrid < gridHeight; currentGrid++)
        {
            bool canBuildHere = true;
            foreach(Vector2Int gridPosition in gridPositions)
            {
                if(!grids[currentGrid].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    canBuildHere = false;
                    break;
                }
            }

            if (canBuildHere)
                return currentGrid;
        }

        return -1;
    }





    public int GetGridNumber(Vector3 worldPosition)
    {
        int gridNumber = Mathf.FloorToInt((worldPosition.y - grids[0].GetOriginPosition().y) / brickHeight);
        if (gridNumber < 0)
            gridNumber = 0;

        //Debug.Log("Grid number: " +  gridNumber);
        return gridNumber;
    }



    public class NoIntersectionException : Exception 
    { 
        public NoIntersectionException() { }

        public NoIntersectionException(string message) 
            : base(message) { }

        public NoIntersectionException(string message, Exception inner)
            : base(message, inner) { }
    }


    public class CannotBuildHereException : Exception
    {
        public CannotBuildHereException() { }

        public CannotBuildHereException(string message)
            : base(message) { }

        public CannotBuildHereException(string message, Exception inner)
            : base(message, inner) { }
    }


    public class GridObject
    {
        private GridXZ<GridObject> grid;
        private int x;
        private int z;
        private PlacedObject placedObject;

        public GridObject(GridXZ<GridObject> grid, int x, int z)
        {
            this.grid = grid;
            this.x = x;
            this.z = z;
        }


        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObejectChanged(x, z);
        }


        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }



        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObejectChanged(x, z);
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }


        public override string ToString()
        {
            return x + ", " + z + "\n" + placedObject;
        }
    }
}
