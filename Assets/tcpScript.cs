using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

using System.Globalization;


#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class tcpScript : MonoBehaviour
{
    public String ipTCPHost;
    public String portTCPHost;

    public GameObject StatusTextManager;
    public GameObject referenceObject;
    public GameObject controller;

    public GameObject xAxis;
    public GameObject yAxis;
    public GameObject zAxis;
    public GameObject xRotation;
    public GameObject yRotation;
    public GameObject zRotation;

    private List<GameObject> axisList;
    public float incrementStepSize;

    private Vector3 position;
    private Quaternion rotation;
    private bool moveObjectFlag = false;
    private int currentAxisNum = 0;

    private bool fineIncrementMode = false;//2 modes for increment input one for fine second one for rough
#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    public void Connect(string host, string port)
    {
        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }



#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        StatusTextManager.GetComponent<TextMesh>().text = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();
        
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);
        
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut) { AutoFlush = true };
        
            Stream streamIn = socket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);

            RestartExchange();
            StatusTextManager.GetComponent<TextMesh>().text = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            StatusTextManager.GetComponent<TextMesh>().text = "Connected!";
            connected = true;
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchanging = false;
    private bool exchangeStopRequested = false;
    private string lastPacket = null;
    private bool connected = false;
    private string errorStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }

    public void Update()
    {
        //if (connected == false){
        //    Connect("192.168.178.44", "5005");//makes the programm impossible to run if there is no connection:/
        //}
        if (lastPacket != null)
        {
            ReportDataToTrackingManager(lastPacket);
            StatusTextManager.GetComponent<TextMesh>().text = position.ToString("F4") + "\n" + rotation.ToString("F4");
        }



    }
    public void Start()
    {
        Connect(ipTCPHost, portTCPHost);
        //Connect("192.168.178.44", "5005");//Laptop at home
        //Connect("192.168.43.152", "5005");//Hotspot
        //Connect("192.168.43.138", "5005");//Alienware
        axisList = new List<GameObject>() { xAxis, yAxis, zAxis, xRotation, yRotation, zRotation };
    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

            writer.Write("X\n");
            //Debug.Log("Sent data!");
            string received = null;

#if UNITY_EDITOR
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes, 0, client.SendBufferSize);
                received += Encoding.UTF8.GetString(bytes, 0, recv);
                if (received.EndsWith("\n")) break;
            }
#else
            received = reader.ReadLine();
