using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Valve.VR;


/*
 *  Logic to handle building a brick structure within a 3D grid in VR
 */
public class GridBuildingSystemVR : MonoBehaviour
{
    public static GridBuildingSystemVR Instance { get; private set; }

    public SteamVR_Action_Single squeezeAction;
    public SteamVR_Action_Boolean triggerAction;
    public SteamVR_Action_Boolean leftDirectionAction;
    public SteamVR_Action_Boolean rightDirectionAction;
    public SteamVR_Action_Boolean downDirectionAction;


    [SerializeField] private float maximumAngleCorrection;
    [SerializeField] private Ghost ghost;
    [SerializeField] private List<PlacedObject> bricks;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOVGhosts;
    [SerializeField] private Material deleteGhostMaterial;
    [SerializeField] private Material previewGhostMaterial;
    [SerializeField] private Transform parentTransform;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private Renderer buildManualScreen;
    [SerializeField] private List<Material> buildManualPages;


    private int currentBuildManualPage = 0;


    

    private LineRenderer anchorLineRenderer;
    private LineRenderer frontLeftLineRenderer;
    private LineRenderer backLeftLineRenderer;
    private LineRenderer backRightLineRenderer;



    [SerializeField] private List<List<PlacedObject>> placedBricks = new List<List<PlacedObject>>();


    private PlacedObjectTypeSO placedObjectTypeSO;



    private Transform currentlyHeldObject = null;
    private PlacedObject currentlyHeldPlacedObject = null;



    private LineRenderer lineRenderer = null;


    [SerializeField] private int gridWidth = 24;
    [SerializeField] private int gridLength = 24;
    [SerializeField] private int gridHeight = 24;
    [SerializeField] private float cellSize = 10f;
    [SerializeField] private float brickOffset = 4.8f;
    [SerializeField] private float brickHeight = 9.6f;
    [SerializeField] private float basePlateHeight = .65f;
    [SerializeField] private float scale = 1f;



    [SerializeField] private float maximumSnapDistance = 9.6f * 1.5f;


    [SerializeField] private float previewLineWidth = 0.001f;


    private List<GridXZ<GridObject>> grids;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;


    public event EventHandler OnSelectedBrickChanged;




