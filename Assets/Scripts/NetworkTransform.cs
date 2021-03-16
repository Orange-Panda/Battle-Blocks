using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTransform : NetworkComponent
{
	public GameObject visual;

	private Vector3 positionObjective;
	private Vector3 positionVelocity;
	private Quaternion rotationObjective;
	private Quaternion rotationVelocity;

	private Vector3 lastPositionSent;
	private Quaternion lastRotationSent;

	private const string UpdateCommand = "P";
	private bool dirty = true;

	protected override void OnAwake()
	{
		base.OnAwake();
		if (visual)
		{
			visual.SetActive(false);
		}
	}

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
			Vector3 currentPosition = transform.position.Rounded();
			if (dirty || currentPosition != lastPositionSent || Quaternion.Angle(lastRotationSent, transform.rotation) > 2)
			{
				dirty = false;
				lastPositionSent = currentPosition;
				lastRotationSent = transform.rotation;
				List<string> args = new List<string>()
				{
					transform.position.x.ToNetworkString(),
					transform.position.y.ToNetworkString(),
					transform.position.z.ToNetworkString(),
					transform.rotation.x.ToNetworkString(),
					transform.rotation.y.ToNetworkString(),
					transform.rotation.z.ToNetworkString(),
					transform.rotation.w.ToNetworkString()
				};
				SendToClient(UpdateCommand, args);
			}
		}
	}

	private void Update()
	{
		if (IsClient)
		{
			transform.position = Vector3.Distance(transform.position, positionObjective) > 2 ? positionObjective : Vector3.SmoothDamp(transform.position, positionObjective, ref positionVelocity, NetworkCore.UpdateDelta);
			transform.rotation = QuaternionUtil.SmoothDamp(transform.rotation, rotationObjective, ref rotationVelocity, NetworkCore.UpdateDelta);
		}
		else if (IsServer)
		{
			positionObjective = transform.position;
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		if (command.Equals(UpdateCommand))
		{
			dirty = false;
			if (args.Count >= 3)
			{
				positionObjective = new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
			}

			if (args.Count >= 7)
			{
				rotationObjective = new Quaternion(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
			}
		}

		if (command.Equals(DirtyCommand))
		{
			dirty = true;
		}
	}

	protected override IEnumerator NetworkUpdate()
	{
		FlagDirtyToServer();
		yield return new WaitUntil(() => !dirty);
		yield return new WaitForSeconds(NetworkCore.UpdateDelta);
		if (visual)
		{
			visual.SetActive(true);
		}
	}
}
