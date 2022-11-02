using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/*
 *  An object that is meant to be placed into a Grid
 *  
 *  Represents a brick
 */
public class PlacedObject : MonoBehaviour
{
    public PlacedObjectTypeSO placedObjectTypeSO;
    private Vector3 worldPosition;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector2Int origin;
    private PlacedObjectTypeSO.Dir dir;

    [SerializeField] private List<Vector2Int> occupiedGridPositions = new List<Vector2Int>();
    [SerializeField] private HashSet<PlacedObject> connectedToUpwards;
    [SerializeField] private HashSet<PlacedObject> connectedToDownwards;
    [SerializeField] private int gridNumber = -1;

    [SerializeField] private bool showAnchors = false;

    private bool pickedUp = false;
    private bool isPlacedInGrid = false;
    private bool hasBaseSupport = false;

    private float lineWidth = 0.001f;

    private Rigidbody rigidbody;


    
    private GameObject anchor;
    private GameObject frontLeftAnchor;
    private GameObject backLeftAnchor;
    private GameObject backRightAnchor;
    private GameObject visualBrick;





    /*
     *  Creates a new placedObject
     */
    public static PlacedObject Create(
        Vector3 worldPosition, 
        Vector2Int origin, 
        PlacedObjectTypeSO.Dir dir, 
        PlacedObjectTypeSO placedObjectTypeSO, 
        float scale)
    {
        Transform placedObjectTransform = 
            Instantiate(
                placedObjectTypeSO.prefab, 
                worldPosition, 
                Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0));
        placedObjectTransform.localScale = new Vector3(scale, scale, scale);

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();

        placedObject.gameObject.layer = 12;
        placedObject.placedObjectTypeSO = placedObjectTypeSO;
        placedObject.origin = origin;
        placedObject.dir = dir;
        placedObject.worldPosition = worldPosition;

