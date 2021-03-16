using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	[SerializeField]
	private Renderer[] renderers;
	private Vector2 input;

	public const string InputMessage = "1";
	private new Rigidbody rigidbody;
	private float speed = 5;

	protected override void OnAwake()
	{
		base.OnAwake();
		rigidbody = GetComponent<Rigidbody>();
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
		if (IsLocalPlayer)
		{
			List<string> args = new List<string>()
			{
				Input.GetAxis("Horizontal").ToNetworkString(),
				Input.GetAxis("Vertical").ToNetworkString()
			}; 
			SendToServer(InputMessage, args);
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		if (command.Equals(InputMessage) && args.Count >= 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
		{
			input = new Vector2(x, y);
		}
	}

	private void FixedUpdate()
	{
		if (IsServer)
		{
			rigidbody.MovePosition(rigidbody.position + transform.forward * Mathf.Clamp(input.y, -1, 1) * speed * Time.fixedDeltaTime);
			rigidbody.rotation = Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y + Mathf.Round(Mathf.Clamp(input.x, -1, 1)), 0);
		}
	}

	protected override IEnumerator NetworkUpdate()
	{
		if (TryGetComponent(out Rigidbody rigidbody))
		{
			if (IsClient)
			{
				Destroy(rigidbody);
			}
			else if (IsServer)
			{
				Spawner spawner = FindObjectOfType<Spawner>();
				rigidbody.position = spawner.playerPoints[Random.Range(0, spawner.playerPoints.Length)];
			}
		}

		yield return null;

		foreach (Renderer renderer in renderers)
		{
			if (IsClient && IsLocalPlayer)
			{
				renderer.material.color = Color.green; 
			}
			else if (IsClient && !IsLocalPlayer)
			{
				renderer.material.color = Color.red;
			}
		}
	}
}