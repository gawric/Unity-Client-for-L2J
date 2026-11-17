/// <summary>Crea la implementación de protocolo para un perfil de servidor.</summary>
public interface IProtocolFactory
{
    IProtocol Create(ServerProfile profile);
}
