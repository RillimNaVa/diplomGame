using System;

// Phase 4 / PR 4.PA — runtime entry tracking how many copies of an upgrade
// the player currently owns this run. See §9.2.
[Serializable]
public sealed class ActiveUpgradeStack
{
    public UpgradeData data;
    public int stacks;

    public ActiveUpgradeStack(UpgradeData data, int stacks)
    {
        this.data = data;
        this.stacks = stacks;
    }
}
