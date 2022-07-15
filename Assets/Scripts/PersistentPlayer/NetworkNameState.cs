using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkNameState : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<FixedPlayerName> Name = new NetworkVariable<FixedPlayerName>();
}

public struct FixedPlayerName : INetworkSerializable
{
    private FixedString32Bytes m_Name;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref m_Name);
    }

    public override string ToString()
    {
        return m_Name.Value;
    }

    public static implicit operator string(FixedPlayerName s) => s.ToString();
    public static implicit operator FixedPlayerName(string s) => new FixedPlayerName() { m_Name = new FixedString32Bytes(s) };
}