    /*
     *  Used to release a held Brick
     *  
     *  Snaps the brick to the grid, if it is currently near a valid grid position
     */
    public void releaseBrick()
    {
        Transform brickTransform = currentlyHeldObject;
        PlacedObject heldBrick = currentlyHeldPlacedObject;
        GameObject heldVisualBrick = heldBrick.VisualBrick;

        // Turn on Collisions again
        heldBrick.ignoreCollisions(false);

        // Reset currently held Object
        currentlyHeldObject = null;
        currentlyHeldPlacedObject = null;
        placedObjectTypeSO = null;


        ghost.Deactivate();


        Vector3 snapPoint = Vector3.zero;


        anchorLineRenderer.enabled = false;
        frontLeftLineRenderer.enabled = false;
        backLeftLineRenderer.enabled = false;
        backRightLineRenderer.enabled = false;


        Vector3 absAngles = new Vector3(
            Mathf.Abs(brickTransform.eulerAngles.x),
            Mathf.Abs(brickTransform.eulerAngles.y),
            Mathf.Abs(brickTransform.eulerAngles.z));

        float absDifX = Mathf.Abs(Mathf.DeltaAngle(brickTransform.eulerAngles.x, 0));
        float absDifZ = Mathf.Abs(Mathf.DeltaAngle(brickTransform.eulerAngles.z, 0));
        float distanceToCollision = 0f;



        try
        {
            // Calculate the snap point for the held brick
            LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");
            Physics.Raycast(heldBrick.transform.position, Vector3.down, out RaycastHit raycastHit, 99f, mask);

            if (!raycastHit.collider)
                throw new CannotBuildHereException();

            snapPoint = GetSnapPoint(heldBrick);
            int gridNumberForBuild = GetGridNumber(new Vector3(snapPoint.x, snapPoint.y + brickHeight * 0.5f, snapPoint.z));
            //grids[gridNumberForBuild].GetXZ(raycastHit.point, out int x, out int z);
            grids[gridNumberForBuild].GetXZ(snapPoint, out int x, out int z);

            Debug.Log("Grid number: " + gridNumberForBuild);

            if (absDifX > maximumAngleCorrection)
            {
                Debug.Log("Angle on X too far!");
                throw new CannotBuildHereException();
            }

            if (absDifZ > maximumAngleCorrection)
            {
                Debug.Log("Angle on X too far!");
                throw new CannotBuildHereException();
            }

            if (distanceToCollision > maximumSnapDistance)
            {
                Debug.Log("Collision too far away!");
                throw new CannotBuildHereException();
            }


            if (gridNumberForBuild >= 0)
            {
                PlacedObjectTypeSO.Dir dir = heldBrick.GetClosestDir();
                List<Vector2Int> gridPositionList = heldBrick.placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);
                Vector2Int rotationOffset = heldBrick.placedObjectTypeSO.GetRotationOffset(dir);
                heldBrick.SetBaseSupport(true);
                heldBrick.IsPlacedInGrid = true;
                heldBrick.makeKinematic();
                brickTransform.position = snapPoint;
                brickTransform.rotation = GetPlacedObjectRotation(heldBrick);

                Debug.Log("Brick occupies:");
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grids[gridNumberForBuild].GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(heldBrick);
                    Debug.Log(gridPosition);
                }
                Debug.Log("In grid " + gridNumberForBuild);


                // Set Grid number and Position for placed Brick
                heldBrick.OccupiedGridPositions = gridPositionList;
                heldBrick.SetGridNumber(gridNumberForBuild);


                // Set Connections if the current brick is not set on the baseplate
                if (gridNumberForBuild > 0)
                {
                    ConnectBrick(heldBrick);
                }

                // Add the Brick to the List of placed Bricks
                placedBricks[gridNumberForBuild].Add(heldBrick);

                // Set Layer for the brick
                MyUtilities.MyUtils.SetLayerRecursively(heldBrick.gameObject, 12);
            }




        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }

        heldBrick.putDown();
    }


















    /*
     *  Pickup the given brick object 
     */
    public void pickupBrick(PlacedObject placedObject)
    {
        placedObject.makePhysicsEnabled();
        placedObject.pickUp();

        
        // Set brick as currently held object
        currentlyHeldPlacedObject = placedObject;
        currentlyHeldObject = placedObject.transform;
        placedObjectTypeSO = placedObject.placedObjectTypeSO;
        placedObject.ignoreCollisions();
        //MyUtilities.MyUtils.SetLayerRecursively(currentlyHeldPlacedObject.gameObject, 0);

        RefreshSelectedObjectType();

        if (placedObject.HasBaseSupport())
        {
            // Remove Brick from placed Bricks
            placedBricks[placedObject.GetGridNumber()].Remove(placedObject);


            Debug.Log("Removing " + placedObject + " from grid!");
            RemoveFromGrid(placedObject);
            Debug.Log("Removing connections of " + placedObject);
            RemoveAllConnectionsOf(placedObject);
            Debug.Log("DONE!");

            // Check integrity of remaining bricks
            CheckBrickConnections();
        }


        currentlyHeldPlacedObject.OccupiedGridPositions.Clear();
        
    }















    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        Instance = this;


        this.scale = transform.localScale.x;
        this.brickHeight = 9.6f * scale;
        this.cellSize = 10 * scale;
        this.brickOffset = 4.8f * scale;
        this.basePlateHeight = .65f * scale * 2;
        this.maximumSnapDistance = brickHeight * 1.5f;

        
        grids = new List<GridXZ<GridObject>>();


        // Set materials for manual screen
        Material[] screenMats = buildManualScreen.materials;
        screenMats[1] = buildManualPages[0];
        buildManualScreen.materials = screenMats;


        bool showDebug = true;
        for (int i = 0; i < gridHeight; i++)
        {
            // Add Lists for Bricks within each grid
            placedBricks.Add(new List<PlacedObject>());


            // Add Grids
            grids.Add(
                new GridXZ<GridObject>(
                    gridWidth, 
                    gridLength, 
                    cellSize, 
                    parentTransform.position + new Vector3(0, basePlateHeight + i * brickOffset, 0), 
                    (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z)));

            if (i == 0 && showDebug)
            {
                grids[i].debugLineColor = Color.magenta;
                grids[i].drawGridLines();
            }
        }


        placedObjectTypeSO = placedObjectTypeSOList[0];

        // Initialize line renderers
        GameObject anchorRendererObject = new GameObject("anchorRendererObject");
        anchorLineRenderer = anchorRendererObject.AddComponent<LineRenderer>();
        anchorLineRenderer.startWidth = previewLineWidth;
        anchorLineRenderer.endWidth = previewLineWidth;
        anchorLineRenderer.material = previewGhostMaterial;

        GameObject frontLeftAnchorRendererObject = new GameObject("frontLeftAnchorRendererObject");
        frontLeftLineRenderer = frontLeftAnchorRendererObject.AddComponent<LineRenderer>();
        frontLeftLineRenderer.startWidth = previewLineWidth;
        frontLeftLineRenderer.endWidth = previewLineWidth;
        frontLeftLineRenderer.material = previewGhostMaterial;

        GameObject backLeftAnchorRendererObject = new GameObject("backLeftAnchorRendererObject");
        backLeftLineRenderer = backLeftAnchorRendererObject.AddComponent<LineRenderer>();
        backLeftLineRenderer.startWidth = previewLineWidth;
        backLeftLineRenderer.endWidth = previewLineWidth;
        backLeftLineRenderer.material = previewGhostMaterial;

        GameObject backRightAnchorRendererObject = new GameObject("backRightAnchorRendererObject");
        backRightLineRenderer = backRightAnchorRendererObject.AddComponent<LineRenderer>();
        backRightLineRenderer.startWidth = previewLineWidth;
        backRightLineRenderer.endWidth = previewLineWidth;
        backRightLineRenderer.material = previewGhostMaterial;


        anchorLineRenderer.enabled = false;
        frontLeftLineRenderer.enabled = false;
        backLeftLineRenderer.enabled = false;
        backRightLineRenderer.enabled = false;


        RefreshSelectedObjectType();
        ghost.Deactivate();
    }

















    private void LateUpdate()
    {
        if(currentlyHeldPlacedObject != null)
        {

            // Get all corners of held brick
            GameObject anchor = currentlyHeldPlacedObject.Anchor;
            GameObject frontLeftAnchor = currentlyHeldPlacedObject.FrontLeftAnchor;
            GameObject backLeftAnchor = currentlyHeldPlacedObject.BackLeftAnchor;
            GameObject backRightAnchor = currentlyHeldPlacedObject.BackRightAnchor;


            // Get the angles of the held brick
            Vector3 absAngles = new Vector3(
                Mathf.Abs(currentlyHeldObject.eulerAngles.x),
                Mathf.Abs(currentlyHeldObject.eulerAngles.y),
                Mathf.Abs(currentlyHeldObject.eulerAngles.z));

            // Calculate angle offsets
            float absDifX = Mathf.Abs(Mathf.DeltaAngle(currentlyHeldObject.eulerAngles.x, 0));
            float absDifZ = Mathf.Abs(Mathf.DeltaAngle(currentlyHeldObject.eulerAngles.z, 0));
            float distanceToCollision = 0f;


            // Calculate estimated placement for currently held brick
            RaycastHit hit;
            LayerMask previewMask = LayerMask.GetMask("GridBuildingSystem", "Brick");
            Vector3 floorNormal = new Vector3(currentlyHeldObject.position.x, 0, currentlyHeldObject.position.z).normalized;
            Physics.Raycast(currentlyHeldObject.position, Vector3.down, out hit, 999f, previewMask);
            if (hit.collider)
            {
                // Get Gridnumbers
                int hitGridNumber = GetGridNumber(hit.point);
                int heldGridNumber = GetGridNumber(new Vector3(anchor.transform.position.x, anchor.transform.position.y + brickHeight*0.5f, anchor.transform.position.z));
                grids[heldGridNumber].GetXZ(anchor.transform.position, out int heldX, out int heldZ);
                currentlyHeldPlacedObject.SetOrigin(new Vector2Int(heldX, heldZ));
                currentlyHeldPlacedObject.SetDir(currentlyHeldPlacedObject.GetClosestDir());

                // Get highest brick between currently held location and RayCastHit
                PlacedObject highestBrick = CalculateHighestBrickBetween(
                    hitGridNumber, 
                    heldGridNumber,
                    currentlyHeldPlacedObject.GetGridPositionList());

                // Set collision distance to correct value
                if (highestBrick != null)
                {
                    Vector3 relHeldPosition = new Vector3(0, currentlyHeldObject.position.y, 0);
                    Vector3 relBrickPosition = new Vector3(0, highestBrick.transform.position.y + brickHeight, 0);
                    distanceToCollision = Mathf.Abs(Vector3.Distance(relHeldPosition, relBrickPosition));
                }
                else
                {
                    distanceToCollision = Mathf.Abs(Vector3.Distance(hit.point, currentlyHeldObject.position));
                }


                // Display Guide Lines
                anchorLineRenderer.enabled = true;
                frontLeftLineRenderer.enabled = true;
                backLeftLineRenderer.enabled = true;
                backRightLineRenderer.enabled = true;

                anchorLineRenderer.SetPosition(0, anchor.transform.position);
                anchorLineRenderer.SetPosition(1, new Vector3(anchor.transform.position.x, hit.point.y, anchor.transform.position.z));

                frontLeftLineRenderer.SetPosition(0, frontLeftAnchor.transform.position);
                frontLeftLineRenderer.SetPosition(1, new Vector3(frontLeftAnchor.transform.position.x, hit.point.y, frontLeftAnchor.transform.position.z));

                backLeftLineRenderer.SetPosition(0, backLeftAnchor.transform.position);
                backLeftLineRenderer.SetPosition(1, new Vector3(backLeftAnchor.transform.position.x, hit.point.y, backLeftAnchor.transform.position.z));

                backRightLineRenderer.SetPosition(0, backRightAnchor.transform.position);
                backRightLineRenderer.SetPosition(1, new Vector3(backRightAnchor.transform.position.x, hit.point.y, backRightAnchor.transform.position.z));
            }
            else
            {
                distanceToCollision = float.MaxValue;


                anchorLineRenderer.enabled = false;
                frontLeftLineRenderer.enabled = false;
                backLeftLineRenderer.enabled = false;
                backRightLineRenderer.enabled = false;
            }



            if (distanceToCollision <= maximumSnapDistance && !ghost.IsActive())
            {
                // Display ghost
                ghost.Activate();


            }
            if(distanceToCollision > maximumSnapDistance && ghost.IsActive())
            {
                ghost.Deactivate();
            }



        }
        else
        { if (ghost.IsActive())
                ghost.Deactivate();
        }
    }


    private void Update()
    {
        if(rightDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Advance manual page
            TurnManualPageForward();
        }

        if(leftDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Go back 1 manual page
            TurnManualPageBackward();
        }


       if(downDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            ResetLooseBrickPositions();
        }
    }




    /*
     *  Refreshes the currently held birkc type
     */
    public void RefreshSelectedObjectType()
    {
        OnSelectedBrickChanged?.Invoke(this, EventArgs.Empty);
    }









    /*
     *  Calculates a snap point for the given brick object
     */
    public Vector3 GetSnapPoint(PlacedObject placedObject)
    {
        if (placedObject == null)
            throw new CannotBuildHereException();

        GameObject anchor;
        GameObject visualBrick = placedObject.VisualBrick;

        // Calculate closest snapping direction
        PlacedObjectTypeSO.Dir dir = placedObject.GetClosestDir();
        Debug.Log("Orientation: " + dir);

        // Set anchor according to rotation
        switch(dir)
        {
            // Brick rotated by 90°
            case PlacedObjectTypeSO.Dir.Left:
                anchor = placedObject.BackRightAnchor;
                break;

            // Brick rotated by 180°
            case PlacedObjectTypeSO.Dir.Up:
                anchor = placedObject.BackLeftAnchor;
                break;

            // Brick rotated by 270°
            case PlacedObjectTypeSO.Dir.Right:
                anchor = placedObject.FrontLeftAnchor;
                break;

            // Brick not rotated
            case PlacedObjectTypeSO.Dir.Down:
            default:
                anchor = placedObject.Anchor;
                break;
        }

        anchor.SetActive(true);
        Debug.Log("Activated " + anchor);
        

        LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");
        Physics.Raycast(anchor.transform.position, Vector3.down, out RaycastHit raycastHit, 99f, mask);

        if (!raycastHit.collider)
            throw new CannotBuildHereException();

        // Get gridNumber of the grid the User is holding the brick in
        int heldInGridNumber = GetGridNumber(
            new Vector3(
                visualBrick.transform.position.x,
                visualBrick.transform.position.y + brickHeight * 0.5f,
                visualBrick.transform.position.z));
        grids[heldInGridNumber].GetXZ(anchor.transform.position, out int heldX, out int heldZ);

        // Calculate gridNumber of where the rayCast hits
        int gridNumber = GetGridNumber(raycastHit.point);
        grids[gridNumber].GetXZ(raycastHit.point, out int x, out int z);


        // Handle Edge cases
        if (x == gridLength)
            x -= 1;
        if (x == -1)
            x = 0;
        if (z == gridWidth)
            z -= 1;
        if (z == -1)
            z = 0;


        



        
        List<Vector2Int> gridPositionList = placedObject.placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);

        // Calculate highest Brick if there is a Brick between the RaycastHit and the held position
        PlacedObject highestBrickBetween = CalculateHighestBrickBetween(gridNumber, heldInGridNumber, gridPositionList);


        // Calculate the grid number to snap the brick into
        int gridNumberForBuild = 0;
        if (highestBrickBetween != null)
        {
            gridNumberForBuild = GetBuildableGridNumber(gridPositionList, GetGridNumber(
                new Vector3(
                    highestBrickBetween.transform.position.x,
                    highestBrickBetween.transform.position.y + brickHeight * 0.5f,
                    highestBrickBetween.transform.position.z)));
        }
        else
        {
            gridNumberForBuild = GetBuildableGridNumber(gridPositionList, gridNumber);
        }


        // Return the snapped position for the brick
        if (gridNumberForBuild >= 0)
        {
            Vector2Int rotationOffset = placedObject.placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = 
                grids[gridNumberForBuild].GetWorldPosition(x, z, dir) 
                + new Vector3(0, grids[gridNumberForBuild].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);

            return placedObjectWorldPosition;
        }
        else
        {
            throw new CannotBuildHereException();
        }
    }




    /*
     *  Returns the rotation quaternion for the currently held brick 
     */
    public Quaternion GetPlacedObjectRotation()
    {
        if(placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(currentlyHeldPlacedObject.GetClosestDir()), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }



    
    
    /*
     *  Returns the rotation quaternion of the given brick
     */
    public Quaternion GetPlacedObjectRotation(PlacedObject placedObject)
    {
        if (placedObject != null)
        {
            return Quaternion.Euler(0, placedObject.placedObjectTypeSO.GetRotationAngle(placedObject.GetClosestDir()), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }




    /*
     *  Goes through the list of all placed bricks starting with the lowest grid.
     *  Removes the brick from the grid and makes it physics enabled if it's not 
     *  supported by a brick below.
     */
    public void CheckBrickConnections()
    {
        for(int currentGrid = 1; currentGrid < placedBricks.Count; currentGrid++)
        {
            List<PlacedObject> bricksInCurrentGrid = new List<PlacedObject>(placedBricks[currentGrid]);
            foreach (PlacedObject brick in bricksInCurrentGrid)
            {
                if (brick.DownwardConnections.Count <= 0)
                {
                    RemoveAllConnectionsOf(brick);
                    RemoveFromGrid(brick);
                    brick.makePhysicsEnabled();
                }
            }
        }
    }




    /*
     *  Removes a given brick from the grid.
     */
    public void RemoveFromGrid(PlacedObject placedObject)
    {
        int gridNumber = placedObject.GetGridNumber();

        if (gridNumber < 0)
            return;

        Debug.Log("Removing from Grid " + gridNumber);

        foreach (Vector2Int gridPosition in placedObject.OccupiedGridPositions)
        {
            grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
        }

        placedBricks[gridNumber].Remove(placedObject);
        placedObject.IsPlacedInGrid = false;
        placedObject.SetBaseSupport(false);
        placedObject.SetGridNumber(-1);
    }







    /*
     *  Connects the given brick to all bricks directly below it
     */
    private void ConnectBrick(PlacedObject placedObject)
    {
        HashSet<PlacedObject> downward = new HashSet<PlacedObject>();


        int gridBelow = placedObject.GetGridNumber() - 1;
        Debug.Log("Grid below is: " + gridBelow);
        if (gridBelow < 0)
            return;

        // Get all bricks below the given brick
        foreach(Vector2Int position in placedObject.OccupiedGridPositions)
        {
            PlacedObject objectInSpaceBelow = 
                grids[gridBelow].GetGridObject(position.x, position.y).GetPlacedObject();// y == z

            Debug.Log("Donward of " + position + " is: " + objectInSpaceBelow);
            if(objectInSpaceBelow != null)
                downward.Add(objectInSpaceBelow); 
        }

        Debug.Log(placedObject + " connected downward to " + downward.Count + " bricks");
        Debug.Log(placedObject + " connected downward to " + downward);


        // Connect the previously calculated bricks to the current one
        foreach (PlacedObject brick in downward)
        {     
            if (brick == null)
            {
                Debug.Log("Downward brick is null!");
            }
            Debug.Log(brick.gameObject.ToString());
            placedObject.AddToDownwardConnections(brick);
            brick.AddToUpwardConnections(placedObject);
        }
    }









    /*
     *  Removes all brick connections for the given brick
     */
    public void RemoveAllConnectionsOf(PlacedObject placedObject)
    {
        foreach(PlacedObject downwardBrick in placedObject.DownwardConnections)
        {
            downwardBrick.RemoveFromUpwardConnections(placedObject);
        }
        placedObject.ClearDownwardConnections();


        foreach(PlacedObject upwardBrick in placedObject.UpwardConnections)
        {
            upwardBrick.RemoveFromDownwardConnections(placedObject);
        }
        placedObject.ClearDownwardConnections();
    }




    /*
     *  Returns the scriptable Object for the currently held brick
     */
    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }





    /*
     *  Returns the scriptable object for the ghost, depending on the currently held brick
     */
    public PlacedObjectTypeSO GetCurrentGhosttPlacedObjectTypeSO()
    {
        if (currentlyHeldPlacedObject != null)
            return currentlyHeldPlacedObject.placedObjectTypeSO;
        else
            return null;
    }






    /*
     *  Returns the number of the first grid that has no bricks occupying the given positions
     *  Starting with the given grid number
     */
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





    /*
     *  Returns the number of the corresponding grid, for a given world position
     */
    public int GetGridNumber(Vector3 worldPosition)
    {
        int gridNumber = Mathf.FloorToInt((worldPosition.y - grids[0].GetOriginPosition().y) / brickHeight);
        if (gridNumber < 0)
            gridNumber = 0;

        return gridNumber;
    }




    /*
     *  Returns the brick with the highest grid number, with at least one position from the given position list.
     *  Starts with the given lowerGrid and stops at the given higherGrid.
     *  
     *  Returns null if no brick is found!
     */
    private PlacedObject CalculateHighestBrickBetween(int lowerGrid, int higherGrid, List<Vector2Int> gridPositions)
    {
        for(int currentGridNumber = higherGrid; currentGridNumber >= lowerGrid; currentGridNumber--)
        {
            List<PlacedObject> bricksInCurrentGrid = placedBricks[currentGridNumber];
            foreach(PlacedObject brick in bricksInCurrentGrid)
            {
                foreach(Vector2Int gridPosition in brick.OccupiedGridPositions)
                {
                    if (gridPositions.Contains(gridPosition))
                        return brick;
                }
            }
        }
        return null;
    }




    /*
     *  Advances the manual by one page
     */
    private void TurnManualPageForward()
    {
        // If at last page do nothing
        if (currentBuildManualPage >= buildManualPages.Count - 1)
        {
            currentBuildManualPage = buildManualPages.Count - 1;
            return;
        }
        else
        {
            // Show next page
            currentBuildManualPage++;
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage];
            buildManualScreen.materials = screenMats;
        }
    }




    /*
     *  Goes back one page in the manual
     */
    private void TurnManualPageBackward()
    {
        // If at first page do nothing
        if(currentBuildManualPage <= 0)
        {
            currentBuildManualPage = 0;
            return;
        }
        else
        {
            // Show previous page
            currentBuildManualPage--;
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage];
            buildManualScreen.materials = screenMats;
        }
    }





    /*
     *  Resets all bricks with no position within the grid to their
     *  starting position
     */
    private void ResetLooseBrickPositions()
    {
        foreach(PlacedObject brick in bricks)
        {
            if (!brick.IsPlacedInGrid)
                brick.RevertToStartingPosition();
        }
    }




    /*
     *  Exception for not getting a raycast intersection
     */
    public class NoIntersectionException : Exception 
    { 
        public NoIntersectionException() { }

        public NoIntersectionException(string message) 
            : base(message) { }

        public NoIntersectionException(string message, Exception inner)
            : base(message, inner) { }
    }



    /*
     *  Exception for illegal build positions
     */
    public class CannotBuildHereException : Exception
    {
        public CannotBuildHereException() { }

        public CannotBuildHereException(string message)
            : base(message) { }

        public CannotBuildHereException(string message, Exception inner)
            : base(message, inner) { }
    }




    /*
     *  Placeholder object to insert into the grid
     */
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
            //grid.TriggerGridObejectChanged(x, z);
        }


        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }



        public void ClearPlacedObject()
        {
            placedObject = null;
            //grid.TriggerGridObejectChanged(x, z);
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

    public PlacedObject GetCurrentlyHeldPlacedObject()
    {
        return currentlyHeldPlacedObject;
    }
}