        return placedObject;
    }




    private void Awake()
    {
        // Setup connection Hashes
        connectedToDownwards = new HashSet<PlacedObject>();
        connectedToUpwards = new HashSet<PlacedObject>();

        // Setup all anchors of the brick
        anchor = transform.GetChild(0).gameObject.transform.GetChild(1).gameObject;
        frontLeftAnchor = transform.GetChild(0).gameObject.transform.GetChild(2).gameObject;
        backLeftAnchor = transform.GetChild(0).gameObject.transform.GetChild(3).gameObject;
        backRightAnchor = transform.GetChild(0).gameObject.transform.GetChild(4).gameObject;
        visualBrick = transform.GetChild(0).gameObject.transform.GetChild(0).gameObject;

        if(!showAnchors)
        {
            anchor.SetActive(false);
            frontLeftAnchor.SetActive(false);
            backLeftAnchor.SetActive(false);
            backRightAnchor.SetActive(false);
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rigidbody = GetComponent<Rigidbody>();
    }






    private void LateUpdate()
    {
        if (!hasBaseSupport && !pickedUp && rigidbody.isKinematic)
            makePhysicsEnabled();
    }






    public List<Vector2Int> GetGridPositionList()
    {
        return placedObjectTypeSO.GetGridPositionList(origin, dir);
    }






    /*
     *  Returns the direction, closest to the current rotation
     *  of the object
     */
    public PlacedObjectTypeSO.Dir GetClosestDir()
    {
        Vector3 angles = transform.eulerAngles;

        float [] difs = { 
            Mathf.Abs(Mathf.DeltaAngle(angles.y, 0)),
            Mathf.Abs(Mathf.DeltaAngle(angles.y, 90)),
            Mathf.Abs(Mathf.DeltaAngle(angles.y, 180)),
            Mathf.Abs(Mathf.DeltaAngle(angles.y, 270))
            };

        float min = difs.Min();

        int index = -1;

        for(int i = 0; i < difs.Length; i++)
        {
            if(difs[i] == min)
            {
                index = i;
                break;
            }    
        }

        switch(index)
        {
            case 0:
                //Debug.Log("DOWN");
                return PlacedObjectTypeSO.Dir.Down;
                break;
            case 1:
                //Debug.Log("LEFT");
                return PlacedObjectTypeSO.Dir.Left;
                break;
            case 2:
                //Debug.Log("UP");
                return PlacedObjectTypeSO.Dir.Up;
                break;
            case 3:
                //Debug.Log("RIGHT");
                return PlacedObjectTypeSO.Dir.Right;
                break;
            default:
                //Debug.Log("No Angle Found!");
                break;
        }

        return PlacedObjectTypeSO.Dir.Down;
    }





    /*
     *  Resets the object to its starting position
     *  and orientation
     */
    public void RevertToStartingPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }





    public void DestroySelf()
    {
        Destroy(gameObject);
    }







    public void pickUp()
    {
        pickedUp = true;
    }






    public void putDown()
    {
        pickedUp = false;
    }




    public void AddToDownwardConnections(PlacedObject placedObject)
    {
        connectedToDownwards.Add(placedObject);
    }

    public void RemoveFromDownwardConnections(PlacedObject placedObject)
    {
        connectedToDownwards.Remove(placedObject);
    }

    public void ClearDownwardConnections()
    {
        connectedToDownwards.Clear();
    }


    public HashSet<PlacedObject> DownwardConnections
    {
        get { return connectedToDownwards; }
        set { connectedToDownwards = value; }
    }



    public GameObject Anchor
    {
        get { return anchor; }
    }


    public GameObject FrontLeftAnchor
    {
        get { return frontLeftAnchor; }
    }

    public GameObject BackLeftAnchor
    {
        get { return backLeftAnchor; }
    }


    public GameObject BackRightAnchor
    {
        get { return backRightAnchor; }
    }


    public GameObject VisualBrick
    {
        get { return visualBrick; }
    }


    public void AddToUpwardConnections(PlacedObject placedObject)
    {
        connectedToUpwards.Add(placedObject);
    }



    public void RemoveFromUpwardConnections(PlacedObject placedObject)
    {
        connectedToUpwards.Remove(placedObject);
    }



    public void ClearUpwardConnections()
    {
        connectedToUpwards.Clear();
    }


    public HashSet<PlacedObject> UpwardConnections
    {
        get { return connectedToUpwards; }
        set { connectedToUpwards = value; }
    }


    public void SetOrigin(Vector2Int value)
    {
        origin = value;
    }

    public void SetDir(PlacedObjectTypeSO.Dir value)
    {
        dir = value;
    }

    public void makeKinematic()
    {
        Debug.Log("Making " + this.ToString() + " Kinematic!");
        rigidbody.isKinematic = true;
    }

    public void makePhysicsEnabled()
    {
        Debug.Log("Making " + this.ToString() + " Physics enabled!");
        rigidbody.isKinematic = false;
    }


    public void ignoreCollisions(bool ignore = true)
    {
        if(ignore)
        {
            MyUtilities.MyUtils.SetLayerRecursively(this.gameObject, 13);
            Debug.Log("Making " + this.ToString() + "Ignore Collisions!");
        }
        else
        {
            MyUtilities.MyUtils.SetLayerRecursively(this.gameObject, 12);
            Debug.Log("Making " + this.ToString() + "Receive Collisions!");
        }
    }



    public bool HasBaseSupport()
    {
        return hasBaseSupport;
    }

    public void SetBaseSupport(bool value)
    {
        hasBaseSupport = value;
    }

    public int GetGridNumber()
    {
        return gridNumber;
    }


    public void SetGridNumber(int number)
    {
        gridNumber = number;
    }


    public List<Vector2Int> OccupiedGridPositions
    {
        get { return occupiedGridPositions; }
        set { occupiedGridPositions = value; }
    }


    public bool IsPlacedInGrid
    {
        get { return isPlacedInGrid; }
        set { isPlacedInGrid = value; }
    }


    public bool isPickedUp()
    {
        return pickedUp;
    }
}
