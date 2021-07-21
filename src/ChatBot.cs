using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json.Linq;

public class ChatBot : MonoBehaviour
{


    public string channel="levelChat";
    public string eventType="message";

    PushSocketIOClient client=null;


    public bool testing=true;
    public bool shouldSendTest=true;


    


    void Update()
    {

        if(client==null){
            client=PushSocketIOClient.Client;
        }



        if(client!=null&&client.authenticated&&testing&&shouldSendTest){

            shouldSendTest=false;
            Invoke("TestChat",Random.Range(5.0f, 25.0f));
        }


        
    }


    JObject Message(string message){
        return new JObject(
            new JProperty("message", message)
        );
    }

    void TestChat(){


        string[] lines=new string[]{
            "My best friend is better than yours! ...",
            "I am the future of America... ...",
            "Shut up brain or I'll stab you with a Q-Tip.",
            "Come to the dark side; we have cookies.",
            "I'm not so good at advice... ...",
            "I run with scissors. ...",
            "Duct Tape is like the force. ...",
            "I poured spot remover on my dog..."
        };


        if(client){
            client.Send(channel, eventType, Message(lines[Random.Range(0, lines.Length)]));
        }

        shouldSendTest=true;


    }

}
