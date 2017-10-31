/// Arduino Tinkerkit Braccio robotic arm simulator with IK (Inverse Kinematics) for Unity
/// Shan-Yuan Teng <tanyuan@cmlab.csie.ntu.edu.tw>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;

public class SolveIK : MonoBehaviour {

	private ServerThread st;
	private bool isSend;
	public bool useIK = true; // IK mode or manual adjustment
	public bool autoEnd = true; // horizontal end in IK mode
	public Listener listener;
	public float delaySeconds = 1000f;
	public string portName="/dev/cu.usbmodem1411";
	SerialPort arduino;
	bool startCommands;

	public Vector3 targetPosition;
	public Vector3 currentPosition;

	[Range(0.0f, 180.0f)]
	public float thetaBase = 90f;
	[Range(15.0f, 165.0f)]
	public float thetaShoulder = 45f;
	[Range(0.0f, 180.0f)]
	public float thetaElbow = 180f;
	[Range(0.0f, 180.0f)]
	public float thetaWristVertical = 90f;
	[Range(0.0f, 180.0f)]
	public float thetaWristRotation = 0f;
	[Range(10.0f, 73.0f)]
	public float thetaGripper = 10f;


	public GameObject[] arms = new GameObject[5];

	/* Arm dimensions( m ) */
	float BASE_HGT = 0.078f;
	float HUMERUS = 0.124f;
	float ULNA = 0.124f;
	float GRIPPER = 0.058f;

	/* pre-calculations */
	float hum_sq;
	float uln_sq;
	public int flag = 0;
	int temp = 0;
	int count=0;
	int pika_flag = 0;

	void Start () {
		/* pre-calculations */
		hum_sq = HUMERUS*HUMERUS;
		uln_sq = ULNA*ULNA;
		arduino = new SerialPort (portName, 9600);
		arduino.Open ();
		startCommands = false;
		st = new ServerThread(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, "127.0.0.1", 8000);
		st.Listen();//讓Server socket開始監聽連線
		st.StartConnect();//開啟Server socket
		isSend = true;
	}

