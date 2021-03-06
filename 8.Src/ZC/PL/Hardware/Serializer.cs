using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PL.Hardware {

    public class Serializer {


        static public void test() {
            var damDefines = CreateDamDefines();
            var cartDefines = CreateCartDefines();
            var define = new Define() {
                CartDefines = cartDefines,
                DamDefines = damDefines,
            };


            var s = JsonConvert.SerializeObject(damDefines);
            Console.WriteLine(s);


            var deserialDamDefines = JsonConvert.DeserializeObject<List<DamDefine>>(s);
            Console.WriteLine(deserialDamDefines.Count);
            Console.WriteLine(deserialDamDefines[0].Name);


            var a2Json = JsonConvert.SerializeObject(new Gc());
            Console.WriteLine(a2Json);

        }

        private static List<CartDefine> CreateCartDefines() {
            var r = new List<CartDefine>();
            r.Add(CreateCartDefine(1));
            r.Add(CreateCartDefine(2));
            r.Add(CreateCartDefine(3));
            return r;
        }

        private static CartDefine CreateCartDefine(int n) {
            var cartDefine = new CartDefine() {
                No = n,
                Name = "Cart" + n,
                Address = "cartAddress" + n,
            };
            return cartDefine;
        }

        private static List<DamDefine> CreateDamDefines() {
            var damDefines = new List<DamDefine>();

            var damDefine = CreateDamDefine(1);

            var gunDefine1 = CreateGunDefine(1);
            var gunDefine2 = CreateGunDefine(2);

            damDefine.GunDefines.Add(gunDefine1);
            damDefine.GunDefines.Add(gunDefine2);
            damDefines.Add(damDefine);
            return damDefines;
        }

        private static GunDefine CreateGunDefine(int n) {
            return new GunDefine() {
                No = n,
                Name = "name" + n,
                Fault = "fault" + n,
                Mark = "mark" + n,
                Remote = "remote" + n,
                Switch = "switch" + n,
                Location = n * 10,
                //AssociateCartName = "Cart" + n,
            };
        }

        private static DamDefine CreateDamDefine(int n) {
            var damDefine = new DamDefine() {
                No = n,
                Name = "BD-" + n,
            };
            return damDefine;
        }
    }
}
