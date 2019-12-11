using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class broadcastTransformation : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Test;
    GameObject obj;
    void Start()
    {
        //print(gameObject.name);
        obj = this.gameObject;
        
    }

    // Update is called once per frame
    void Update()
    {
        int randomNumber = Random.Range(0, 10);
        string text = randomNumber.ToString();
        Vector3 posi = this.transform.position;
        Test.GetComponent<TextMesh>().text = posi.ToString();
    }
}
