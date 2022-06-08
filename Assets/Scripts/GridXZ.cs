using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtilities;

public class GridXZ<TGridObject>
{
    public const int HEAT_MAP_MAX_VALUE = 100;
    public const int HEAT_MAP_MIN_VALUE = 0;


    public Color debugLineColor = Color.white;

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;

    private TGridObject[,] gridArray;
    private TextMesh[,] debugTextArray;


    public EventHandler<OnGridValueChangedEvenArgs> OnGridValueChanged;
    public class OnGridValueChangedEvenArgs : EventArgs
    {
        public int x;
        public int z;
    }

    public GridXZ(int width, int height, float cellSize, Vector3 originPosition, Func<GridXZ<TGridObject>, int, int, TGridObject>  createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];


        for(int x = 0; x < gridArray.GetLength(0); x++)
        {
            for(int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }



        

    }



    public void drawGridLines()
    {
        //bool showDebug = true;
        Debug.DrawLine(GetWorldPosition(0, 0), GetWorldPosition(0, 0) + new Vector3(0, 1, 0), Color.red, 100f);
        debugTextArray = new TextMesh[width, height];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                //debugTextArray[x, z] = MyUtils.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, cellSize) * 0.5f, 1, Color.white, TextAnchor.MiddleCenter);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), debugLineColor, 100f);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), debugLineColor, 100f);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), debugLineColor, 100f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), debugLineColor, 100f);

        OnGridValueChanged += (object sender, OnGridValueChangedEvenArgs evenArgs) =>
        {
                //debugTextArray[evenArgs.x, evenArgs.z].text = gridArray[evenArgs.x, evenArgs.z]?.ToString();
        };
    }



    public int GetWidth()
    {
        return width;
    }


    public int GetHeight()
    {
        return height;
    }


    public float GetCellSize()
    {
        return cellSize;
    }


    public Vector3 GetOriginPosition()
    {
        return originPosition;
    }


    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }




    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    { 
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);

        //Debug.Log("X: " + x + " Z: " + z);
    }


    public void SetGridObject(Vector3 worldPosition, TGridObject value)
    {
        int x, z;

        GetXZ(worldPosition, out x, out z);
        SetGridObject(x, z, value);
    }




    public void SetGridObject(int x, int z, TGridObject value)
    {
        if(x >= 0 && x >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
            if (OnGridValueChanged != null)
            {
                OnGridValueChanged(this, new OnGridValueChangedEvenArgs { x = x, z = z });
            }
        }
    }





    public void TriggerGridObejectChanged(int x, int z)
    {
        if (OnGridValueChanged != null)
        {
            OnGridValueChanged(this, new OnGridValueChangedEvenArgs { x = x, z = z });
        }
    }


    public TGridObject GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            return default(TGridObject);
        }
    }


    public TGridObject GetGridObject(Vector3 worldPosition)
    {
        int x, z;

        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }

   
}
