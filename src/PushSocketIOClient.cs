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
    public string username="";
    public string password="";

    public string channelPrefix="example.";


    SocketIOClient client;


    public delegate void EventDelegate(JToken[] data);

    Dictionary<string, Dictionary<string, List<EventDelegate>>> events=new Dictionary<string, Dictionary<string, List<EventDelegate>>>();


    public static PushSocketIOClient Client;

    // Start is called before the first frame update
    // 
    void Start()
    {

      ConnectSocket();
    }

    void ConnectSocket()
    {

      PushSocketIOClient.Client=this;

        client = new SocketIOClient(
            new SocketIOClientOption(EngineIOScheme.https, url, 443)
        );




        string  user=username;
        if(user==null||user.Equals("")){
          user="unity-"+"-"+Application.platform+"-"+SystemInfo.deviceUniqueIdentifier;
        }


        
        Debug.Log(user);

        client.On("connection", delegate()
        {
          Debug.Log("Connected!");



          JObject credentials=new JObject(
            new JProperty("appId",appId),
            new JProperty("username",user)
          );

          if(password!=null&&!password.Equals("")){
            credentials.Add(new JProperty("password",password));
          }

          Debug.Log("send auth "+credentials);
          client.Emit("authenticate", credentials, delegate(JToken[] ack){
              


              

              if(ack[0].ToObject<bool>()){
                  Debug.Log("authenticated");
                  Subscribe("global", "message", delegate(JToken[] data){

                    Debug.Log("received message");

                  }, delegate(JToken[] ack){


                    Send("global", "message", new JObject(
                      new JProperty("hello","world")
                    ));


                  });


                  


              }


             



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


    void Send(string channelName, string eventName, JObject data){

      string channel=channelPrefix+channelName;


        JObject send=new JObject(
            new JProperty("channel", channel+"/"+eventName),
            new JProperty("data", data)
          );
        Debug.Log("Send: "+channel+"/"+eventName+": "+send);

        client.Emit("emit", send, delegate(JToken[] ack){

            if(!ack[0].ToObject<bool>()){
                Debug.Log("Failed to send data: "+channel+"/"+eventName);
                return;
            }
            
            Debug.Log("Sent data: "+channel+"/"+eventName);
           
        });


    }

    void Subscribe(string channelName, string eventName, EventDelegate callback){
      Subscribe(channelName, eventName, callback, null);
    }
    void Subscribe(string channelName, string eventName, EventDelegate callback, EventDelegate ackCallback){


      string channel=channelPrefix+channelName;


      Debug.Log("Subscribe to: "+channel+": "+eventName);

      if(!events.ContainsKey(channel)){
        events.Add(channel, new Dictionary<string, List<EventDelegate>>());
      }

      if(!events[channel].ContainsKey(eventName)){
        events[channel].Add(eventName, new List<EventDelegate>());

        //subscribe here!
        

        Debug.Log("Emit subscription");

        JObject send=new JObject(
            new JProperty("channel", channel+"/"+eventName)
        );

        client.Emit("subscribe", send, delegate(JToken[] ack){
            Debug.Log("subscribed: "+channel+"/"+eventName);

            if(ackCallback!=null){
              ackCallback(ack);
            }

        });
        client.On(channel+"/"+eventName, delegate(JToken[] token){
            Debug.Log("received: "+channel+"/"+eventName);
            //fire any events;
        });

      }


      Debug.Log("Add Delegate");

      events[channel][eventName].Add(callback);

    }


     void OnDisable()
    {
        if(client!=null){
           client.Close();
        }
    }


   
}