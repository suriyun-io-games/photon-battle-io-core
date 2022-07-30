using UnityEngine;

public class DeathMatchNetworkGameRule : IONetworkGameRule
{
    [Tooltip("Rewards for each ranking, sort from high to low (1 - 10)")]
    public MatchReward[] rewards;
    public override bool HasOptionBotCount { get { return true; } }
    public override bool HasOptionMatchTime { get { return true; } }
    public override bool HasOptionMatchKill { get { return true; } }
    public override bool HasOptionMatchScore { get { return false; } }
    public override bool ShowZeroScoreWhenDead { get { return false; } }
    public override bool ShowZeroKillCountWhenDead { get { return false; } }
    public override bool ShowZeroAssistCountWhenDead { get { return false; } }
    public override bool ShowZeroDieCountWhenDead { get { return false; } }
    public override bool RankedByKillCount { get { return true; } }

    public override void OnStopConnection(BaseNetworkGameManager manager)
    {
        base.OnStopConnection(manager);
        if (IsMatchEnded)
            MatchRewardHandler.SetRewards(BaseNetworkGameCharacter.LocalRank, rewards);
    }

    public override bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        var targetCharacter = character as CharacterEntity;
        // In death match mode will not reset score, kill, assist, death
        targetCharacter.Exp = 0;
        targetCharacter.Level = 1;
        targetCharacter.StatPoint = 0;
        targetCharacter.WatchAdsCount = 0;
        targetCharacter.AttributeAmounts = new AttributeAmounts(0);

        return true;
    }
}
