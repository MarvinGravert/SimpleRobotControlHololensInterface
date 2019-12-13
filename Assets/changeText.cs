using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeText : MonoBehaviour
{
    public GameObject obj;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.GetComponent<TextMesh>().text=obj.transform.position.ToString()+"\n"+obj.transform.rotation.ToString();
    }
}
