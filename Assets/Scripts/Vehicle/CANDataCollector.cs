﻿/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class DataPoint<T> {
	public string signalName;
	public float timestamp;
	public T value;
	
	public DataPoint(string signal, float time, T value) {
		signalName = signal;
		timestamp = time;
		this.value = value;
	}
}

public struct FullDataFrame
{
    public float time;
    public float cruiseSpeed;
    public float rpm;
    public float gearPosActual;
    public float gearPosTarget;
    public float accelleratorPos;
    public float deceleratorPos;
    public float rollRate;
    public float steeringWheelAngle;
    public float vehicleSpeed;
    public float vehicleSpeedOverGround;
    public float wheelSpeedFL;
    public float wheelSpeedFR;
    public float wheelSpeedRL;
    public float wheelSpeedRR;
    public float yawRate;

    public string ToCSV()
    {
        return string.Format("EMSSetSpeed, {1:F4}, {0}\n" +
                "EngineSpeed, {2:F4}, {0}\n" +
                "GearPosActual, {3:N}, {0}\n" +
                "GearPosTarget, {4:N}, {0}\n" +
                "AcceleratorPedalPos, {5:F4}, {0}\n" +
                "DeceleratorPedalPos, {6:F4}, {0}\n" +
                "RollRate, {7:F4}, {0}\n" +
                "SteeringWheelAngle, {8:F4}, {0}\n" +
                "VehicleSpeed, {9:F4}, {0}\n" +
                "VehicleSpeedOverGnd, {10:F4}, {0}\n" +
                "WheelSpeedFrL, {11:F4}, {0}\n" +
                "WheelSpeedFrR, {12:F4}, {0}\n" +
                "WheelSpeedReL, {13:F4}, {0}\n" +
                "WheelSpeedReR, {14:F4}, {0}\n" +
                "YawRate, {15:F4}, {0}\n", time, cruiseSpeed, rpm, gearPosActual, gearPosTarget, accelleratorPos, deceleratorPos, rollRate, steeringWheelAngle, vehicleSpeed, vehicleSpeedOverGround, wheelSpeedFL, wheelSpeedFR, wheelSpeedRL, wheelSpeedRR, yawRate);
            
    }

    public string ToICCSV()
    {
        return string.Format(
            "EngineSpeed, {1:F4}, {0}\n" +
            "VehicleSpeed, {2:F4}, {0}\n", time, rpm, vehicleSpeed);
    }
}

[StructLayout(LayoutKind.Sequential)]
public class CANDataCollector : MonoBehaviour {

	private List<DataPoint<int>> intData;
	private List<DataPoint<float>>	floatData;
	
	private Rigidbody rb;
	private VehicleController vehicleController;

    private DataStreamServer dataStream;

    public const float sendRate = 0.1f;
    private float lastSend = 0f;


    private float lastYaw = 0f;
    private float lastRoll = 0f;

    void Awake() {
		rb = GetComponent<Rigidbody>();
		vehicleController = GetComponent<VehicleController>();
        dataStream = DataStreamServer.Instance;
        lastYaw = transform.localRotation.eulerAngles.y;
        lastRoll = transform.localRotation.eulerAngles.z;
        
	}

	void OnEnable() {
		intData = new List<DataPoint<int>>();
		floatData = new List<DataPoint<float>>();
	}



    void Update() {

        float yaw = (transform.localRotation.eulerAngles.y - lastYaw) / Time.deltaTime;
        float roll = (transform.localRotation.eulerAngles.z - lastRoll) / Time.deltaTime;
        lastRoll = transform.localRotation.eulerAngles.z;
        lastYaw = transform.localRotation.eulerAngles.y;

        if (Time.time - lastSend < sendRate)
            return;

        float time = Time.time;

       

        //EngineSpeed
        floatData.Add(new DataPoint<float>("EngineSpeed", time, vehicleController.RPM));

        //GearPosActual
        int gear = vehicleController.IsShifting ? -3 : vehicleController.Gear;
        intData.Add(new DataPoint<int>("GearPosActual", time, gear));

        //GearPosTarget
        intData.Add(new DataPoint<int>("GearPosTarget", time, vehicleController.Gear));

        //AcceleratorPedalPos
        int pedalPos = Mathf.RoundToInt(Mathf.Clamp01(vehicleController.accellInput) * 100);
        intData.Add(new DataPoint<int>("AcceleratorPedalPos", time, pedalPos));

        //DeceleratorPedalPos
        int brakePos = Mathf.RoundToInt(Mathf.Clamp(-1, 0, vehicleController.accellInput) * 100);
        intData.Add(new DataPoint<int>("DeceleratorPedalPos", time, brakePos));

        //RollRate
        //TODO

        //SteeringWheelAngle
        int wheelAngle = Mathf.RoundToInt(vehicleController.steerInput * 720);
        intData.Add(new DataPoint<int>("SteeringWheelAngle", time, wheelAngle));

        //VehicleSpeed
        float kmh = rb.velocity.magnitude * 3.6f;
        floatData.Add(new DataPoint<float>("VehicleSpeed", time, kmh));
        //TODO: calculate this from wheel rpm

        //VehicleSpeedOverGnd
        floatData.Add(new DataPoint<float>("VehicleSpeedOverGnd", time, kmh));

        //WheelSpeedFrL
        floatData.Add(new DataPoint<float>("WheelSpeedFrL", time, vehicleController.WheelFL.rpm * 60));

        //WheelSpeedFrR
        floatData.Add(new DataPoint<float>("WheelSpeedFrR", time, vehicleController.WheelFR.rpm * 60));

        //WheelSpeedReL
        floatData.Add(new DataPoint<float>("WheelSpeedReL", time, vehicleController.WheelRL.rpm * 60));

        //WheelSpeedReR
        floatData.Add(new DataPoint<float>("WheelSpeedReR", time, vehicleController.WheelRR.rpm * 60));

        //YawRate
        //TODO

        FullDataFrame frame = new FullDataFrame()
        {
            time = time,
            cruiseSpeed = 0f,
            rpm = vehicleController.RPM,
            gearPosActual = gear,
            gearPosTarget = vehicleController.Gear,
            accelleratorPos = pedalPos,
            deceleratorPos = brakePos,
            rollRate = roll,
            steeringWheelAngle = wheelAngle,
            vehicleSpeed = kmh,
            vehicleSpeedOverGround = kmh,
            wheelSpeedFL = vehicleController.WheelFL.rpm * 60,
            wheelSpeedFR = vehicleController.WheelFR.rpm * 60,
            wheelSpeedRL = vehicleController.WheelRL.rpm * 60,
            wheelSpeedRR = vehicleController.WheelRR.rpm * 60,
            yawRate = yaw
        };

        dataStream.SendAsText(frame);
    }
}
