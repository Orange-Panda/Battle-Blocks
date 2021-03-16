using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NegotiableTransform : NetworkComponent
{
	public GameObject visual;

	private Vector3 offset;
	private Vector3 internalPosition;
	private Vector3 positionObjective;
	private Vector3 positionVelocity;
	private Vector3 lastPositionSent;

	private Quaternion rotationObjective;
	private Quaternion rotationVelocity;
	private Quaternion lastRotationSent;

	private const string UpdateCommand = "1";
	private const string MoveCommand = "2";
	private bool dirty = true;
	private float speed = 5;
	private new Rigidbody rigidbody;

	protected override void OnAwake()
	{
		base.OnAwake();
		rigidbody = GetComponent<Rigidbody>();
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
			Vector3 currentPosition = rigidbody.position.Rounded();
			if (dirty || currentPosition != lastPositionSent || Quaternion.Angle(lastRotationSent, transform.rotation) > 2)
			{
				dirty = false;
				lastPositionSent = currentPosition;
				lastRotationSent = rigidbody.rotation;
				List<string> args = new List<string>()
				{
					rigidbody.position.x.ToNetworkString(),
					rigidbody.position.y.ToNetworkString(),
					rigidbody.position.z.ToNetworkString(),
					rigidbody.rotation.x.ToNetworkString(),
					rigidbody.rotation.y.ToNetworkString(),
					rigidbody.rotation.z.ToNetworkString(),
					rigidbody.rotation.w.ToNetworkString()
				};
				SendToClient(UpdateCommand, args);
			}
		}

		if (IsClient)
		{
			if (IsLocalPlayer && offset != Vector3.zero)
			{
				List<string> args = new List<string>()
				{
					offset.x.ToNetworkString(),
					offset.y.ToNetworkString(),
					offset.z.ToNetworkString(),
					rigidbody.rotation.x.ToNetworkString(),
					rigidbody.rotation.y.ToNetworkString(),
					rigidbody.rotation.z.ToNetworkString(),
					rigidbody.rotation.w.ToNetworkString()
				};
				SendToServer(MoveCommand, args);
			}
		}
	}

	private void Update()
	{
		if (IsClient)
		{
			internalPosition = Vector3.Distance(internalPosition, positionObjective) > 2 ? positionObjective : Vector3.SmoothDamp(internalPosition, positionObjective, ref positionVelocity, NetworkCore.UpdateDelta);
			if (IsLocalPlayer)
			{
				offset = Vector3.MoveTowards(offset, Vector3.ClampMagnitude(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")), 1), Time.deltaTime * speed);
			}
			rigidbody.MovePosition(internalPosition + offset);
			rigidbody.rotation = QuaternionUtil.SmoothDamp(rigidbody.rotation, rotationObjective, ref rotationVelocity, NetworkCore.UpdateDelta);
		}
		else if (IsServer)
		{
			positionObjective = rigidbody.position;
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
				offset = Vector2.Min(offset, rigidbody.position - positionObjective);
			}

			if (args.Count >= 7)
			{
				rotationObjective = new Quaternion(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
			}
		}

		if (command.Equals(MoveCommand))
		{
			if (args.Count >= 3)
			{
				Vector3 input = new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
				rigidbody.MovePosition(rigidbody.position + Vector3.ClampMagnitude(input, NetworkCore.UpdateDelta * speed));
			}

			if (args.Count >= 7)
			{
				rigidbody.rotation = new Quaternion(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
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
		if (visual)
		{
			visual.SetActive(true);
		}
	}
}