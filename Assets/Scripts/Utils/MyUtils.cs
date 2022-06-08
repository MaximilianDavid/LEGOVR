using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MyUtilities
{
    public class MyUtils
    {



        public static TextMesh CreateWorldText(
       string text,
       Transform parent = null,
       Vector3 localPosition = default(Vector3),
       int fontSize = 40,
       Color? color = null,
       TextAnchor textAnchor = TextAnchor.UpperLeft,
       TextAlignment textAlignment = TextAlignment.Left,
       int sortingOrder = 5000)
        {
            if (color == null)
                color = Color.white;
            return CreateWorldText(parent, text, localPosition, fontSize, (Color)color, textAnchor, textAlignment, sortingOrder);
        }


        public static TextMesh CreateWorldText(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment, int sortingOrder)
        {
            GameObject gameObject = new GameObject("World_Text", typeof(TextMesh));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;

            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            textMesh.anchor = textAnchor;
            textMesh.alignment = textAlignment;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;

            return textMesh;
        }





        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }






        public static Vector3 GetMouseWorldPositionWithZ()
        {
            return GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera)
        {
            return GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        //public static Vector3 GetMouseWorldPosition() => Instance.GetMouseWorldPosition_Instance();

        public static Vector3 GetMouseWorldPosition_Instance()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
            {
                //Debug.Log("Hit: " + raycastHit.point);
                return raycastHit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }
    }
}