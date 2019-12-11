using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class broadcastTransformation : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject TextObjectCubePosition;
    public GameObject TextCameraPosition;
    public GameObject Camera;
    GameObject obj;
    void Start()
    {
        //print(gameObject.name);
        obj = this.gameObject;
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector3 posi = this.transform.position;
        Quaternion quat = this.transform.rotation;
        TextObjectCubePosition.GetComponent<TextMesh>().text = posi.ToString()+"\n"+quat.ToString();

        Vector3 posi2 = Camera.transform.position;
        Quaternion quat2 = Camera.transform.rotation;
        TextCameraPosition.GetComponent<TextMesh>().text= posi2.ToString() + "\n" + quat2.ToString();

    }
}