#endif

            lastPacket = received;
            //Debug.Log("Read data: " + received);

            exchanging = false;
        }
    }

    private void ReportDataToTrackingManager(string data)
    {
        if (data == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }
        //print(data);
        //var parts = data.Split(';');
        //foreach (var part in parts)
        //{
        //    ReportStringToTrackingManager(part);
        //}
        //Debug.Log(data);
        ReportStringToTrackingManager(data);
    }

    private void ReportStringToTrackingManager(string rigidBodyString)
    {
        var parts = rigidBodyString.Split(':');//split position:rotation:AmpIncrement:ChangeA,ChangeB aka float:float:float:bool
        var positionData = parts[0].Split(',');
        var rotationData = parts[1].Split(',');
        var incrementAmplifier = parts[2];
        var buttonChanged = parts[3].Split(',');
        //x,y,z:i,j,k,w original was number:x,y,z:i,j,k,w
        float x = float.Parse(positionData[0], CultureInfo.InvariantCulture);//CultureInfo.InvariantCulture necessary because it was not parsing "." correctly
        float y = float.Parse(positionData[1], CultureInfo.InvariantCulture);
        float z = float.Parse(positionData[2], CultureInfo.InvariantCulture);
        float qx = float.Parse(rotationData[0], CultureInfo.InvariantCulture);
        float qy = float.Parse(rotationData[1], CultureInfo.InvariantCulture);
        float qz = float.Parse(rotationData[2], CultureInfo.InvariantCulture);
        float qw = float.Parse(rotationData[3], CultureInfo.InvariantCulture);
        //position = new Vector3(x, y, z);//RealWorld object in holoWorld
        //rotation = new Quaternion(qx, qy, qz, qw);
        //StatusTextManager.GetComponent<TextMesh>().text = position.ToString()+"\n"+rotation.ToString()+"\n"+ parts[3];
        ////unity uses Left handed thus we need to change the system 
        ////we keep y and z the same and x invert thus 
        ////https://gamedev.stackexchange.com/questions/157946/converting-a-quaternion-in-a-right-to-left-handed-coordinate-system
        position = new Vector3(-x, y, z);//RealWorld object in holoWorld
        rotation = new Quaternion(qx, -qy, -qz, qw);

        ////we are interestd in the vive Position in Hololens World hence we need to transform backwards
        ////we have our previous transformation matrix aka transform from vive to reference object 
        Vector3 refPos = referenceObject.transform.position;
        Quaternion refRot = referenceObject.transform.rotation;
        Quaternion invRefRot = Quaternion.Inverse(refRot);
        Vector3 invRefPos = invRefRot * refPos;
        controller.transform.position = invRefRot * position + refPos;
        //controller.transform.position = referenceObject.transform.TransformDirection(position) - refPos;
        controller.transform.rotation = invRefRot * rotation;


        
        float incrementAmp = float.Parse(incrementAmplifier, CultureInfo.InvariantCulture);
        bool triggerButton = bool.Parse(buttonChanged[0]);
        bool trackpadPressed = bool.Parse(buttonChanged[1]);
        bool menuButton = bool.Parse(buttonChanged[2]);
        bool gripButton = bool.Parse(buttonChanged[3]);
        MoveObject(incrementAmp, triggerButton, trackpadPressed,menuButton,gripButton);
        
    }
    private void MoveObject(float xTrackpadPosition, bool triggerButton, bool trackpadPressed, bool menuButton, bool gripButton)
    {
        //var temp = buttonAChanged;
        //buttonAChanged = buttonBChanged;
        //buttonBChanged = temp;
        if (menuButton == true)
        {
            fineIncrementMode = !fineIncrementMode;
        }

        
        //if buttonBCHaanged we change our mode frm moving to not moving or vice versa
        //if this happends we also disable to color highlighting
        //if buttonAchanged we move the axis the axis are numbered and include the rotation
        //axis num 0-5 x y z rotx roty rotz
        //incrementAmp is the multiplier for or stepsize
        if (triggerButton == true)
        {
            //change target Axis
            //first current highlight disable if moveObjectFlag is set
            if (moveObjectFlag == false)
            {
                //if not true no axis is highlighed thus just increase counter
                currentAxisNum++;
                if (currentAxisNum > 5)
                {
                    currentAxisNum = 0;
                }
            }
            else
            {

                if (currentAxisNum < 3)//the translationelements have subelemnents
                {
                    foreach (Renderer r in axisList[currentAxisNum].GetComponentsInChildren<Renderer>())
                    {
                        r.material.color = Color.white;
                    }
                }
                else
                {
                    axisList[currentAxisNum].GetComponent<Renderer>().material.color = Color.white;
                }
                currentAxisNum++;
                //now highlight the current one
                if (currentAxisNum > 5)
                {
                    currentAxisNum = 0;
                }
                if (currentAxisNum < 3)
                {
                    foreach (Renderer r in axisList[currentAxisNum].GetComponentsInChildren<Renderer>())
                    {
                        r.material.color = Color.red;
                    }
                }
                else
                {
                    axisList[currentAxisNum].GetComponent<Renderer>().material.color = Color.red;
                }



            }
        }

        if (gripButton == true && moveObjectFlag == true)
        {
            //disable moving
            moveObjectFlag = false;
            //remove highlighting
            if (currentAxisNum <3)
            {
                foreach (Renderer r in axisList[currentAxisNum].GetComponentsInChildren<Renderer>())
                {
                    r.material.color = Color.white;
                }
            }
            else
            {
                axisList[currentAxisNum].GetComponent<Renderer>().material.color = Color.white;
            }


        }


        if (gripButton == true && moveObjectFlag == false)
        {
            //enable moving
            moveObjectFlag = true;
            //highlight axis
            if (currentAxisNum < 3)
            {
                foreach (Renderer r in axisList[currentAxisNum].GetComponentsInChildren<Renderer>())
                {
                    r.material.color = Color.red;
                }
            }
            else
            {
                axisList[currentAxisNum].GetComponent<Renderer>().material.color = Color.red;
            }
        }
        //move object
        if (moveObjectFlag == true)
        {
            if (fineIncrementMode == false)
            {
                //move object in rough mode
                if (currentAxisNum < 3)
                {
                    var tempRotation = referenceObject.transform.rotation;//we need to transform the unit vectors in the coordinante system of the object before
                                                                          //adding them to the current position of the object
                                                                          //change position
                    switch (currentAxisNum)
                    {
                        case 0:
                            referenceObject.transform.position += tempRotation * new Vector3(0.05f * xTrackpadPosition, 0, 0);
                            break;
                        case 1:
                            referenceObject.transform.position += tempRotation * new Vector3(0, 0.05f * xTrackpadPosition, 0);
                            break;
                        case 2:
                            referenceObject.transform.position += tempRotation * new Vector3(0, 0, 0.05f * xTrackpadPosition);
                            break;
                    }

                }
                else
                {
                    //change rotation
                    switch (currentAxisNum)
                    {
                        case 3:
                            referenceObject.transform.eulerAngles += new Vector3(0.2f * xTrackpadPosition, 0, 0);
                            break;
                        case 4:
                            referenceObject.transform.eulerAngles += new Vector3(0, 0.2f * xTrackpadPosition, 0);
                            break;
                        case 5:
                            referenceObject.transform.eulerAngles += new Vector3(0, 0, 0.2f * xTrackpadPosition);
                            break;
                    }
                }
            }
            else
            {
                //move object in fine mode
                //move object only if trackpad is pressed and depending on the xposition
                if (trackpadPressed == true)
                {
                    var inputDirection = Math.Sign(xTrackpadPosition);

                    if (currentAxisNum < 3)
                    {
                        var tempRotation = referenceObject.transform.rotation;//we need to transform the unit vectors in the coordinante system of the object before
                                                                              //adding them to the current position of the object
                                                                              //change position
                        switch (currentAxisNum)
                        {
                            case 0:
                                referenceObject.transform.position += tempRotation * new Vector3(0.001f * inputDirection, 0, 0);
                                break;
                            case 1:
                                referenceObject.transform.position += tempRotation * new Vector3(0, 0.001f * inputDirection, 0);
                                break;
                            case 2:
                                referenceObject.transform.position += tempRotation * new Vector3(0, 0, 0.001f * inputDirection);
                                break;
                        }

                    }
                    else
                    {
                        //change rotation
                        switch (currentAxisNum)
                        {
                            case 3:
                                referenceObject.transform.eulerAngles += new Vector3(0.1f * inputDirection, 0, 0);
                                break;
                            case 4:
                                referenceObject.transform.eulerAngles += new Vector3(0, 0.1f * inputDirection, 0);
                                break;
                            case 5:
                                referenceObject.transform.eulerAngles += new Vector3(0, 0, 0.1f * inputDirection);
                                break;
                        }
                    }
                }

            }
           
        }

        //if buttonBchanged == false and moveobjectflag==false nothing happens
    }
    
    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;
        }
#else
        if (exchangeTask != null) {
            exchangeTask.Wait();
            socket.Dispose();
            writer.Dispose();
            reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

}