	void Update () {
		// Set target position from itself
		temp += 1;
		targetPosition = transform.position;
		//Debug.Log (startCommands);
		if (useIK) {
			SetArm (targetPosition.x, targetPosition.y, targetPosition.z, autoEnd);
		}
		if (Input.GetKeyDown (KeyCode.H) /*&& startCommands!=true*/) {
			flag = 1;
		}
		if (Input.GetKeyDown (KeyCode.N) ) {
			flag = 2;
		}
		if (Input.GetKeyDown (KeyCode.S) ) {
			flag = 3;
		}
		if (Input.GetKeyDown (KeyCode.P) ) {
			flag = 4;
		}
		if (Input.GetKeyDown (KeyCode.C) ) {
			flag = 5;
		}
		//Debug.Log (listener.exp);
		if (pika_flag == 0) {
			switch (listener.exp) {
			case "h":
				if (flag == 1)
					count++;
				else {
					flag = 1;
					count = 0;
				}
				break;
			case "s":
				if (flag == 3)
					count++;
				else {
					flag = 3;
					count = 0;
				}
				break;
			case "n":
				if (flag == 2)
					count++;
				else {
					flag = 2;
					count = 0;
				}
				break;
			}
		}
		if (count>5 && flag == 1 && startCommands!=true && temp%24==0) {
			temp = 0;
			StartCoroutine (Happy());
		}
		else if (count>10 && flag == 2 && startCommands!=true && temp%16==0) {
			temp = 0;
			StartCoroutine (Nature());
		}
		else if (count>5 && flag == 3 && startCommands!=true && temp%75==0) {
			temp = 0;
			StartCoroutine (Sad());
		}
		else if (flag == 4 && startCommands!=true && temp%40==0) {
			temp = 0;
			StartCoroutine (Pika());
		}else if (flag == 5 && startCommands!=true && temp%40==0) {
			temp = 0;
			StartCoroutine (PikaChu());
		}
		// Update robot arm model
		//arms [0].transform.localRotation = Quaternion.Euler(new Vector3 (0f, thetaBase, 0f));
		//arms [1].transform.localRotation = Quaternion.Euler(new Vector3 (0f, 0f, thetaShoulder - 90f));
		//arms [2].transform.localRotation = Quaternion.Euler(new Vector3 (0f, 0f, thetaElbow - 90f));
		//arms [3].transform.localRotation = Quaternion.Euler(new Vector3 (0f, 0f, thetaWristVertical - 90f));
		//arms [4].transform.localRotation = Quaternion.Euler(new Vector3 (0f, thetaWristRotation, 0f));

		// Current wrist position
		//currentPosition = arms [3].transform.position;

		//Socket
		if (st.receiveMessage != null) {
			Debug.Log ("Client :" + st.receiveMessage);
			if (st.receiveMessage [0] == 'H') {
				Debug.Log (st.receiveMessage);
				flag = 5;
				pika_flag = 1;

			} else if (st.receiveMessage [0] == 'L') {
				Debug.Log (st.receiveMessage);
				flag = 2;
				pika_flag = 0;
			}
			st.receiveMessage = null;
		}
		Debug.Log ("Flag : " + flag);
		Debug.Log ("Debug_Flag : " + pika_flag);
		if (isSend == true)
			StartCoroutine (delaySend ());
		st.Receive ();


	}
	IEnumerator Nature(){
		startCommands = true;

		if (arduino.IsOpen) {
			string str;
			thetaBase = 120;
			thetaElbow = 125;
			thetaWristVertical = 105;
			thetaWristRotation = 0;
			thetaShoulder = 30;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			Debug.Log ("Send Serial: " + str);

			//yield return new WaitForSeconds (0.1f);
			thetaShoulder = 50;
			thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";
			arduino.Write (str);
			//Debug.Log ("Send Serial: " + str);
		}
		yield return new WaitForSeconds(delaySeconds);
		startCommands = false;

	}
	IEnumerator Happy(){
		startCommands = true;

		if (arduino.IsOpen) {
			string str;
			thetaBase = 30;
			thetaElbow = 90;
			thetaWristVertical = 90;
			thetaWristRotation = 0;
			thetaShoulder = 70;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			//Debug.Log ("Send Serial: " + str);

			//yield return new WaitForSeconds (0.1f);
			thetaShoulder = 110;
			thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";
			arduino.Write (str);
			//Debug.Log ("Send Serial: " + str);
		}
		yield return new WaitForSeconds(delaySeconds);
		startCommands = false;

	}
	IEnumerator Sad(){
		startCommands = true;
		yield return new WaitForSeconds(delaySeconds);

		if (arduino.IsOpen) {
			string str;
			thetaBase = 150;
			thetaShoulder = 0;
			thetaElbow = 67;
			thetaWristVertical = 60;
			thetaWristRotation = 0;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			Debug.Log ("Send Serial: " + str);

			//yield return new WaitForSeconds (0.1f);
			thetaBase = 90;
			thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";
			arduino.Write (str);
			//Debug.Log ("Send Serial: " + str);
		}
		startCommands = false;

	}
	IEnumerator Test(){
		startCommands = true;
		yield return new WaitForSeconds(delaySeconds);

		if (arduino.IsOpen) {
			string str;
			thetaBase = 120;
			thetaShoulder = 55;
			thetaElbow = 40;
			thetaWristVertical = 10;
			thetaWristRotation = 0;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			//Debug.Log ("Send Serial: " + str);

		}
		startCommands = false;

	}
	IEnumerator Pika(){
		startCommands = true;
		yield return new WaitForSeconds(delaySeconds);

		if (arduino.IsOpen) {
			string str;
			thetaBase = 120;
			thetaShoulder = 75;
			thetaElbow = 85;
			thetaWristVertical = 110;
			thetaWristRotation = 0;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			//Debug.Log ("Send Serial: " + str);

		}
		startCommands = false;

	}
	IEnumerator PikaChu(){
		startCommands = true;
		yield return new WaitForSeconds(delaySeconds);

		if (arduino.IsOpen) {
			string str;
			thetaBase = 120;
			thetaShoulder = 45;
			thetaElbow = 180;
			thetaWristVertical = 0;
			thetaWristRotation = 0;
			string thetaBaseStr = (Mathf.RoundToInt(thetaBase)).ToString("000");
			string thetaShoulderStr = (Mathf.RoundToInt(thetaShoulder)).ToString("000");
			string thetaElbowStr = (Mathf.RoundToInt(thetaElbow)).ToString("000");
			string thetaWristVerticalStr = (Mathf.RoundToInt(thetaWristVertical)).ToString("000");
			string thetaWristRotationStr = (Mathf.RoundToInt(thetaWristRotation)).ToString("000");
			string thetaGripperStr = (Mathf.RoundToInt(thetaGripper)).ToString("000");

			str = thetaBaseStr + thetaShoulderStr + thetaElbowStr + thetaWristVerticalStr + thetaWristRotationStr + thetaGripperStr + "\n";

			arduino.Write (str);

			//Debug.Log ("Send Serial: " + str);

		}
		startCommands = false;

	}
		
