using System;

namespace LivingFSM
{
    public class StateTransitedEventArgs : EventArgs
    {
        public StateTransitedEventArgs(Enum msgCmd, Enum state)
        {
            MsgCmd = msgCmd;
            State = state;
        }

        public Enum MsgCmd { get; }
        public Enum State { get; }
    }
}