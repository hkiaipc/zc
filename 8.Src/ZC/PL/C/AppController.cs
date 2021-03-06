using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using PLC;
using NLog;
using PL.Hardware;

namespace PL {

    public class AppController {
        #region Members
        static private Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _isChecking = false;
        private PlController _plController = null;
        private Timer _timer;
        private App _app;
        #endregion //Members

        /// <summary>
        /// 
        /// </summary>
        public AppController(App app, Gc gc) {
            this._app = app;
            this.ControllerStatus = new AppControllerStatus(gc.AppControlStatus, ControllerStatusEnum.Idle);
            this.AutoManualStatus = new AutoManualStatus(gc.AutoManual);
            this.ZtPlcStatus = new ZtPlcStatus(gc.ZtPlcStatus);
            this.CurrentWorkingDamStatus = new CurrentWorkingDamStatus(gc.CurrentWorkingDam);
            this.CurrentDoneCycleCountStatus = new CurrentDoneCycleCountStatus(gc.CurrentDoneCycleCount);
            this.PlOptionsReader = new PlOptionsReader(this._app);
        }

        /// <summary>
        /// 
        /// </summary>
        public App App {
            get {
                return _app;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        static private void Debug(string msg) {
            _logger.Debug(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        static private void Debug(string msg, params object[] args) {
            _logger.Debug(msg, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsChecking() {
            return _isChecking;
        }

        /// <summary>
        /// call it when app exit
        /// </summary>
        public void Close() {
            this.ControllerStatus.Value = ControllerStatusEnum.NotRun;
        }

        /// <summary>
        /// 
        /// </summary>
        public AutoManualStatus AutoManualStatus {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public AppControllerStatus ControllerStatus {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public PlOptionsReader PlOptionsReader {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public ZtPlcStatus ZtPlcStatus {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public CurrentWorkingDamStatus CurrentWorkingDamStatus {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public CurrentDoneCycleCountStatus CurrentDoneCycleCountStatus {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Check() {
            if (!_isChecking) {
                _isChecking = true;
                OnCheck();
                _isChecking = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string[] GetSubscriptionItemNames() {
            var r = new List<string>(2000);
            foreach (var dam in _app.Dams.ToArray()) {
                foreach (var gun in dam.Guns.ToArray()) {
                    r.Add(gun.Switch.Address);
                    r.Add(gun.Fault.Address);
                    r.Add(gun.Remote.Address);
                    r.Add(gun.Mark.Address);
                    //r.Add(gun.GunWorkStatus.Address);
                }
            }

            _app.Carts.ForEach((cart) => {
                r.Add(cart.Address);
                r.Add(cart.FaultAddress);
            });

            _app.MaterialAreas.ForEach((materialArea) => {
                //r.Add(materialArea.Define.MaterialAttributeAddress);
                //r.Add(materialArea.Define.MaterialIdAddress);
                r.Add(materialArea.Define.StockGroupIdAddress);
                r.Add(materialArea.Define.StockGroupIdStringAddress);
                materialArea.MaterialHeapPositions.ForEach(mhp => {
                    r.Add(mhp.Define.IdAddress);
                    r.Add(mhp.Define.AttributeAddress);
                    r.Add(mhp.Define.StartPositionAddress);
                    r.Add(mhp.Define.EndPositionAddress);
                });
            });

            //var gc = Gc.Instance;
            var gc = _app.Gc;
            r.Add(gc.AppControlStatus);
            r.Add(gc.AutoManual);
            r.Add(gc.CycleCount);
            r.Add(gc.CycleMode);
            r.Add(gc.GunCountPerGroup);
            r.Add(gc.PlTimeSecond);
            r.Add(gc.WorkDam);
            r.Add(gc.ZtPlcStatus);
            r.Add(gc.PlTimeRemaining);
            r.Add(gc.CycleEndStopPump);
            r.Add(gc.CurrentWorkingDam);
            r.Add(gc.CurrentDoneCycleCount);
            return r.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnCheck() {
            try {
                MyLogManager.Output("Check...");

                //if (!_app.Opc.IsConnected()) {
                var opcSm = OpcServerManager.Instance;
                
                if(!opcSm.IsConnected()){
                    bool success = opcSm.TryConnect();
                    if (success) {
                        var opcServer = opcSm.OpcServer;
                        var itemNames = GetSubscriptionItemNames();
                        opcServer.AddSubscriptionItems(itemNames);

                        this.ControllerStatus.Value = ControllerStatusEnum.Idle;
                        this.ControllerStatus.Write();
                    }
                    return;
                }

                if (AutoManualStatus.Read() == AutoManualStatusEnum.Auto) {
                    var controllerStatusEnum = this.ControllerStatus.Value;
                    var ztPlcStatusEnum = ZtPlcStatus.Read();
                    if (ztPlcStatusEnum == ZtPlcStatusEnum.Start) {
                        #region start
                        if (controllerStatusEnum == ControllerStatusEnum.Idle ||
                            controllerStatusEnum == ControllerStatusEnum.Completed) {
                            var options = this.PlOptionsReader.Read();
                            _plController = new PlController(this, options);
                            if (_plController.Start()) {
                                this.ControllerStatus.Value = ControllerStatusEnum.Working;
                            } else {
                                this.ControllerStatus.Value = ControllerStatusEnum.Completed;
                                this.ZtPlcStatus.Write(ZtPlcStatusEnum.Completed);
                            }
                        } else if (controllerStatusEnum == ControllerStatusEnum.Working) {
                            var checkResult = _plController.Check();
                            if (checkResult == PlCheckResultEnum.Completed) {
                                this.ControllerStatus.Value = ControllerStatusEnum.Completed;
                                this.ZtPlcStatus.Write(ZtPlcStatusEnum.Completed);
                                _plController.Close();
                                _plController = null;
                            } else {
                                // working
                                //
                            }
                        } else {
                            // NotRun ?
                            Debug("unknown controller status: {0}", controllerStatusEnum);
                        }
                        #endregion start
                    } else if (ztPlcStatusEnum == ZtPlcStatusEnum.Stop) {
                        #region stop
                        if (controllerStatusEnum == ControllerStatusEnum.Idle ||
                         controllerStatusEnum == ControllerStatusEnum.Completed) {
                            //nothing
                            //
                        } else if (controllerStatusEnum == ControllerStatusEnum.Working) {
                            System.Diagnostics.Debug.Assert(_plController != null);

                            _plController.Stop();
                            this.ControllerStatus.Value = ControllerStatusEnum.Completed;
                            this.ZtPlcStatus.Write(ZtPlcStatusEnum.Completed);

                        } else {
                            Debug("unknown controller status: {0}", controllerStatusEnum);
                        }
                        #endregion stop
                    } else if (ztPlcStatusEnum == ZtPlcStatusEnum.Completed) {
                        // nothind
                        //
                    } else {
                        Debug("unknown ztPlcStatus: {0}", ztPlcStatusEnum);
                    }
                } else {
                    // manual, nothing
                    //
                }
            } catch (OpcException opcEx) {
                MyLogManager.Output(opcEx.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start() {
            if (_timer == null) {
                _timer = new Timer();
                _timer.Interval = Config.CheckInterval;
                _timer.Tick += _timer_Tick;
                _timer.Start();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _timer_Tick(object sender, EventArgs e) {
            Check();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsStarted() {
            return _timer != null && _timer.Enabled;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop() {
            if (_timer != null) {
                _timer.Stop();
                _timer = null;
            }
        }
    }
}
