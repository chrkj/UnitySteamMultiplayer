using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void OnHostSelected() => SceneManager.LoadScene("HostGameScene");
    public void OnJoinSelected() => SceneManager.LoadScene("JoinGameScene");
}
