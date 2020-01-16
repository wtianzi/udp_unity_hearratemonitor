﻿// Zach Duer - Institute of Creativity, Arts, and Technology, Virginia Polytechnic Institute and State University
// depends on Bespoke OSC library

// How to use:
// 1. Import this script into your Unity project that will be receiving messages
// 2. Attach this script to an object in your scene
// 3. Receive!


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Bespoke.Common;
using Bespoke.Common.Osc;

public class ECGOSCMessageReceiver : MonoBehaviour
{
	//Port to receive data on local server machine - basically arbitrary, but the sender needs to know to send to this port
	private int localPort = 8111;
    
    public LineRenderer heartrateLineRenderer;
    private List<double> m_heartrate;//contains 20 heart rates
    
	private Thread receivingThread; // thread opened to receive data as it comes in
	private UdpClient receivingClient;
	private byte[] bytePacket;
	private IPEndPoint receivedEndPoint;
	private volatile bool dataReceived; // used in update every frame to determine if new data has been received since the last frame

	//Used to resend connect command to remote machine until a reply is received.
	private bool connected = false;
	private int frameCounter = 0;
    
	//private OscMessage messageToSend; // for creating and sending OSC messages.  the OSCMessage type is provided by the Bespoke library
	private IPEndPoint localEndPoint;
	private List<IPEndPoint> clientEndPoints = new List<IPEndPoint>();

	// Use this for initialization
	void Start ()
	{
        m_heartrate = new List<double>();
		dataReceived = false;
		connected = false;

		receivingThread = new Thread (new ThreadStart (ReceiveData));
		receivingThread.IsBackground = true;
		receivingThread.Start ();

		localEndPoint = new IPEndPoint(IPAddress.Loopback, localPort);
		OscPacket.UdpClient = new UdpClient();  // what does this actually do?

        for ( int i=0;i<heartrateLineRenderer.numPositions; i++)
        {
            m_heartrate.Add(0.0f);            
            heartrateLineRenderer.SetPosition(i, new Vector3(i, 0, 0));
            
        }
        
	}

	// Update is called once per frame
	void Update ()
	{
		if (dataReceived)
		{
			dataReceived = false;
            ShowECGData(OscPacket.FromByteArray(receivedEndPoint, bytePacket));
			//FrameParser(OscPacket.FromByteArray(receivedEndPoint, bytePacket));

            //set linerenderer
            for(int i=0;i<heartrateLineRenderer.numPositions;i++)
            {
                heartrateLineRenderer.SetPosition(i, new Vector3(i, 1 + (int)(0.1 * m_heartrate[i]), 0));
            }
		}
	}

	// Use this for exiting
	void OnApplicationQuit ()
	{
		try
		{
			//SendCommand ("StreamFrames Stop");
			//SendCommand ("Disconnect");
			if (receivingClient.Available == 1)
			{
				receivingClient.Close ();
			}
			if (receivingThread != null)
			{
				receivingThread.Abort ();
			}
		} catch (Exception e)
		{
			Debug.Log (e.Message);
		}
	}

	// Receive thread
	private void ReceiveData ()
	{
		receivingClient = new UdpClient (localPort);
		receivingClient.Client.ReceiveTimeout = 500;
		while (true)
		{
			try
			{
				IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 0);
				bytePacket = receivingClient.Receive (ref anyIP);
				receivedEndPoint = anyIP;
				dataReceived = true;
			}
			catch (Exception err)
			{
				SocketException sockErr = (SocketException)err;
				if (sockErr.ErrorCode != 10060)
				{
					Debug.Log ("Error receiving packet: " + sockErr.ToString ());
				}
			}
		}
	}


    private void ShowECGData(OscPacket packet)
    {
        if (packet.IsBundle)
        {
            foreach (OscMessage message in ((OscBundle)packet).Messages)
            {
                if (String.Compare(message.Address, "/Heart rate from ECG/") == 0)
                {                    
                    string t_heartrate = (string)message.Data[0];
                    string[] str_array=t_heartrate.Split('|');//timestamp(second) | heart rate(Beats/min)                    
                    this.GetComponent<TextMesh>().text = str_array[1] + "  (Beats/min) \r\n" + "At :" + str_array[0]+"s";

                    m_heartrate.RemoveAt(0);
                    m_heartrate.Add(Double.Parse(str_array[1]));

                    //Debug.Log("timestamp | heart rate(Beats/min): " + t_heartrate);
                }
               
            }
        }
        else
        { // if the packet is not a bundle and is just one message
            if (String.Compare(((OscMessage)packet).Address, "/ExampleOSCAddressOfMessage/") == 0)
            {
                string theStringMessageIKnowIsBeingSent = ((OscMessage)packet).ToString();
                //Debug.Log("received example message string: " + theStringMessageIKnowIsBeingSent);
            }
        }
    }
	// Process Data Frame OscBundle
	private void FrameParser(OscPacket packet) {
		//LogPacket(packet); //Used for debugging to see all received packets
		if (packet.IsBundle) {
			foreach (OscMessage message in ((OscBundle)packet).Messages) {
                if (String.Compare(message.Address, "/Heart rate from ECG/") == 0)
                {
					// Without using any of the fancy OSC stuff that lets you know what type of data is in each message,
					// you can get the data in the message by accessing packet.Data[index], as long as you know how many indices there are.
					// essentially, i've set this up with the assumption that you're doing both the sending and receiving, so you know what is what and how much data 
					// so, for example:
					string theStringMessageIKnowIsBeingSent = (string)message.Data[0];
					Debug.Log ("1received string: " + theStringMessageIKnowIsBeingSent);
				}
				if (String.Compare (message.Address, "/ExampleOSCAddressOfMessageHeader/Position/") == 0) {
					Vector3 position = new Vector3 ();;
					position.x = (float)message.Data[0];
					position.y = (float)message.Data[1];
					position.z = (float)message.Data[1];
					Debug.Log ("received example position: " + position);
				}
			}
		}
		else{ // if the packet is not a bundle and is just one message
			if (String.Compare(((OscMessage)packet).Address, "/ExampleOSCAddressOfMessage/") == 0) {
                string theStringMessageIKnowIsBeingSent = ((OscMessage)packet).ToString();
				Debug.Log ("received example message string: " + theStringMessageIKnowIsBeingSent);
            }
		}
	}

	// Log OscMessage or OscBundle
	private static void LogPacket(OscPacket packet)
	{
		if(packet.IsBundle)
		{
			foreach (OscMessage message in ((OscBundle)packet).Messages)
			{
				LogMessage(message);
			}
		}
		else
		{
			LogMessage((OscMessage)packet);
		}
	}

	// Log OscMessage
	private static void LogMessage(OscMessage message)
	{
		StringBuilder s = new StringBuilder();
		s.Append(message.Address);
		for (int i = 0; i < message.Data.Count; i++)
		{
			s.Append(" ");
			if (message.Data[i] == null)
			{
				s.Append("Nil");
			}
			else
			{
				s.Append(message.Data[i] is byte[] ? BitConverter.ToString((byte[])message.Data[i]) : message.Data[i].ToString());
			}
		}
		Debug.Log(s);
	}
}
