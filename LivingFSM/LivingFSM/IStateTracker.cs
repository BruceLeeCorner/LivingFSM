using System;

namespace LivingFSM
{
    public interface IStateTracker
    {
        Enum CurrState { get; }
        int HistoryStateCapacity { get; }
        Enum PrevState { get; set; }

        Enum GetState(int prevIndex);
    }
}