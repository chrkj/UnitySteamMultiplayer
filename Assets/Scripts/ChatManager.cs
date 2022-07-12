using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    [SerializeField] public GameObject ChatPanel;
    [SerializeField] public GameObject TextObject;

    private const int CHAT_BUFFER_SIZE = 250;
    private readonly Queue<GameObject> m_Messages = new Queue<GameObject>();

    public void SendMessageToChat(string text)
    {
        var newMessage = new Message();
        var newText = Instantiate(TextObject, ChatPanel.transform);
        string timeStamp = $"[{System.DateTime.Now:HH:mm:ss}] ";
        
        newMessage.Text = timeStamp + text;
        newMessage.TextObject = newText.GetComponent<Text>();
        newMessage.TextObject.text = newMessage.Text;

        if (m_Messages.Count >= CHAT_BUFFER_SIZE) 
            Destroy(m_Messages.Dequeue());
        m_Messages.Enqueue(newText);
    }

    [System.Serializable]
    public class Message
    {
        public string Text;
        public Text TextObject;
    }
}