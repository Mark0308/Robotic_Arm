using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;

public class server : MonoBehaviour {

	private ServerThread st;
	// Use this for initialization
	private bool isSend;
	void Start () {
		st = new ServerThread(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, "127.0.0.1", 8000);
		st.Listen();//讓Server socket開始監聽連線
		st.StartConnect();//開啟Server socket
		isSend = true;

	}
	
	// Update is called once per frame
	void Update () {
		if (st.receiveMessage != null) {
			Debug.Log ("Client :" + st.receiveMessage);
			st.receiveMessage = null;
		}
		if (isSend == true)
			StartCoroutine (delaySend ());
		st.Receive ();
	}
	private IEnumerator delaySend()
	{
		isSend = false;
		yield return new WaitForSeconds(1);//延遲1秒後才發送
		st.Send("ok");
		isSend = true;
	}

	private void OnApplicationQuit()//應用程式結束時自動關閉連線
	{
		st.StopConnect();
	}

}
