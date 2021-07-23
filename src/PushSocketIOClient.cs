using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

using EngineIOSharp.Common.Enum;
using SocketIOSharp.Client;
using SocketIOSharp.Common;

using System;

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


    //TODO: make this private
    public bool connected=false;
    public bool authenticated=false;


    public bool printDebug=false;


    public delegate void EventDelegate(JToken[] data);
    
    Dictionary<string, Dictionary<string, List<EventDelegate>>> events=new Dictionary<string, Dictionary<string, List<EventDelegate>>>();
    

    public delegate void StateDelegate();
    Dictionary<string, List<StateDelegate>> stateListeners=new Dictionary<string, List<StateDelegate>>();


    public static PushSocketIOClient Client;

    class EmitData{

      public string cmd;
      public JObject data;
      public EventDelegate ack;

    }


    Queue<EmitData> emitQueue=new Queue<EmitData>();
    bool emitting=false;

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


        
        //Debug.Log(user);

        client.On("connection", delegate()
        {

          connected=true;
          EmitState("connect");
          Debug.Log("Connected");

          JObject credentials=new JObject(
            new JProperty("appId",appId),
            new JProperty("username",user)
          );

          if(password!=null&&!password.Equals("")){
            credentials.Add(new JProperty("password",password));
          }

          //Debug.Log("send auth "+credentials);
          QueueEmit("authenticate", credentials, delegate(JToken[] ack){
              
              
              

              if(ack[0].ToObject<bool>()){

                authenticated=true;
                EmitState("authenticate");
                  //Debug.Log("Authenticated");
              }


          });

        });

        client.On("error", delegate()
        {
          Debug.Log("Error!");
        });

        client.On("disconnect", delegate()
        {
          connected=false;
          Debug.Log("Disconnect!");
        });


        client.Connect();

    }


    void Update(){
        if((!emitting)&&emitQueue.Count>0){
          Emit(emitQueue.Dequeue());
        }
    }


    void Emit(EmitData evt){

      emitting=true;
      client.Emit(evt.cmd, evt.data, delegate(JToken[] ack){
        
        if(evt.ack!=null){
          evt.ack(ack);
        }
        emitting=false;
      });

    }


    void QueueEmit(string cmd, JObject data, EventDelegate ack){

      EmitData evt=new EmitData();
      evt.cmd=cmd;
      evt.data=data;
      evt.ack=ack;
      emitQueue.Enqueue(evt);

    }


    public void Send(string channelName, string eventName, JObject data){

      string channel=channelPrefix+channelName;


        JObject send=new JObject(
            new JProperty("channel", channel+"/"+eventName),
            new JProperty("data", data)
          );
        //Debug.Log("Send: "+channel+"/"+eventName+": "+send);

        QueueEmit("emit", send, delegate(JToken[] ack){

            if(!ack[0].ToObject<bool>()){
                Debug.Log("Failed to send data: "+channel+"/"+eventName);
                return;
            }
            
            //Debug.Log("Sent data: "+channel+"/"+eventName);
           
        });

    }

    public void Subscribe(string channelName, string eventName, EventDelegate callback){
      Subscribe(channelName, eventName, callback, null);
    }


    void Once(string state, StateDelegate del){

      StateDelegate wrapper=null;

      wrapper=delegate(){
        OffState(state, wrapper);
        del();
      };
      OnState(state, wrapper);

    }

    void EmitState(string state){

      Debug.Log("Emit state: "+state);

      if(!stateListeners.ContainsKey(state)){
          return;
      }

      foreach(StateDelegate del in stateListeners[state].ToArray()){
        del();
      }

    }

    void OnState(string state, StateDelegate del){
        if(!stateListeners.ContainsKey(state)){
          stateListeners.Add(state, new List<StateDelegate>());
        }



        stateListeners[state].Add(del);
    }

    void OffState(string state, StateDelegate del){
      if(!stateListeners.ContainsKey(state)){
        throw new Exception("There are no listeners for state: "+state);   
      }

      if(!stateListeners[state].Contains(del)){
        throw new Exception("Callback is not listening for state: "+state);   
      }

      stateListeners[state].Remove(del);
      if(stateListeners[state].Count==0){
        stateListeners.Remove(state);
      }

    }

    public void Subscribe(string channelName, string eventName, EventDelegate callback, EventDelegate ackCallback){


      if(!(connected&&authenticated)){
        
        Debug.Log("Queue subscription: "+channelName+"/"+eventName);

        Once("authenticate", delegate(){

          Subscribe(channelName, eventName, callback, ackCallback);

        });

        return;

      }



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

        QueueEmit("subscribe", send, delegate(JToken[] ack){
            Debug.Log("subscribed: "+channel+"/"+eventName);

            if(ackCallback!=null){
              ackCallback(ack);
            }

        });
        client.On(channel+"/"+eventName, delegate(JToken[] data){
            Debug.Log("received: "+channel+"/"+eventName);
            //fire any events;
            
            if(!events.ContainsKey(channel)){
              Debug.Log("No listeners: "+channel);
              return;
            }


            if(!events[channel].ContainsKey(eventName)){
              Debug.Log("No listeners: "+channel+"/"+eventName);
              return;
            }
                


            foreach(EventDelegate listener in events[channel][eventName]){
              listener(data);
            }
              
            

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