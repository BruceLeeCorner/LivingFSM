using System;

namespace LivingFSM
{
    /* 前进到扫码位，扫码，前进到取料位，等待料被取走，回退到上料位
     * 后续添加功能：如果扫码失败，往后退20，再次向前到扫码位。扫码3次失败则进入错误状态。
     * 在物流运输过程中，包裹通常以高速在传输带上移动，传统的条形码或二维码扫描技术难以在这种高速动态环境下保持稳定的识别率。
     * RFID技术则凭借其非接触式自动识别能力，完美解决了这一问题。
     * RFID系统通过空间耦合实现无接触信息传递，其阅读器可以发射无线电波，
     * 当贴有RFID标签的包裹进入其工作区域时，标签内部的芯片会被激活并发送出自身编码等信息。
     * 阅读器接收到这些信息后，会进行解码并传送给后台系统，从而实现对高速移动中包裹的实时识读。、
     *
     * 若RFID读取失败，产品后退若干行程，然后重新进入感应区，重新激活读取。最多重试2次。
     */

    public class LoadChipAction : LongTimeActionBase
    {
        private enum StepId
        {
            Go2ScanPoint,
            Delay500ms,
            Scan,
            Go2PickPoint,
            IsChipTaken,
            Delay300ms,
            Back2ReadyPoint,
            Other
        }

        private readonly LoadMachine _loadMachine;

        public LoadChipAction(LoadMachine loadMachine) : base(loadMachine)
        {
            _loadMachine = loadMachine;
        }

        public override StepResult Steps()
        {
            RoutineStart();
            Step(StepId.Go2ScanPoint, () =>
            {
                _loadMachine.Go(100);
                return true;
            }, () =>
            {
                return (true, _loadMachine.CurrPoint == 100);
            });

            Delay(StepId.Delay500ms, 500);

            Execute(StepId.Scan, () =>
            {
                Barcode = _loadMachine.Scan();
                return true;
            });

            Step(StepId.Go2PickPoint, () =>
            {
                _loadMachine.Go(200);
                return true;
            }, () =>
            {
                return (true, _loadMachine.CurrPoint == 200);
            });

            Wait(StepId.IsChipTaken, () => _loadMachine.IsChipExist());
            Delay(StepId.Delay300ms, 300);
            Step(StepId.Back2ReadyPoint, () =>
            {
                _loadMachine.Go(0);
                return true;
            }, () =>
            {
                return (true, _loadMachine.CurrPoint == 0);
            });
            //Step(StepId.Other, () =>
            //{
            //    Result result = Result.Forward;
            //    var day = DateTime.Now.Day;
            //    if (day <= 10)
            //    {
            //        result = Result.Finished;
            //    }
            //    else if (day <= 20)
            //    {
            //        result = Result.Failed;
            //    }
            //    else
            //    {
            //        result = Result.Forward;
            //    }
            //    return (result,null);
            //});
            RoutineStop();
            return StepResultToken;
        }

        public string Barcode { get; private set; }

        public override bool Abort(object parameter)
        {
            throw new NotImplementedException();
        }

        public override bool Failed(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Forward(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Finished(object parameter)
        {
        }
    }

#endregion Solid
}