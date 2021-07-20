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


    public delegate void EventDelegate();

    Dictionary<string, Dictionary<string, List<EventDelegate>>> events=new Dictionary<string, Dictionary<string, List<EventDelegate>>>();


    public static PushSocketIOClient Client;

    // Start is called before the first frame update
    void Start()
    {

      PushSocketIOClient.Client=this;

        client = new SocketIOClient(
            new SocketIOClientOption(EngineIOScheme.https, url, 443)
        );


        client.On("connection", delegate()
        {
          Debug.Log("Connected!");

          JObject credentials=new JObject(
            new JProperty("appId",appId),
            new JProperty("username",username+"-"+Application.platform)
          );

          if(password!=null&&!password.Equals("")){
            credentials.Add(new JProperty("password",password));
          }

          Debug.Log("send auth "+credentials);
          client.Emit("authenticate", credentials, delegate(JToken[] ack){
                Debug.Log("authenticated");

          });

        });

        client.On("error", delegate()
        {
          Debug.Log("Error!");
        });

        client.On("disconnect", delegate()
        {
          Debug.Log("Disconnect!");
        });


        client.Connect();






    }


    void Subscribe(string channel, string eventName, EventDelegate callback){

      if(!events.ContainsKey(channel)){
        events.Add(channel, new Dictionary<string, List<EventDelegate>>());
      }

      if(!events[channel].ContainsKey(eventName)){
        events[channel].Add(eventName, new List<EventDelegate>());

        //subscribe here!
        

        client.Emit("subscribe", channel+"/"+eventName, delegate(JToken[] token){
            Debug.Log("subscribed: "+channel+"/"+eventName);

        });
        client.On(channel+"/"+eventName, delegate(JToken[] token){
            Debug.Log("received: "+channel+"/"+eventName);

        });

      }

      events[channel][eventName].Add(callback);

    }


     void OnDisable()
    {
        if(client!=null){
           client.Close();
        }
    }


   
}