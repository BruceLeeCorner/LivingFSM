using System;

namespace LivingFSM
{
    public class LoadMachine : StateMachine
    {
        public void Go(int traget)
        {
        }

        public string Scan()
        {
            return Environment.TickCount.ToString();
        }

        public bool IsChipExist()
        {
            return false;
        }

        public int CurrPoint => DateTime.Now.Second * 10; // 0,10,20,...600
    }
}