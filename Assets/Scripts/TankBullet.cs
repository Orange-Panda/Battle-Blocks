using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A predominently server side component which causes a projectile to move, deal damage, and be destroyed.
/// </summary>
public class TankBullet : NetworkComponent
{
	[SerializeField]
	private Renderer[] renderers;
	private float speed = 25;
	private bool used = false;

	protected override IEnumerator NetworkUpdate()
	{
		if (transform.TryGetComponent(out Rigidbody rigidbody) && IsClient)
		{
			Destroy(rigidbody);
		}

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

		yield return new WaitForSeconds(10f);

		if (IsServer && !used)
		{
			used = true;
			NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
		}
	}

	private void Update()
	{
		if (IsServer)
		{
			transform.position += transform.forward * speed * Time.deltaTime;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsServer && !used)
		{
			if (other.gameObject.TryGetComponent(out Enemy enemy))
			{
				used = true;
				enemy.TakeDamage(Owner);
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
			else if (other.gameObject.TryGetComponent(out Tank tank) && tank.Owner != Owner)
			{
				used = true;
				tank.TakeDamage(Owner);
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
			else if (other.CompareTag("Wall"))
			{
				used = true;
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
		}
	}

	public override void HandleMessage(string command, List<string> args) { }
}