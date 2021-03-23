using NetworkEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIExample : MonoBehaviour
{
	[Header("Components")]
	public TMP_InputField ip;
	public TMP_InputField port;
	public Button startServer;
	public Button startClient;
	public Button leaveGame;
	public Button quitGame;

	private void Start()
	{
		if (NetworkCore.ActiveNetwork)
		{
			ip.onEndEdit.AddListener((value) => SetIP(value));
			ip.SetTextWithoutNotify(NetworkCore.ActiveNetwork.IPAddress);
			port.onEndEdit.AddListener((value) => SetPort(value));
			port.SetTextWithoutNotify(NetworkCore.ActiveNetwork.Port.ToString());
			startServer.onClick.AddListener(() => NetworkCore.ActiveNetwork.StartServer());
			startClient.onClick.AddListener(() => NetworkCore.ActiveNetwork.StartClient());
			leaveGame.onClick.AddListener(() => NetworkCore.ActiveNetwork.LeaveGame());
			quitGame.onClick.AddListener(() => NetworkCore.ActiveNetwork.QuitGame());
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	private void Update()
	{
		NetworkCore core = NetworkCore.ActiveNetwork;
		if (core == null)
		{
			gameObject.SetActive(false);
		}
		else
		{
			ip.interactable = !core.IsConnected;
			port.interactable = !core.IsConnected;
			startServer.gameObject.SetActive(!core.IsConnected);
			startClient.gameObject.SetActive(!core.IsConnected);
			leaveGame.gameObject.SetActive(core.IsConnected);
			quitGame.gameObject.SetActive(!core.IsConnected);
		}
	}

	private void SetIP(string value)
	{
		NetworkCore.ActiveNetwork.SetIP(value);
		ip.SetTextWithoutNotify(NetworkCore.ActiveNetwork.IPAddress);
	}

	private void SetPort(string value)
	{
		if (int.TryParse(value, out int port))
		{
			NetworkCore.ActiveNetwork.SetPort(port);
		}

		this.port.SetTextWithoutNotify(NetworkCore.ActiveNetwork.Port.ToString());
	}
}