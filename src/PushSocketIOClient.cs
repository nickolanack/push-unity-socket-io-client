using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

using EngineIOSharp.Common.Enum;
using SocketIOSharp.Client;
using SocketIOSharp.Common;

public class PushSocketIOClient : MonoBehaviour
{

    public uint port=443;
    public string url;
    public EngineIOScheme scheme=EngineIOScheme.https;


    public string appId="example";
    public string username="unity3d";
    public string password="";

    SocketIOClient client;

    // Start is called before the first frame update
    void Start()
    {


        client = new SocketIOClient(
            new SocketIOClientOption(EngineIOScheme.https, "socketio.nickolanack.com", 443)
        );


        client.On(SocketIOEvent.CONNECTION, () =>
        {
          Debug.Log("Connected!");

          JObject credentials=new JObject(
            new JProperty("appId",appId),
            new JProperty("username",username+"-"+Application.platform)
          );

          if(password!=null&&!password.Equals("")){
            credentials.Add(new JProperty("password",password));
          }

          client.Emit("authenticate", credentials, (JToken[] data)=>{
            Debug.Log("authenticated");
          });

        });

        client.On(SocketIOEvent.ERROR, () =>
        {
          Debug.Log("Error!");
        });

        client.On(SocketIOEvent.DISCONNECT, () =>
        {
          Debug.Log("Disconnect!");
        });


        client.Connect();
    }





     void OnDisable()
    {
        if(client!=null){
           client.Close();
        }
    }


   
}