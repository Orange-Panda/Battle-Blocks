using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	[SerializeField]
	private Renderer[] renderers;
	private Vector2 input;

	public const string InputMessage = "I";
	public const string StatusMessage = "S";
	public const string AttackMessage = "A";
	private new Rigidbody rigidbody;
	private float speed = 5;
	private float attackCooldown = 0;

	private int Health { get; set; } = 10;
	private const int MaxHealth = 10;

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

	private void Update()
	{
		if (IsLocalPlayer)
		{
			attackCooldown = Mathf.MoveTowards(attackCooldown, 0, Time.deltaTime);

			if (attackCooldown <= 0 && Input.GetKey(KeyCode.Space))
			{
				attackCooldown = 0.5f;
				SendToServer(AttackMessage);
			}
		}
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
		
		if (IsServer)
		{
			SendToServer(StatusMessage, Health.ToString());
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		if (command.Equals(InputMessage) && args.Count >= 2 && float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
		{
			input = new Vector2(x, y);
		}

		if (command.Equals(StatusMessage) && args.Count >= 1 && int.TryParse(args[0], out int value))
		{
			Health = value;
		}

		if (command.Equals(AttackMessage) && NetworkItems.TryGetIndex("player_bullet", out int index))
		{
			NetworkCore.ActiveNetwork.CreateNetworkObject(index, Owner, rigidbody.position + new Vector3(0, 0.4f, 0), Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y, 0));
		}
	}

	private void FixedUpdate()
	{
		if (IsServer)
		{
			rigidbody.MovePosition(rigidbody.position + transform.forward * Mathf.Clamp(input.y, -1, 1) * speed * Time.fixedDeltaTime);
			rigidbody.rotation = Quaternion.Euler(0, rigidbody.rotation.eulerAngles.y + Mathf.Round(Mathf.Clamp(input.x, -1, 1)) * 2, 0);
		}
	}

	public void TakeDamage(int value = 1)
	{
		if (IsServer)
		{
			Health = Mathf.Clamp(Health - value, 0, MaxHealth);

			if (Health <= 0)
			{
				Spawner spawner = FindObjectOfType<Spawner>();
				rigidbody.position = spawner.playerPoints[Random.Range(0, spawner.playerPoints.Length)];
				Health = MaxHealth;
			}
		}
	}

	protected override IEnumerator NetworkUpdate()
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