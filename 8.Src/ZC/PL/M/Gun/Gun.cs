using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLC;

namespace PL {

    public class Gun {
        #region ctor
        /// <summary>
        /// 
        /// </summary>
        public Gun() {

        }
        #endregion //ctor

        #region Members

        public int No { get; set; }
        public LinkedListNode<Gun> GunNode { get; set; }
        public Fault Fault { get; set; }
        public Mark Mark { get; set; }
        public Remote Remote { get; set; }
        public Switch Switch { get; set; }
        public GunWorkStatus GunWorkStatus { get; set; }
        public Dam Dam { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        public Cart AssociateCart { get; set; }
        #endregion //Members

        //#region Area
        ///// <summary>
        ///// 
        ///// </summary>
        //public Area Area {
        //    get {
        //        if (_area == null) {
        //            _area = Area.Empty;
        //        }
        //        return _area;
        //    }
        //    set {
        //        if (_area != value) {
        //            _area = value;
        //        }
        //    }
        //} private Area _area;
        //#endregion //Area

        #region Next
        /// <summary>
        /// 
        /// </summary>
        public Gun Next {
            get {
                var nextNode = this.GunNode.Next;
                if (nextNode != null) {
                    return nextNode.Value;
                } else {
                    return null;
                }
            }
        }
        #endregion //Next

        #region Eq
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gun"></param>
        /// <returns></returns>
        public bool Eq(Gun gun) {
            return this.Dam.No == this.Dam.No &&
                this.No == gun.No;
        }
        #endregion //Eq

        #region Gt
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gun"></param>
        /// <returns></returns>
        public bool Gt(Gun gun) {
            if (this.Dam.No > gun.Dam.No) {
                return true;
            }

            if (this.Dam.No == gun.Dam.No) {
                return this.No > gun.No;
            }

            return false;
        }
        #endregion //Gt

        #region Lt

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gun"></param>
        /// <returns></returns>
        public bool Lt(Gun gun) {
            if (this.Dam.No < gun.Dam.No) {
                return true;
            }

            if (this.Dam.No == gun.Dam.No) {
                return this.No < gun.No;
            }

            return false;
        }
        #endregion //Lt


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsNotCoverCart() {
            return !IsCoverCart();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsCoverCart() {
            if (this.AssociateCart == null) {
                return false;
            } else {
                var endLocation = this.AssociateCart.Location + Config.CartRange;

                var beginLocation = this.AssociateCart.Location - Config.CartRange;
                if (beginLocation < 0) {
                    beginLocation = 0;
                }

                return
                    this.Location >= beginLocation &&
                    this.Location <= endLocation;
            }
        }

        #region CanUse

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CanUse() {
            GunWorkStatusEnum gunWorkStatusEnum;
            bool r = CanUse(out gunWorkStatusEnum);

            // set plc gun status
            //
            this.GunWorkStatus.Status = gunWorkStatusEnum;
            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CanUse(out GunWorkStatusEnum gunWorkStatusEnum) {
            //return
            //    this.Fault.IsFault == false &&
            //    this.Mark.IsMarked == false &&
            //    this.Remote.IsRemote == true &&
            //    //this.Area.CanWet() &&
            //    this.CanWet() &&
            //    this.IsNotCoverCart();

            if (this.Fault.IsFault) {
                gunWorkStatusEnum = GunWorkStatusEnum.NotWorkWithFault;
                return false;
            }

            if (this.Mark.IsMarked) {
                gunWorkStatusEnum = GunWorkStatusEnum.NotWorkWithMark;
                return false;
            }

            if (this.Remote.IsRemote) {
                gunWorkStatusEnum = GunWorkStatusEnum.NotWorkWithRemote;
                return false;
            }
            if (!this.IsMaterialHeapCanWet()) {
                gunWorkStatusEnum = GunWorkStatusEnum.NotWorkWithMaterialHeap;
                return false;
            }

            if (this.IsCoverCart()) {
                gunWorkStatusEnum = GunWorkStatusEnum.NotWorkWithCart;
                return false;
            }

            gunWorkStatusEnum = GunWorkStatusEnum.Normal;
            return true;
        }
        #endregion //CanUse

        #region Location
        /// <summary>
        /// 
        /// </summary>
        public decimal Location {
            get;
            set;
        }
        #endregion //Location

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsMaterialHeapCanWet() {
            var materialHeap = this.Dam.MaterialHeaps.FindByGun(this);
            if (materialHeap != null) {
                Debug.Assert(materialHeap.IsReadedFromPlc);
                return materialHeap.CanWet;
            } else {
                return true;
            }
        }
    }
}