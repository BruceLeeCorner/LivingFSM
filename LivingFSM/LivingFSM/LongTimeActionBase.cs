using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingFSM
{
    public abstract class LongTimeActionBase : IReceiver
    {
        private readonly CountdownTimer _countdownTimer;
        private readonly List<Enum> _historySteps;
        private readonly List<Enum> todoSteps;
        private readonly StateMachine stateMachine;
        private Enum _currStepId;
        private bool _waitingCondition;

        //loop control
        private int _loopCounter = 0;

        private int _loopTotalCountSetting = 0;
        private Enum _loopStepStartId;

        public LongTimeActionBase(StateMachine stateMachine)
        {
            _historySteps = new List<Enum>();
            this.stateMachine = stateMachine;
        }

        public StepResult StepResultToken { get; private set; }

        protected void PostMsg(Enum msgCmd, params object[] args)
        {
            this.stateMachine.PostMsg(msgCmd, args);
        }

        public abstract void Forward(object parameter);

        public abstract void Finished(object parameter);

        public abstract bool Failed(object parameter);

        public abstract bool Abort(object parameter);

        void IReceiver.RecvArgs(params object[] args)
        {
            this.Args = args;
            Reset();
        }

        public virtual bool CanAction() => true;

        public event Action CannotAction;

        public void OnCannotAction()
        {
            CannotAction?.Invoke();
        }

        protected object[] Args { get; private set; }

        public abstract StepResult Steps();

        /// <summary>
        /// 用于高定制Action执行结果，以及启动另一个LongTimeAction
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void Step(Enum id, Func<StepResult> action)
        {
            PrepareStep(id);
            StepResultToken = action.Invoke();
            if (StepResultToken.Result == Result.Forward)
                NextStep();
        }

        public void Execute(Enum id, Func<bool> func)
        {
            Step(id, func, () => (true, true));
        }

        public void Wait(Enum id, Func<bool> check) => Step(id, () => true, () => (true, check()));

        public void Delay(Enum id, int milliseconds)
        {
            ExecuteAndDelay(id, () => true, milliseconds);
        }

        public void ExecuteAndDelay(Enum id, Func<bool> func, int milliseconds)
        {
            Step(id,
            () =>
            {
                _countdownTimer.Restart(milliseconds);
                return func.Invoke();
            },
            () => (true, _countdownTimer.IsTimeOut)
            );
        }

        public void Step(Enum id, Func<bool> func, Func<(bool success, bool reach)> check)
        {
            PrepareStep(id);

            if (_waitingCondition == false)
            {
                if (!func.Invoke())
                {
                    StepResultToken = StepResult.Failed;
                    return;
                }
                _waitingCondition = true;
            }

            (bool success, bool reach) = check();
            if (!success)
            {
                StepResultToken = StepResult.Failed;
                return;
            }
            if (reach)
            {
                StepResultToken = StepResult.Forward;
                NextStep();
            }
        }

        public void Step(Enum id, LongTimeActionBase longTimeActionBase)
        {
            if (_waitingCondition == false)
            {
                ((IReceiver)longTimeActionBase).RecvArgs();
                _waitingCondition = true;
            }

            StepResultToken = longTimeActionBase.Steps();
            if (StepResultToken.Result == Result.Finished)
            {
                NextStep();
            }
        }

        public void Loop(Enum id, int loopCount)
        {
            PrepareStep(id);
            _loopStepStartId = _currStepId;
            _loopTotalCountSetting = loopCount;
            NextStep();
            StepResultToken = StepResult.Forward;
        }

        public void EndLoop(Enum id)
        {
            PrepareStep(id);
            ++_loopCounter;
            if (_loopCounter >= _loopTotalCountSetting) //Loop 结束
            {
                _loopCounter = 0;
                _loopTotalCountSetting = 0; // Loop 结束时，当前loop和loop总数都清零
                NextStep();
                StepResultToken = StepResult.Forward;
            }
            //继续下一LOOP
            Rollback2Step(_loopStepStartId);
            StepResultToken = StepResult.Forward;
        }

        protected void RoutineStart()
        {
            StepResultToken = StepResult.Forward;
        }

        protected void RoutineStop()
        {
            StepResultToken = StepResult.Finished;
        }

        private void PrepareStep(Enum id)
        {
            //如果失败；只有reset之后才能继续。即使未在返回Fail时，忘记切换状态，下一次的Timer到来，不会执行任何Step
            if (StepResultToken.Result != Result.Forward)
                return;

            // 已经执行过的步骤直接跳过
            if (_historySteps.Any(item => item.IsSame(id)))
            {
                return;
            }

            // 碰到的第一个尚未执行的步骤作为当前要处理的步骤。
            if ((_historySteps.Contains(_currStepId) && _currStepId != id))
            {
                _currStepId = id;

                if (todoSteps.Contains(id))
                {
                    todoSteps.Remove(id);
                }
            }

            // 尚未执行但还未轮到的步骤（只是加入到待执行步骤而已）
            if (_currStepId != id)
            {
                if (!todoSteps.Contains(id))
                {
                    todoSteps.Add(id);
                }
                return;
            }
        }

        private void NextStep()
        {
            _historySteps.Add(_currStepId);
            _waitingCondition = false;
        }

        private void Rollback2Step(Enum id)
        {
            int index = _historySteps.FindIndex(item => item.IsSame(id));
            if (index == -1)
            {
                System.Diagnostics.Trace.Assert(false, $"Error, no step {index}");
            }
            if (_historySteps.Count > index + 1)
            {
                _historySteps.RemoveRange(index + 1, _historySteps.Count - index - 1);
            }
            _currStepId = _historySteps[index];
            todoSteps.Insert(0, _currStepId);
            _waitingCondition = false;
        }

        private void Reset()
        {
            _currStepId = ActionStepId.Initial;
            _historySteps.Clear();
            todoSteps.Clear();
            _loopCounter = 0;
            _loopTotalCountSetting = 0;
            _waitingCondition = false;
        }
    }
}