using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEMProtoApp
{
    public class Utils
    {
        /// <summary>
        /// GCを実行する
        /// </summary>
        public static void GCCollect()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
                // GC.Collect 呼び出し後に GC.WaitForPendingFinalizers を呼び出します。これにより、すべてのオブジェクトに対するファイナライザが呼び出されるまで、現在のスレッドは待機します。
                // ファイナライザ作動後は、回収すべき、(ファイナライズされたばかりの) アクセス不可能なオブジェクトが増えます。もう1度 GC.Collect を呼び出し、それらを回収します。
                GC.Collect(); // アクセス不可能なオブジェクトを除去
                GC.WaitForPendingFinalizers(); // ファイナライゼーションが終わるまでスレッド待機
                GC.Collect(0); // ファイナライズされたばかりのオブジェクトに関連するメモリを開放
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
                System.Diagnostics.Debug.WriteLine(exception.StackTrace);
            }
        }
    }
}
