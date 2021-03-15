using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTransform : NetworkComponent
{
	private readonly float smoothTime = 1f / NetworkCore.UpdateRate;
	private Vector3 objective;
	private Vector3 currentVelocity;

	private Quaternion rotationObjective;
	private Quaternion rotationVelocity;

	private const string UpdateCommand = "UPD";

	private void OnEnable()
	{
		NetworkCore.ActiveNetwork.NetworkTick += ActiveNetwork_NetworkTick;
	}

	private void OnDisable()
	{
		NetworkCore.ActiveNetwork.NetworkTick -= ActiveNetwork_NetworkTick;
	}

	private void ActiveNetwork_NetworkTick()
	{
		if (IsServer)
		{
			List<string> args = new List<string>()
			{
				transform.position.x.ToString("N2"),
				transform.position.y.ToString("N2"),
				transform.position.z.ToString("N2"),
				transform.rotation.x.ToString("N2"),
				transform.rotation.y.ToString("N2"),
				transform.rotation.z.ToString("N2"),
				transform.rotation.w.ToString("N2"),
			};
			SendToClient(UpdateCommand, args);
		}
	}

	private void Update()
	{
		if (IsClient)
		{
			transform.position = Vector3.SmoothDamp(transform.position, objective, ref currentVelocity, smoothTime);
			transform.rotation = QuaternionUtil.SmoothDamp(transform.rotation, rotationObjective, ref rotationVelocity, smoothTime);
		}
		else if (IsServer)
		{
			objective = transform.position;
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		if (command.Equals(UpdateCommand))
		{
			if (args.Count >= 3)
			{
				objective = new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
			}

			if (args.Count >= 7)
			{
				rotationObjective = new Quaternion(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
			}
		}
	}

	protected override IEnumerator NetworkUpdate()
	{
		yield break;
	}
}