	void SetArm(float x, float y, float z, bool endHorizontal) {
		// Base angle
		float bas_angle_r = Mathf.Atan2( x, z );
		float bas_angle_d = bas_angle_r * Mathf.Rad2Deg + 90f;

		float wrt_y = y - BASE_HGT; // Wrist relative height to shoulder
		float s_w = x * x + z * z + wrt_y * wrt_y; // Shoulder to wrist distance square
		float s_w_sqrt = Mathf.Sqrt (s_w);

		// Elbow angle: knowing 3 edges of the triangle, get the angle
		float elb_angle_r = Mathf.Acos ((hum_sq + uln_sq - s_w) / (2f * HUMERUS * ULNA));
		float elb_angle_d = 270f - elb_angle_r * Mathf.Rad2Deg;

		// Shoulder angle = a1 + a2
		float a1 = Mathf.Atan2 (wrt_y, Mathf.Sqrt (x * x + z * z));
		float a2 = Mathf.Acos ((hum_sq + s_w - uln_sq) / (2f * HUMERUS * s_w_sqrt));
		float shl_angle_r = a1 + a2;
		float shl_angle_d = 180f - shl_angle_r * Mathf.Rad2Deg;

		// Keep end point horizontal
		if (endHorizontal) {
			float end_x = arms [4].transform.position.x;
			float end_y = arms [4].transform.position.y;
			float end_z = arms [4].transform.position.z;

			float end_last_angle = thetaWristVertical;

			float dx = end_x - x;
			float dz = end_z - z;

			float wrt_angle_r = Mathf.Atan2 (end_y - y, Mathf.Sqrt (dx * dx + dz * dz));
			float wrt_angle_d = end_last_angle + wrt_angle_r * Mathf.Rad2Deg;

			// Update angle
			if (wrt_angle_d >= 0f && wrt_angle_d <= 180f)
				thetaWristVertical = wrt_angle_d;
		}

		// Update angles
		if (bas_angle_d >= 0f && bas_angle_d <=180f)
			thetaBase = bas_angle_d;
		if (shl_angle_d >= 15f && shl_angle_d <=165f)
			thetaShoulder = shl_angle_d;
		if (elb_angle_d >= 0f && elb_angle_d <=180f)
			thetaElbow = elb_angle_d;
	}
	//Socket delay
	private IEnumerator delaySend()
	{
		isSend = false;
		yield return new WaitForSeconds(1);//延遲1秒後才發送
		st.Send("ok");
		isSend = true;
	}
	//QuitSocket
	private void OnApplicationQuit()//應用程式結束時自動關閉連線
	{
		st.StopConnect();
	}
}
