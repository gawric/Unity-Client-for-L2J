using System;
using UnityEngine;

public class AskJoinPartyDto : IWireDto
{
    private string _requestorName;
    private PartyDistributionType _partyDistributionType;

    public string RequestorName => _requestorName;
    public PartyDistributionType DistributionType => _partyDistributionType;

    

    public void ReadFrom(PacketReader reader)
    {
        try
        {
            // Читаем packetId (уже должен быть прочитан в базовом классе для идентификации пакета)
            _requestorName = reader.ReadOtherS();
            int distributionTypeId = reader.ReadI();

            // Конвертируем ID в enum
            _partyDistributionType = PartyDistributionTypeExtensions.FindById(distributionTypeId) ??
                                   PartyDistributionType.FindersKeepers;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AskJoinParty] Parse error: {ex.Message}");
            _requestorName = string.Empty;
            _partyDistributionType = PartyDistributionType.FindersKeepers;
        }
    }

    public override string ToString()
    {
        return $"[AskJoinParty] Requestor: {_requestorName}, DistributionType: {_partyDistributionType}";
    }
}